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
        Task<ViewModels.Exhibit> CreateAsync(ViewModels.ExhibitCreate exhibit, CancellationToken ct);
        Task<ViewModels.Exhibit> CopyAsync(Guid exhibitId, CancellationToken ct);
        Task<Tuple<MemoryStream, string>> DownloadJsonAsync(Guid exhibitId, CancellationToken ct);
        Task<Exhibit> UploadJsonAsync(FileForm form, CancellationToken ct);
        Task<ViewModels.Exhibit> UpdateAsync(Guid id, ViewModels.ExhibitEdit exhibit, CancellationToken ct);
        Task<ViewModels.Exhibit> SetMoveAndInjectAsync(Guid id, int move, int inject, CancellationToken ct);
        Task<ViewModels.Exhibit> AdvanceAsync(Guid id, CancellationToken ct);
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
        private readonly ISteamfitterService _steamfitterService;

        public ExhibitService(
            GalleryDbContext context,
            IAuthorizationService authorizationService,
            IPrincipal user,
            IMapper mapper,
            IUserArticleService userArticleService,
            IUserClaimsService userClaimsService,
            ISteamfitterService steamfitterService)
        {
            _steamfitterService = steamfitterService;
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
                if (!isMember)
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

        public async Task<ViewModels.Exhibit> CreateAsync(ViewModels.ExhibitCreate exhibit, CancellationToken ct)
        {
            // Validate required fields
            if (exhibit.CollectionId == Guid.Empty)
                throw new ArgumentException("CollectionId is required");

            // Validate that the collection exists
            var collection = await _context.Collections.FirstOrDefaultAsync(m => m.Id == exhibit.CollectionId, ct);
            if (collection == null)
                throw new EntityNotFoundException<Collection>($"Collection {exhibit.CollectionId} not found");

            var userId = _user.GetId();
            exhibit.Name = string.IsNullOrEmpty(exhibit.Name) ? collection.Name : exhibit.Name;
            exhibit.Description = string.IsNullOrEmpty(exhibit.Description) ? collection.Description : exhibit.Description;
            // share email goes through this Scenario with Gallery's service account
            if (exhibit.ScenarioId.HasValue &&
                !await _steamfitterService.CanCallerReadScenarioAsync(exhibit.ScenarioId.Value, ct))
                throw new EntityNotFoundException<Exhibit>("Scenario not found");
            var exhibitEntity = _mapper.Map<ExhibitEntity>(exhibit);
            exhibitEntity.Id = exhibitEntity.Id != Guid.Empty ? exhibitEntity.Id : Guid.NewGuid();

            _context.Exhibits.Add(exhibitEntity);
            await _context.SaveChangesAsync(ct);
            var createOwnerMembership = new ExhibitMembershipEntity()
            {
                UserId = userId,
                ExhibitId = exhibitEntity.Id,
                RoleId = ExhibitRoleDefaults.ExhibitCreatorRoleId
            };
            await _context.ExhibitMemberships.AddAsync(createOwnerMembership, ct);
            await _context.SaveChangesAsync(ct);
            await _userClaimsService.RefreshClaims(userId);

            return await GetAsync(exhibitEntity.Id, false, ct);
        }

        public async Task<ViewModels.Exhibit> CopyAsync(Guid exhibitId, CancellationToken ct)
        {
            var exhibitFileObject = await BuildExhibitFileObject(exhibitId, ct);
            var newExhibitEntity = await privateExhibitCopyAsync(ToImport(exhibitFileObject), false, ct);
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
            // the templates and this exhibit's own articles, never a sibling exhibit's
            var articles = await _context.Articles
                .AsNoTracking()
                .Where(ArticleScope.InExhibit(exhibitEntity))
                .ToListAsync(ct);
            var teams = await _context.Teams
                .AsNoTracking()
                .Where(m => m.ExhibitId == exhibitEntity.Id)
                .ToListAsync(ct);
            var teamCards = await _context.TeamCards
                .AsNoTracking()
                .Where(m => m.Card.CollectionId == exhibitEntity.CollectionId && m.Team.ExhibitId == exhibitEntity.Id)
                .ToListAsync(ct);
            // only the TeamArticles naming an article the file carries; older data can link a sibling exhibit's article
            var articleIds = articles.Select(m => m.Id).ToList();
            var teamArticles = await _context.TeamArticles
                .AsNoTracking()
                .Where(m => m.ExhibitId == exhibitEntity.Id && articleIds.Contains(m.ArticleId) && m.Team.ExhibitId == exhibitEntity.Id)
                .ToListAsync(ct);
            var exhibitFileObject = new ExhibitFileFormat()
            {
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

        private static ExhibitImport ToImport(ExhibitFileFormat exhibitFileObject) => new()
        {
            Collection = new CollectionImportFields
            {
                Id = exhibitFileObject.Collection.Id,
                Name = exhibitFileObject.Collection.Name,
                Description = exhibitFileObject.Collection.Description
            },
            Cards = exhibitFileObject.Cards.Select(ImportBuilder.ToImport).ToList(),
            Articles = exhibitFileObject.Articles.Select(ImportBuilder.ToImport).ToList(),
            Exhibit = new ExhibitImportFields
            {
                Id = exhibitFileObject.Exhibit.Id,
                Name = exhibitFileObject.Exhibit.Name,
                Description = exhibitFileObject.Exhibit.Description,
                ShowAdvanceButton = exhibitFileObject.Exhibit.ShowAdvanceButton
            },
            Teams = exhibitFileObject.Teams.Select(ImportBuilder.ToImport).ToList(),
            TeamCards = exhibitFileObject.TeamCards.Select(ImportBuilder.ToImport).ToList(),
            TeamArticles = exhibitFileObject.TeamArticles.Select(ImportBuilder.ToImport).ToList()
        };

        /// <summary>
        /// Saves a new exhibit built from <paramref name="exhibitImport"/>. With <paramref name="copyTheCollection"/>
        /// it also saves a new collection with the file's cards and template articles; without it the new exhibit
        /// uses the file's collection, which must exist. The exhibit's own articles are copied to the new exhibit.
        /// No users, roles or memberships are read; the caller becomes the exhibit's creator.
        /// </summary>
        private async Task<ExhibitEntity> privateExhibitCopyAsync(ExhibitImport exhibitImport, bool copyTheCollection, CancellationToken ct)
        {
            var currentUserId = _user.GetId();
            var username = (await _context.Users.SingleOrDefaultAsync(u => u.Id == _user.GetId())).Name;
            var newExhibitId = Guid.NewGuid();
            var fileExhibitId = exhibitImport.Exhibit.Id;
            var fileArticles = exhibitImport.Articles ?? [];

            // build and check everything before anything is tracked
            CollectionEntity newCollection = null;
            Guid collectionId;
            var cardIdMap = new Dictionary<Guid, Guid>();
            var articleIdMap = new Dictionary<Guid, Guid>();
            List<CardEntity> cards;
            List<ArticleEntity> articles;
            if (copyTheCollection)
            {
                collectionId = Guid.NewGuid();
                newCollection = new CollectionEntity
                {
                    Id = collectionId,
                    Name = exhibitImport.Collection.Name + " - " + username,
                    Description = exhibitImport.Collection.Description,
                    CreatedBy = currentUserId
                };
                (cards, articles, articleIdMap) = ImportBuilder.BuildCollectionContent(
                    collectionId, exhibitImport.Cards, fileArticles, fileExhibitId, newExhibitId, cardIdMap);
            }
            else
            {
                collectionId = exhibitImport.Collection.Id;
                // the cards and template articles stay where they are; only the exhibit's own articles are copied
                foreach (var cardId in await _context.Cards.Where(c => c.CollectionId == collectionId).Select(c => c.Id).ToListAsync(ct))
                    cardIdMap[cardId] = cardId;
                (cards, articles, articleIdMap) = ImportBuilder.BuildCollectionContent(
                    collectionId, [], fileArticles.Where(a => a == null || a.ExhibitId == fileExhibitId), fileExhibitId, newExhibitId, cardIdMap);
                foreach (var articleId in await _context.Articles.Where(a => a.CollectionId == collectionId && a.ExhibitId == null).Select(a => a.Id).ToListAsync(ct))
                    articleIdMap[articleId] = articleId;
            }
            // an article of another exhibit was left out, and so is a TeamArticle that names it
            var leftOutArticleIds = fileArticles
                .Where(a => a != null && a.Id != Guid.Empty && a.ExhibitId != null && a.ExhibitId != fileExhibitId)
                .Select(a => a.Id)
                .ToHashSet();

            var exhibitEntity = new ExhibitEntity
            {
                Id = newExhibitId,
                CollectionId = collectionId,
                Name = exhibitImport.Exhibit.Name,
                Description = exhibitImport.Exhibit.Description,
                ShowAdvanceButton = exhibitImport.Exhibit.ShowAdvanceButton,
                CurrentMove = 0,
                CurrentInject = 0,
                ScenarioId = null,
                CreatedBy = currentUserId
            };
            var teamIdMap = new Dictionary<Guid, Guid>();
            var teams = new List<TeamEntity>();
            foreach (var team in exhibitImport.Teams ?? [])
            {
                // a team without an id is imported, but nothing in the file can name it
                if (team == null || team.Id != Guid.Empty && teamIdMap.ContainsKey(team.Id))
                    throw new BadRequestException("The file lists a team more than once.");
                var teamEntity = new TeamEntity
                {
                    Id = Guid.NewGuid(),
                    ExhibitId = newExhibitId,
                    Name = team.Name,
                    ShortName = team.ShortName,
                    Email = team.Email,
                    CreatedBy = currentUserId
                };
                if (team.Id != Guid.Empty)
                    teamIdMap[team.Id] = teamEntity.Id;
                teams.Add(teamEntity);
            }
            var teamCards = new List<TeamCardEntity>();
            foreach (var teamCard in exhibitImport.TeamCards ?? [])
            {
                if (teamCard == null || !teamIdMap.TryGetValue(teamCard.TeamId, out var teamId) || !cardIdMap.TryGetValue(teamCard.CardId, out var cardId))
                    throw new BadRequestException("A team card names a team or card that is not in the file.");
                teamCards.Add(new TeamCardEntity
                {
                    Id = Guid.NewGuid(),
                    TeamId = teamId,
                    CardId = cardId,
                    Move = teamCard.Move,
                    Inject = teamCard.Inject,
                    IsShownOnWall = teamCard.IsShownOnWall,
                    CanPostArticles = teamCard.CanPostArticles,
                    CreatedBy = currentUserId
                });
            }
            var teamArticles = new List<TeamArticleEntity>();
            foreach (var teamArticle in exhibitImport.TeamArticles ?? [])
            {
                if (teamArticle != null && leftOutArticleIds.Contains(teamArticle.ArticleId))
                    continue;
                if (teamArticle == null || !teamIdMap.TryGetValue(teamArticle.TeamId, out var teamId) || !articleIdMap.TryGetValue(teamArticle.ArticleId, out var articleId))
                    throw new BadRequestException("A team article names a team or article that is not in the file.");
                teamArticles.Add(new TeamArticleEntity
                {
                    Id = Guid.NewGuid(),
                    ExhibitId = newExhibitId,
                    TeamId = teamId,
                    ArticleId = articleId,
                    CreatedBy = currentUserId
                });
            }

            if (newCollection != null)
                await _context.Collections.AddAsync(newCollection, ct);
            await _context.Cards.AddRangeAsync(cards, ct);
            await _context.Exhibits.AddAsync(exhibitEntity, ct);
            await _context.Articles.AddRangeAsync(articles, ct);
            await _context.Teams.AddRangeAsync(teams, ct);
            await _context.TeamCards.AddRangeAsync(teamCards, ct);
            await _context.TeamArticles.AddRangeAsync(teamArticles, ct);
            await _context.SaveChangesAsync(ct);
            var createOwnerMembership = new ExhibitMembershipEntity()
            {
                UserId = currentUserId,
                ExhibitId = newExhibitId,
                RoleId = ExhibitRoleDefaults.ExhibitCreatorRoleId
            };
            await _context.ExhibitMemberships.AddAsync(createOwnerMembership, ct);
            await _context.SaveChangesAsync(ct);
            await _userClaimsService.RefreshClaims(currentUserId);

            // get the new Exhibit to return
            return await _context.Exhibits
                .SingleOrDefaultAsync(sm => sm.Id == newExhibitId, ct);
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
            ExhibitImport exhibitImport;
            try
            {
                exhibitImport = JsonSerializer.Deserialize<ExhibitImport>(exhibitJson, options);
            }
            catch (JsonException)
            {
                throw new BadRequestException("The uploaded file is not valid JSON.");
            }
            if (exhibitImport == null || exhibitImport.Exhibit == null || exhibitImport.Collection == null)
                throw new BadRequestException("The uploaded file is not a valid exhibit file.");

            // make a copy and add it to the database
            var exhibitEntity = await privateExhibitCopyAsync(exhibitImport, true, ct);

            return _mapper.Map<Exhibit>(exhibitEntity);
        }

        public async Task<ViewModels.Exhibit> UpdateAsync(Guid id, ViewModels.ExhibitEdit exhibit, CancellationToken ct)
        {
            var exhibitToUpdate = await _context.Exhibits.SingleOrDefaultAsync(v => v.Id == id, ct);
            if (exhibitToUpdate == null)
                throw new EntityNotFoundException<Exhibit>();

            // share email goes through this Scenario with Gallery's service account
            if (exhibit.ScenarioId.HasValue && exhibit.ScenarioId != exhibitToUpdate.ScenarioId &&
                !await _steamfitterService.CanCallerReadScenarioAsync(exhibit.ScenarioId.Value, ct))
                throw new EntityNotFoundException<Exhibit>("Scenario not found");

            _mapper.Map(exhibit, exhibitToUpdate);
            _context.Exhibits.Update(exhibitToUpdate);
            await _context.SaveChangesAsync(ct);
            await _userArticleService.LoadUserArticlesAsync(exhibitToUpdate, ct);

            return await GetAsync(exhibitToUpdate.Id, false, ct);
        }

        public async Task<ViewModels.Exhibit> SetMoveAndInjectAsync(Guid id, int move, int inject, CancellationToken ct)
        {
            var exhibitToUpdate = await _context.Exhibits.SingleOrDefaultAsync(v => v.Id == id, ct);
            if (exhibitToUpdate == null)
                throw new EntityNotFoundException<Exhibit>();

            exhibitToUpdate.CurrentMove = move;
            exhibitToUpdate.CurrentInject = inject;
            await _context.SaveChangesAsync(ct);
            await _userArticleService.LoadUserArticlesAsync(exhibitToUpdate, ct);

            var updatedExhibit = await GetAsync(exhibitToUpdate.Id, false, ct);

            return updatedExhibit;
        }

        public async Task<ViewModels.Exhibit> AdvanceAsync(Guid id, CancellationToken ct)
        {
            var exhibitToUpdate = await _context.Exhibits.SingleOrDefaultAsync(v => v.Id == id, ct);
            if (exhibitToUpdate == null)
                throw new EntityNotFoundException<Exhibit>();

            // Get all unique move/inject pairs from the articles in this exhibit
            var moveInjectPairs = await _context.Articles
                .Where(ArticleScope.InExhibit(exhibitToUpdate))
                .Select(a => new { a.Move, a.Inject })
                .Distinct()
                .OrderBy(a => a.Move)
                .ThenBy(a => a.Inject)
                .ToListAsync(ct);

            var currentMove = exhibitToUpdate.CurrentMove;
            var currentInject = exhibitToUpdate.CurrentInject;

            // Find the next move/inject pair after the current position
            var nextPair = moveInjectPairs
                .Where(p => p.Move > currentMove || (p.Move == currentMove && p.Inject > currentInject))
                .OrderBy(p => p.Move)
                .ThenBy(p => p.Inject)
                .FirstOrDefault();

            if (nextPair == null)
                return null;

            exhibitToUpdate.CurrentMove = nextPair.Move;
            exhibitToUpdate.CurrentInject = nextPair.Inject;
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
