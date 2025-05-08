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
using Gallery.Api.Infrastructure.Authorization;
using Gallery.Api.Infrastructure.Exceptions;
using Gallery.Api.Infrastructure.Extensions;
using Gallery.Api.ViewModels;

namespace Gallery.Api.Services
{
    public interface ICollectionService
    {
        Task<IEnumerable<ViewModels.Collection>> GetAsync(CancellationToken ct);
        Task<IEnumerable<ViewModels.Collection>> GetMineAsync(CancellationToken ct);
        Task<ViewModels.Collection> GetAsync(Guid id, CancellationToken ct);
        Task<ViewModels.Collection> CreateAsync(ViewModels.Collection collection, CancellationToken ct);
        Task<ViewModels.Collection> CopyAsync(Guid collectionId, CancellationToken ct);
        Task<Tuple<MemoryStream, string>> DownloadJsonAsync(Guid collectionId, CancellationToken ct);
        Task<Collection> UploadJsonAsync(FileForm form, CancellationToken ct);
        Task<ViewModels.Collection> UpdateAsync(Guid id, ViewModels.Collection collection, CancellationToken ct);
        Task<bool> DeleteAsync(Guid id, CancellationToken ct);
    }

    public class CollectionService : ICollectionService
    {
        private readonly GalleryDbContext _context;
        private readonly IAuthorizationService _authorizationService;
        private readonly ClaimsPrincipal _user;
        private readonly IMapper _mapper;

        public CollectionService(
            GalleryDbContext context,
            IAuthorizationService authorizationService,
            IPrincipal user,
            IMapper mapper)
        {
            _context = context;
            _authorizationService = authorizationService;
            _user = user as ClaimsPrincipal;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ViewModels.Collection>> GetAsync(CancellationToken ct)
        {
            IQueryable<CollectionEntity> collections = _context.Collections;

            return _mapper.Map<IEnumerable<Collection>>(await collections.ToListAsync());
        }

        public async Task<IEnumerable<ViewModels.Collection>> GetMineAsync(CancellationToken ct)
        {
            var userId = _user.GetId();
            IQueryable<CollectionEntity> collections = _context.Teams
                .Where(t => t.TeamUsers.Any(tu => tu.UserId == userId) && t.Exhibit.CollectionId != null)
                .Select(t => t.Exhibit.Collection)
                .Distinct()
                .OrderBy(c => c.Name);

            return _mapper.Map<IEnumerable<Collection>>(await collections.ToListAsync());
        }

        public async Task<ViewModels.Collection> GetAsync(Guid id, CancellationToken ct)
        {
            var item = await _context.Collections.SingleOrDefaultAsync(sm => sm.Id == id, ct);

            return _mapper.Map<Collection>(item);
        }

        public async Task<ViewModels.Collection> CreateAsync(ViewModels.Collection collection, CancellationToken ct)
        {
            var collectionEntity = _mapper.Map<CollectionEntity>(collection);
            collectionEntity.Id = collectionEntity.Id != Guid.Empty ? collectionEntity.Id : Guid.NewGuid();

            _context.Collections.Add(collectionEntity);
            await _context.SaveChangesAsync(ct);
            collection = await GetAsync(collectionEntity.Id, ct);

            return collection;
        }

        public async Task<ViewModels.Collection> CopyAsync(Guid collectionId, CancellationToken ct)
        {
            var collectionEntity = await _context.Collections
                .AsNoTracking()
                .SingleOrDefaultAsync(m => m.Id == collectionId);
            if (collectionEntity == null)
                throw new EntityNotFoundException<CollectionEntity>("Collection not found with ID=" + collectionId.ToString());

            var cards = await _context.Cards
                .AsNoTracking()
                .Where(c => c.CollectionId == collectionId)
                .ToListAsync(ct);
            var articles = await _context.Articles
                .AsNoTracking()
                .Where(c => c.CollectionId == collectionId)
                .ToListAsync(ct);
            var newCollectionEntity = await privateCollectionCopyAsync(collectionEntity, cards, articles, ct);
            var collection = _mapper.Map<Collection>(newCollectionEntity);

            return collection;
        }

        private async Task<CollectionEntity> privateCollectionCopyAsync(
            CollectionEntity collectionEntity,
            List<CardEntity> cards,
            List<ArticleEntity> articles,
            CancellationToken ct)
        {
            var currentUserId = _user.GetId();
            var username = (await _context.Users.SingleOrDefaultAsync(u => u.Id == _user.GetId())).Name;
            var oldCollectionId = collectionEntity.Id;
            var newCollectionId = Guid.NewGuid();
            var dateCreated = DateTime.UtcNow;
            collectionEntity.Id = newCollectionId;
            collectionEntity.DateCreated = dateCreated;
            collectionEntity.CreatedBy = currentUserId;
            collectionEntity.DateModified = collectionEntity.DateCreated;
            collectionEntity.ModifiedBy = collectionEntity.CreatedBy;
            collectionEntity.Name = collectionEntity.Name + " - " + username;
            await _context.Collections.AddAsync(collectionEntity, ct);
            // copy cards
            var newCardIds = new Dictionary<Guid, Guid>();
            foreach (var cardEntity in cards)
            {
                newCardIds[cardEntity.Id] = Guid.NewGuid();
                cardEntity.Id = newCardIds[cardEntity.Id];
                cardEntity.CollectionId = collectionEntity.Id;
                cardEntity.Collection = null;
                cardEntity.DateCreated = collectionEntity.DateCreated;
                cardEntity.CreatedBy = collectionEntity.CreatedBy;
                 await _context.Cards.AddAsync(cardEntity, ct);
            }
            // copy articles
            foreach (var articleEntity in articles)
            {
                articleEntity.Id = Guid.NewGuid();
                articleEntity.CollectionId = newCollectionId;
                articleEntity.Collection = null;
                articleEntity.CardId = articleEntity.CardId == null ? null : newCardIds[(Guid)articleEntity.CardId];
                articleEntity.Card = null;
                articleEntity.DateCreated = collectionEntity.DateCreated;
                articleEntity.CreatedBy = collectionEntity.CreatedBy;
                await _context.Articles.AddAsync(articleEntity, ct);
            }
            await _context.SaveChangesAsync(ct);

            // get the new Collection to return
            collectionEntity = await _context.Collections
                .SingleOrDefaultAsync(sm => sm.Id == newCollectionId, ct);

            return collectionEntity;
        }

        public async Task<Tuple<MemoryStream, string>> DownloadJsonAsync(Guid collectionId, CancellationToken ct)
        {
            var collection = await _context.Collections
                .SingleOrDefaultAsync(sm => sm.Id == collectionId, ct);
            if (collection == null)
            {
                throw new EntityNotFoundException<CollectionEntity>("Collection not found " + collectionId);
            }
            // get the cards
            var cards = await _context.Cards
                .AsNoTracking()
                .Where(c => c.CollectionId == collectionId)
                .ToListAsync(ct);
            //get the articles
            var articles = await _context.Articles
                .AsNoTracking()
                .Where(c => c.CollectionId == collectionId)
                .ToListAsync(ct);
            // create the whole object
            var collectionFileObject = new CollectionFileFormat(){
                Collection = collection,
                Cards = cards,
                Articles = articles
            };
            var collectionFileJson = "";
            var options = new JsonSerializerOptions()
            {
                ReferenceHandler = ReferenceHandler.Preserve
            };
            collectionFileJson = JsonSerializer.Serialize(collectionFileObject, options);
            // convert string to stream
            byte[] byteArray = Encoding.ASCII.GetBytes(collectionFileJson);
            MemoryStream memoryStream = new MemoryStream(byteArray);
            var filename = collection.Description.ToLower().EndsWith(".json") ? collection.Description : collection.Description + ".json";

            return System.Tuple.Create(memoryStream, filename);
        }

        public async Task<Collection> UploadJsonAsync(FileForm form, CancellationToken ct)
        {
            var uploadItem = form.ToUpload;
            var collectionJson = "";
            using (StreamReader reader = new StreamReader(uploadItem.OpenReadStream()))
            {
                // convert stream to string
                collectionJson = reader.ReadToEnd();
            }
            var options = new JsonSerializerOptions()
            {
                ReferenceHandler = ReferenceHandler.Preserve
            };
            var collectionFileObject = JsonSerializer.Deserialize<CollectionFileFormat>(collectionJson, options);
            // make a copy and add it to the database
            var collectionEntity = await privateCollectionCopyAsync(collectionFileObject.Collection, collectionFileObject.Cards, collectionFileObject.Articles, ct);

            return _mapper.Map<Collection>(collectionEntity);
        }

        public async Task<ViewModels.Collection> UpdateAsync(Guid id, ViewModels.Collection collection, CancellationToken ct)
        {
            var collectionToUpdate = await _context.Collections.SingleOrDefaultAsync(v => v.Id == id, ct);
            if (collectionToUpdate == null)
                throw new EntityNotFoundException<Collection>();

            _mapper.Map(collection, collectionToUpdate);

            _context.Collections.Update(collectionToUpdate);
            await _context.SaveChangesAsync(ct);

            collection = await GetAsync(collectionToUpdate.Id, ct);

            return collection;
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
        {
            var collectionToDelete = await _context.Collections.SingleOrDefaultAsync(v => v.Id == id, ct);
            if (collectionToDelete == null)
                throw new EntityNotFoundException<Collection>();

            _context.Collections.Remove(collectionToDelete);
            await _context.SaveChangesAsync(ct);

            return true;
        }

    }

    class CollectionFileFormat
    {
        public CollectionEntity Collection { get; set; }
        public List<CardEntity> Cards { get; set; }
        public List<ArticleEntity> Articles { get; set; }
    }
}
