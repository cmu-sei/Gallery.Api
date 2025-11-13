// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Gallery.Api.Data;
using Gallery.Api.Data.Models;
using Gallery.Api.Infrastructure.Exceptions;
using Gallery.Api.Infrastructure.Extensions;
using Gallery.Api.ViewModels;
using Microsoft.IdentityModel.Tokens;

namespace Gallery.Api.Services
{
    public interface IExhibitService
    {
        Task<IEnumerable<ViewModels.Exhibit>> GetAsync(bool canViewAll, CancellationToken ct);
        Task<IEnumerable<ViewModels.Exhibit>> GetMineAsync(CancellationToken ct);
        Task<IEnumerable<ViewModels.Exhibit>> GetUserExhibitsAsync(Guid userId, CancellationToken ct);
        Task<IEnumerable<ViewModels.Exhibit>> GetByCollectionAsync(Guid collectionId, bool canViewAll, CancellationToken ct);
        Task<IEnumerable<ViewModels.Exhibit>> GetMineByCollectionAsync(Guid collectionId, CancellationToken ct);
        Task<ViewModels.Exhibit> GetAsync(Guid id, bool checkForTeamMembership, CancellationToken ct);
        Task<ViewModels.Exhibit> CreateAsync(ViewModels.Exhibit exhibit, CancellationToken ct);
        Task<ViewModels.Exhibit> CopyAsync(Guid exhibitId, CancellationToken ct);
        Task<Tuple<MemoryStream, string>> DownloadJsonAsync(Guid exhibitId, CancellationToken ct);
        Task<Exhibit> UploadJsonAsync(FileForm form, CancellationToken ct);
        Task<ViewModels.Exhibit> UpdateAsync(Guid id, ViewModels.Exhibit exhibit, CancellationToken ct);
        Task<ViewModels.Exhibit> SetMoveAndInjectAsync(Guid id, int move, int inject, CancellationToken ct);
        Task<bool> DeleteAsync(Guid id, CancellationToken ct);
    }

    public class ExhibitService : IExhibitService
    {
        private readonly GalleryDbContext _context;
        private readonly IAuthorizationService _authorizationService;
        private readonly ClaimsPrincipal _user;
        private readonly IMapper _mapper;
        private readonly IUserArticleService _userArticleService;
        private readonly IUserClaimsService _userClaimsService;

        public ExhibitService(
            GalleryDbContext context,
            IAuthorizationService authorizationService,
            IPrincipal user,
            IMapper mapper,
            IUserArticleService userArticleService,
            IUserClaimsService userClaimsService)
        {
            _context = context;
            _authorizationService = authorizationService;
            _user = user as ClaimsPrincipal;
            _mapper = mapper;
            _userArticleService = userArticleService;
            _userClaimsService = userClaimsService;
        }

        public async Task<IEnumerable<ViewModels.Exhibit>> GetAsync(bool canViewAll, CancellationToken ct)
        {
            List<ExhibitEntity> exhibits = new List<ExhibitEntity>();
            if (canViewAll)
            {
                exhibits = await _context.Exhibits.ToListAsync();
            }
            else
            {
                var userId = _user.GetId();
                exhibits = await _context.ExhibitMemberships
                    .Where(m => m.UserId == userId)
                    .Select(m => m.Exhibit)
                    .ToListAsync();
            }

            return _mapper.Map<IEnumerable<Exhibit>>(exhibits);
        }

        public async Task<IEnumerable<ViewModels.Exhibit>> GetMineAsync(CancellationToken ct)
        {
            var userId = _user.GetId();

            return await GetUserExhibitsAsync(userId, ct);
        }

        public async Task<IEnumerable<ViewModels.Exhibit>> GetUserExhibitsAsync(Guid userId, CancellationToken ct)
        {
            var exhibits = await _context.Teams
                .Where(t => t.TeamUsers.Any(tu => tu.UserId == userId) && t.ExhibitId != null)
                .Select(et => et.Exhibit)
                .Distinct()
                .ToListAsync();

            return _mapper.Map<IEnumerable<Exhibit>>(exhibits);
        }

        public async Task<ViewModels.Exhibit> GetAsync(Guid id, bool checkForTeamMembership, CancellationToken ct)
        {
            if (checkForTeamMembership)
            {
                var userId = _user.GetId();
                var isMember = await _context.TeamUsers
                    .AnyAsync(tu => tu.UserId == userId && tu.Team.ExhibitId == id, ct);
                if(!isMember)
                    throw new ForbiddenException();
            }
            var item = await _context.Exhibits.SingleOrDefaultAsync(sm => sm.Id == id, ct);

            return _mapper.Map<Exhibit>(item);
        }

        public async Task<IEnumerable<ViewModels.Exhibit>> GetByCollectionAsync(Guid collectionId, bool canViewAll, CancellationToken ct)
        {
            var exhibits = new List<ExhibitEntity>();
            if (canViewAll)
            {
                exhibits = await _context.Exhibits
                    .Where(a => a.CollectionId == collectionId)
                    .OrderByDescending(a => a.DateCreated)
                    .ToListAsync();
            }
            else
            {
                var userId = _user.GetId();
                exhibits = await _context.ExhibitMemberships
                    .Where(m => m.Exhibit.CollectionId == collectionId && m.UserId == userId)
                    .Select(m => m.Exhibit)
                    .OrderByDescending(e => e.DateCreated)
                    .ToListAsync();
            }

            return _mapper.Map<IEnumerable<Exhibit>>(exhibits);
        }

        public async Task<IEnumerable<ViewModels.Exhibit>> GetMineByCollectionAsync(Guid collectionId, CancellationToken ct)
        {
            var userId = _user.GetId();
            IQueryable<ExhibitEntity> exhibits = _context.Teams
                .Where(t => t.TeamUsers.Any(tu => tu.UserId == userId) && t.Exhibit.CollectionId == collectionId)
                .Select(t => t.Exhibit)
                .Distinct()
                .OrderByDescending(a => a.DateCreated);

            return _mapper.Map<IEnumerable<Exhibit>>(await exhibits.ToListAsync());
        }

        public async Task<ViewModels.Exhibit> CreateAsync(ViewModels.Exhibit exhibit, CancellationToken ct)
        {
            var collection = await _context.Collections.FirstOrDefaultAsync(m => m.Id == exhibit.CollectionId);
            if (collection == null)
                throw new EntityNotFoundException<Collection>("Collection not found while trying to create an exhibit.");

            var userId = _user.GetId();
            exhibit.Name = string.IsNullOrEmpty(exhibit.Name) ? collection.Name : exhibit.Name;
            exhibit.Description = string.IsNullOrEmpty(exhibit.Description) ? collection.Description : exhibit.Description;
            exhibit.DateCreated = DateTime.UtcNow;
            exhibit.CreatedBy = userId;
            exhibit.DateModified = null;
            exhibit.ModifiedBy = null;
            var exhibitEntity = _mapper.Map<ExhibitEntity>(exhibit);
            exhibitEntity.Id = exhibitEntity.Id != Guid.Empty ? exhibitEntity.Id : Guid.NewGuid();
            _context.Exhibits.Add(exhibitEntity);
            await _context.SaveChangesAsync(ct);
            var createOwnerMembership = new ExhibitMembershipEntity() {
                UserId = userId,
                ExhibitId = exhibitEntity.Id,
                RoleId = ExhibitRoleDefaults.ExhibitCreatorRoleId
            };
            await _context.ExhibitMemberships.AddAsync(createOwnerMembership, ct);
            await _context.SaveChangesAsync(ct);
            await _userClaimsService.RefreshClaims(userId);
            exhibit = await GetAsync(exhibitEntity.Id, false, ct);

            return exhibit;
        }

        public async Task<ViewModels.Exhibit> CopyAsync(Guid exhibitId, CancellationToken ct)
        {
            var exhibitFileObject = await BuildExhibitFileObject(exhibitId, ct);
            var newExhibitEntity = await privateExhibitCopyAsync(exhibitFileObject, false, ct);
            var exhibit = _mapper.Map<Exhibit>(newExhibitEntity);

            return exhibit;
        }

        private async Task<ExhibitFileFormat> BuildExhibitFileObject(Guid exhibitId, CancellationToken ct)
        {
            var exhibitEntity = await _context.Exhibits
                .AsNoTracking()
                .Include(m => m.Teams)
                .AsSingleQuery()
                .SingleOrDefaultAsync(m => m.Id == exhibitId);
            if (exhibitEntity == null)
                throw new EntityNotFoundException<ExhibitEntity>("Exhibit not found with ID=" + exhibitId.ToString());

            var collection = await _context.Collections
                .AsNoTracking()
                .Where(m => m.Id == exhibitEntity.CollectionId)
                .SingleOrDefaultAsync(ct);
            var cards = await _context.Cards
                .AsNoTracking()
                .Where(m => m.CollectionId == exhibitEntity.CollectionId)
                .ToListAsync(ct);
            var articles = await _context.Articles
                .AsNoTracking()
                .Where(m => m.CollectionId == exhibitEntity.CollectionId)
                .ToListAsync(ct);
            var teams = await _context.Teams
                .AsNoTracking()
                .Where(m => m.ExhibitId == exhibitEntity.Id)
                .ToListAsync(ct);
            var teamCards = await _context.TeamCards
                .AsNoTracking()
                .Where(m => m.Card.CollectionId == exhibitEntity.CollectionId && m.Team.ExhibitId == exhibitEntity.Id)
                .ToListAsync(ct);
            var teamArticles = await _context.TeamArticles
                .AsNoTracking()
                .Where(m => m.Article.CollectionId == exhibitEntity.CollectionId && m.Team.ExhibitId == exhibitEntity.Id)
                .ToListAsync(ct);
            var exhibitFileObject = new ExhibitFileFormat() {
                Collection = collection,
                Cards = cards,
                Articles = articles,
                Exhibit = exhibitEntity,
                Teams = teams,
                TeamCards = teamCards,
                TeamArticles = teamArticles
            };

            return exhibitFileObject;
        }

        private async Task<ExhibitEntity> privateExhibitCopyAsync(ExhibitFileFormat exhibitFileObject, bool copyTheCollection, CancellationToken ct)
        {
            var currentUserId = _user.GetId();
            var username = (await _context.Users.SingleOrDefaultAsync(u => u.Id == _user.GetId())).Name;
            var dateCreated = DateTime.UtcNow;
            var oldCollectionId = exhibitFileObject.Collection.Id;
            var newCollectionId = Guid.NewGuid();
            var cardMap = new Dictionary<Guid, Guid>();
            var articleMap = new Dictionary<Guid, Guid>();
            if (copyTheCollection)
            {
                // copy the collection
                exhibitFileObject.Collection.Id = newCollectionId;
                exhibitFileObject.Collection.DateCreated = dateCreated;
                exhibitFileObject.Collection.CreatedBy = currentUserId;
                exhibitFileObject.Collection.DateModified = exhibitFileObject.Collection.DateCreated;
                exhibitFileObject.Collection.ModifiedBy = exhibitFileObject.Collection.CreatedBy;
                exhibitFileObject.Collection.Name = exhibitFileObject.Collection.Name + " - " + username;
                await _context.Collections.AddAsync(exhibitFileObject.Collection, ct);
                // copy cards
                foreach (var cardEntity in exhibitFileObject.Cards)
                {
                    cardMap[cardEntity.Id] = Guid.NewGuid();
                    cardEntity.Id = cardMap[cardEntity.Id];
                    cardEntity.CollectionId = exhibitFileObject.Collection.Id;
                    cardEntity.Collection = null;
                    cardEntity.DateCreated = exhibitFileObject.Collection.DateCreated;
                    cardEntity.CreatedBy = exhibitFileObject.Collection.CreatedBy;
                    await _context.Cards.AddAsync(cardEntity, ct);
                }
                // copy articles
                foreach (var articleEntity in exhibitFileObject.Articles)
                {
                    articleMap[articleEntity.Id] = Guid.NewGuid();
                    articleEntity.Id = articleMap[articleEntity.Id];
                    articleEntity.CollectionId = newCollectionId;
                    articleEntity.Collection = null;
                    articleEntity.CardId = articleEntity.CardId == null ? null : cardMap[(Guid)articleEntity.CardId];
                    articleEntity.Card = null;
                    articleEntity.DateCreated = exhibitFileObject.Collection.DateCreated;
                    articleEntity.CreatedBy = exhibitFileObject.Collection.CreatedBy;
                    await _context.Articles.AddAsync(articleEntity, ct);
                }
            }

            var oldExhibitId = exhibitFileObject.Exhibit.Id;
            var newExhibitId = Guid.NewGuid();
            exhibitFileObject.Exhibit.Id = newExhibitId;
            exhibitFileObject.Exhibit.CollectionId = copyTheCollection ? newCollectionId : oldCollectionId;
            exhibitFileObject.Exhibit.DateCreated = DateTime.UtcNow;
            exhibitFileObject.Exhibit.CreatedBy = currentUserId;
            exhibitFileObject.Exhibit.DateModified = exhibitFileObject.Exhibit.DateCreated;
            exhibitFileObject.Exhibit.ModifiedBy = exhibitFileObject.Exhibit.CreatedBy;
            exhibitFileObject.Exhibit.Teams = null;
            exhibitFileObject.Exhibit.CurrentMove = 0;
            exhibitFileObject.Exhibit.CurrentInject = 0;
            exhibitFileObject.Exhibit.ScenarioId = null;
            await _context.Exhibits.AddAsync(exhibitFileObject.Exhibit, ct);
            // copy teams
            var teamMap = new Dictionary<Guid, Guid>();
            foreach (var team in exhibitFileObject.Teams)
            {
                teamMap[team.Id] = Guid.NewGuid();
                team.Id = teamMap[team.Id];
                team.ExhibitId = exhibitFileObject.Exhibit.Id;
                team.Exhibit = null;
                team.DateCreated = exhibitFileObject.Exhibit.DateCreated;
                team.CreatedBy = exhibitFileObject.Exhibit.CreatedBy;
                await _context.Teams.AddAsync(team, ct);
            }
            // copy team cards
            foreach (var teamCard in exhibitFileObject.TeamCards)
            {
                teamCard.Id = Guid.NewGuid();
                teamCard.TeamId = teamMap[teamCard.TeamId];
                teamCard.Team = null;
                teamCard.CardId = copyTheCollection ? cardMap[teamCard.CardId] : teamCard.CardId;
                teamCard.Card = null;
                teamCard.DateCreated = dateCreated;
                teamCard.CreatedBy = currentUserId;
                await _context.TeamCards.AddAsync(teamCard, ct);
            }
            // copy team articles
            foreach (var teamArticle in exhibitFileObject.TeamArticles)
            {
                teamArticle.Id = Guid.NewGuid();
                teamArticle.ExhibitId = newExhibitId;
                teamArticle.Exhibit = null;
                teamArticle.TeamId = teamMap[teamArticle.TeamId];
                teamArticle.Team = null;
                teamArticle.ArticleId = copyTheCollection ? articleMap[teamArticle.ArticleId] : teamArticle.ArticleId;
                teamArticle.Article = null;
                teamArticle.DateCreated = dateCreated;
                teamArticle.CreatedBy = currentUserId;
                await _context.TeamArticles.AddAsync(teamArticle, ct);
            }
            await _context.SaveChangesAsync(ct);
            var createOwnerMembership = new ExhibitMembershipEntity() {
                UserId = currentUserId,
                ExhibitId = newExhibitId,
                RoleId = ExhibitRoleDefaults.ExhibitCreatorRoleId
            };
            await _context.ExhibitMemberships.AddAsync(createOwnerMembership, ct);
            await _context.SaveChangesAsync(ct);
            await _userClaimsService.RefreshClaims(currentUserId);

            // get the new Exhibit to return
            var exhibitEntity = await _context.Exhibits
                .SingleOrDefaultAsync(sm => sm.Id == exhibitFileObject.Exhibit.Id, ct);

            return exhibitEntity;
        }

        public async Task<Tuple<MemoryStream, string>> DownloadJsonAsync(Guid exhibitId, CancellationToken ct)
        {
            var exhibitFileObject = await BuildExhibitFileObject(exhibitId, ct);
            var createdBy = (await _context.Users.SingleOrDefaultAsync(m => m.Id == exhibitFileObject.Exhibit.CreatedBy, ct)).Name;
            var dateCreated = exhibitFileObject.Exhibit.DateCreated.ToString("-YYYYmmDD-");
            var exhibitJson = "";
            var options = new JsonSerializerOptions()
            {
                ReferenceHandler = ReferenceHandler.Preserve
            };
            exhibitJson = JsonSerializer.Serialize(exhibitFileObject, options);
            // convert string to stream
            byte[] byteArray = Encoding.ASCII.GetBytes(exhibitJson);
            MemoryStream memoryStream = new MemoryStream(byteArray);
            var filename = exhibitFileObject.Collection.Name + dateCreated + createdBy + ".json";

            return System.Tuple.Create(memoryStream, filename);
        }

        public async Task<Exhibit> UploadJsonAsync(FileForm form, CancellationToken ct)
        {
            var uploadItem = form.ToUpload;
            var exhibitJson = "";
            using (StreamReader reader = new StreamReader(uploadItem.OpenReadStream()))
            {
                // convert stream to string
                exhibitJson = reader.ReadToEnd();
            }
            var options = new JsonSerializerOptions()
            {
                ReferenceHandler = ReferenceHandler.Preserve
            };
            var exhibitFileObject = JsonSerializer.Deserialize<ExhibitFileFormat>(exhibitJson, options);
            // make a copy and add it to the database
            var exhibitEntity = await privateExhibitCopyAsync(exhibitFileObject, true, ct);

            return _mapper.Map<Exhibit>(exhibitEntity);
        }

        public async Task<ViewModels.Exhibit> UpdateAsync(Guid id, ViewModels.Exhibit exhibit, CancellationToken ct)
        {
            var exhibitToUpdate = await _context.Exhibits.SingleOrDefaultAsync(v => v.Id == id, ct);
            if (exhibitToUpdate == null)
                throw new EntityNotFoundException<Exhibit>();

            _mapper.Map(exhibit, exhibitToUpdate);
            _context.Exhibits.Update(exhibitToUpdate);
            await _context.SaveChangesAsync(ct);
            await _userArticleService.LoadUserArticlesAsync(exhibitToUpdate, ct);

            exhibit = await GetAsync(exhibitToUpdate.Id, false, ct);

            return exhibit;
        }

        public async Task<ViewModels.Exhibit> SetMoveAndInjectAsync(Guid id, int move, int inject, CancellationToken ct)
        {
            var exhibitToUpdate = await _context.Exhibits.SingleOrDefaultAsync(v => v.Id == id, ct);
            if (exhibitToUpdate == null)
                throw new EntityNotFoundException<Exhibit>();

            await _context.SaveChangesAsync(ct);
            await _userArticleService.LoadUserArticlesAsync(exhibitToUpdate, ct);

            var updatedExhibit = await GetAsync(exhibitToUpdate.Id, false, ct);

            return updatedExhibit;
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
        {
            var exhibitToDelete = await _context.Exhibits.SingleOrDefaultAsync(v => v.Id == id, ct);
            if (exhibitToDelete == null)
                throw new EntityNotFoundException<Exhibit>();

            _context.Exhibits.Remove(exhibitToDelete);
            await _context.SaveChangesAsync(ct);

            return true;
        }

    }
    class ExhibitFileFormat
    {
        public CollectionEntity Collection { get; set; }
        public List<CardEntity> Cards { get; set; }
        public List<ArticleEntity> Articles { get; set; }
        public ExhibitEntity Exhibit { get; set; }
        public List<TeamEntity> Teams { get; set; }
        public List<TeamCardEntity> TeamCards { get; set; }
        public List<TeamArticleEntity> TeamArticles { get; set; }
    }
}
