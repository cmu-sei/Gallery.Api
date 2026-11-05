// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Gallery.Api.Data;
using Gallery.Api.Data.Models;
using Gallery.Api.Services;
using Gallery.Api.Hubs;
using Gallery.Api.Infrastructure.Extensions;
using Crucible.Common.EntityEvents.Events;
using Gallery.Api.Infrastructure.Mappings;

namespace Gallery.Api.Infrastructure.EventHandlers
{
    public class CollectionHandler
    {
        protected readonly GalleryDbContext _db;
        protected readonly ICollectionService _CollectionService;
        protected readonly IHubContext<MainHub> _mainHub;

        public CollectionHandler(
            GalleryDbContext db,
            ICollectionService CollectionService,
            IHubContext<MainHub> mainHub)
        {
            _db = db;
            _CollectionService = CollectionService;
            _mainHub = mainHub;
        }

        protected async Task<string[]> GetGroups(CollectionEntity collectionEntity, CancellationToken cancellationToken)
        {
            var groupIds = new List<string>();
            groupIds.Add(collectionEntity.Id.ToString());
            groupIds.Add(MainHub.COLLECTION_GROUP);
            // add this collection's users
            var exhibitIdList = (IQueryable<Guid>)_db.Exhibits
                .Where(e => e.CollectionId == collectionEntity.Id)
                .Select(e => e.Id);
            var teamIdList = _db.Teams
                .Where(t => t.ExhibitId != null && exhibitIdList.Contains((Guid)t.ExhibitId))
                .Select(t => t.Id);
            var userIdList = await _db.TeamUsers
                .Where(tu => teamIdList.Contains(tu.TeamId))
                .Select(tu => tu.UserId)
                .ToListAsync();
            foreach (var userId in userIdList)
            {
                groupIds.Add(userId.ToString());
            }

            return groupIds.ToArray();
        }

        protected async Task HandleCreateOrUpdate(
            CollectionEntity collectionEntity,
            string method,
            string[] modifiedProperties,
            CancellationToken cancellationToken)
        {
            var groupIds = await this.GetGroups(collectionEntity, cancellationToken);
            var collection = collectionEntity.ToCollection();
            var tasks = new List<Task>();

            foreach (var groupId in groupIds)
            {
                tasks.Add(_mainHub.Clients.Group(groupId).SendAsync(method, collection, modifiedProperties, cancellationToken));
            }

            await Task.WhenAll(tasks);
        }
    }

    public class CollectionCreatedSignalRHandler : CollectionHandler, INotificationHandler<EntityCreated<CollectionEntity>>
    {
        public CollectionCreatedSignalRHandler(
            GalleryDbContext db,
            ICollectionService collectionService,
            IHubContext<MainHub> mainHub) : base(db, collectionService, mainHub) { }

        public async Task Handle(EntityCreated<CollectionEntity> notification, CancellationToken cancellationToken)
        {
            await base.HandleCreateOrUpdate(notification.Entity, MainHubMethods.CollectionCreated, null, cancellationToken);
        }
    }

    public class CollectionUpdatedSignalRHandler : CollectionHandler, INotificationHandler<EntityUpdated<CollectionEntity>>
    {
        public CollectionUpdatedSignalRHandler(
            GalleryDbContext db,
            ICollectionService collectionService,
            IHubContext<MainHub> mainHub) : base(db, collectionService, mainHub) { }

        public async Task Handle(EntityUpdated<CollectionEntity> notification, CancellationToken cancellationToken)
        {
            await base.HandleCreateOrUpdate(
                notification.Entity,
                MainHubMethods.CollectionUpdated,
                notification.ModifiedProperties.Select(x => x.TitleCaseToCamelCase()).ToArray(),
                cancellationToken);
        }
    }

    public class CollectionDeletedSignalRHandler : CollectionHandler, INotificationHandler<EntityDeleted<CollectionEntity>>
    {
        public CollectionDeletedSignalRHandler(
            GalleryDbContext db,
            ICollectionService collectionService,
            IHubContext<MainHub> mainHub) : base(db, collectionService, mainHub)
        {
        }

        public async Task Handle(EntityDeleted<CollectionEntity> notification, CancellationToken cancellationToken)
        {
            var groupIds = await base.GetGroups(notification.Entity, cancellationToken);
            var tasks = new List<Task>();

            foreach (var groupId in groupIds)
            {
                tasks.Add(_mainHub.Clients.Group(groupId.ToString()).SendAsync(MainHubMethods.CollectionDeleted, notification.Entity.Id, cancellationToken));
            }

            await Task.WhenAll(tasks);
        }
    }
}
