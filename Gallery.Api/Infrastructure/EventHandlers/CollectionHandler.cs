// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Gallery.Api.Data;
using Gallery.Api.Data.Models;
using Gallery.Api.Services;
using Gallery.Api.Hubs;
using Gallery.Api.Infrastructure.Authorization;
using Gallery.Api.Infrastructure.Extensions;

namespace Gallery.Api.Infrastructure.EventHandlers
{
    public class CollectionHandler
    {
        protected readonly GalleryDbContext _db;
        protected readonly IMapper _mapper;
        protected readonly ICollectionService _CollectionService;
        protected readonly IHubContext<MainHub> _mainHub;

        public CollectionHandler(
            GalleryDbContext db,
            IMapper mapper,
            ICollectionService CollectionService,
            IHubContext<MainHub> mainHub)
        {
            _db = db;
            _mapper = mapper;
            _CollectionService = CollectionService;
            _mainHub = mainHub;
        }

        protected async Task<Guid[]> GetGroups(CollectionEntity collectionEntity, CancellationToken cancellationToken)
        {
            var groupIds = new List<Guid>();
            groupIds.Add(collectionEntity.Id);
            // add System Admins
            var systemAdminPermissionId = (await _db.Permissions.Where(p => p.Key == UserClaimTypes.SystemAdmin.ToString()).FirstOrDefaultAsync()).Id;
            groupIds.Add(systemAdminPermissionId);
            // add this collection's users
            var exhibitIdList = _db.Exhibits
                .Where(e => e.CollectionId == collectionEntity.Id)
                .Select(e => e.Id);
            var teamIdList = _db.ExhibitTeams
                .Where(et => exhibitIdList.Contains(et.ExhibitId))
                .Select(et => et.TeamId);
            var userIdList = await _db.TeamUsers
                .Where(tu => teamIdList.Contains(tu.TeamId))
                .Select(tu => tu.UserId)
                .ToListAsync();
            foreach (var userId in userIdList)
            {
                groupIds.Add(userId);
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
            var collection = _mapper.Map<ViewModels.Collection>(collectionEntity);
            var tasks = new List<Task>();

            foreach (var groupId in groupIds)
            {
                tasks.Add(_mainHub.Clients.Group(groupId.ToString()).SendAsync(method, collection, modifiedProperties, cancellationToken));
            }

            await Task.WhenAll(tasks);
        }
    }

    public class CollectionCreatedSignalRHandler : CollectionHandler, INotificationHandler<EntityCreated<CollectionEntity>>
    {
        public CollectionCreatedSignalRHandler(
            GalleryDbContext db,
            IMapper mapper,
            ICollectionService collectionService,
            IHubContext<MainHub> mainHub) : base(db, mapper, collectionService, mainHub) { }

        public async Task Handle(EntityCreated<CollectionEntity> notification, CancellationToken cancellationToken)
        {
            await base.HandleCreateOrUpdate(notification.Entity, MainHubMethods.CollectionCreated, null, cancellationToken);
        }
    }

    public class CollectionUpdatedSignalRHandler : CollectionHandler, INotificationHandler<EntityUpdated<CollectionEntity>>
    {
        public CollectionUpdatedSignalRHandler(
            GalleryDbContext db,
            IMapper mapper,
            ICollectionService collectionService,
            IHubContext<MainHub> mainHub) : base(db, mapper, collectionService, mainHub) { }

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
            IMapper mapper,
            ICollectionService collectionService,
            IHubContext<MainHub> mainHub) : base(db, mapper, collectionService, mainHub)
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
