// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Gallery.Api.Data;
using Gallery.Api.Data.Models;
using Gallery.Api.Services;
using Gallery.Api.Hubs;
using Gallery.Api.Infrastructure.Authorization;

namespace Gallery.Api.Infrastructure.EventHandlers
{
    public class BaseUserPermissionHandler
    {
        protected readonly GalleryDbContext _db;
        protected readonly IMapper _mapper;
        protected readonly IHubContext<MainHub> _mainHub;

        public BaseUserPermissionHandler(
            GalleryDbContext db,
            IMapper mapper,
            IUserService userService,
            IHubContext<MainHub> mainHub)
        {
            _db = db;
            _mapper = mapper;
            _mainHub = mainHub;
        }

        protected async Task<Guid[]> GetGroups(UserPermissionEntity userPermissionEntity, CancellationToken cancellationToken)
        {
            var groupIds = new List<Guid>();
            var systemAdminPermissionId = (await _db.Permissions.Where(p => p.Key == UserClaimTypes.SystemAdmin.ToString()).FirstOrDefaultAsync()).Id;
            groupIds.Add(systemAdminPermissionId);
            groupIds.Add(userPermissionEntity.UserId);

            return groupIds.ToArray();
        }

        protected async Task HandleChange(
            UserPermissionEntity userPermissionEntity,
            string method,
            string[] modifiedProperties,
            CancellationToken cancellationToken)
        {
            var groupIds = await this.GetGroups(userPermissionEntity, cancellationToken);
            var tasks = new List<Task>();
            var user = await _db.Users
                .ProjectTo<ViewModels.User>(_mapper.ConfigurationProvider, dest => dest.Permissions)
                .FirstAsync(u => u.Id == userPermissionEntity.UserId);

            foreach (var groupId in groupIds)
            {
                tasks.Add(_mainHub.Clients.Group(groupId.ToString()).SendAsync(method, user, modifiedProperties, cancellationToken));
            }

            await Task.WhenAll(tasks);
        }
    }

    public class UserPermissionCreatedSignalRHandler : BaseUserPermissionHandler, INotificationHandler<EntityCreated<UserPermissionEntity>>
    {
        public UserPermissionCreatedSignalRHandler(
            GalleryDbContext db,
            IMapper mapper,
            IUserService userPermissionService,
            IHubContext<MainHub> mainHub) : base(db, mapper, userPermissionService, mainHub) { }

        public async Task Handle(EntityCreated<UserPermissionEntity> notification, CancellationToken cancellationToken)
        {
            await base.HandleChange(notification.Entity, MainHubMethods.UserUpdated, null, cancellationToken);
        }
    }

    public class UserPermissionDeletedSignalRHandler : BaseUserPermissionHandler, INotificationHandler<EntityDeleted<UserPermissionEntity>>
    {
        public UserPermissionDeletedSignalRHandler(
            GalleryDbContext db,
            IMapper mapper,
            IUserService userPermissionService,
            IHubContext<MainHub> mainHub) : base(db, mapper, userPermissionService, mainHub)
        {
        }

        public async Task Handle(EntityDeleted<UserPermissionEntity> notification, CancellationToken cancellationToken)
        {
            await base.HandleChange(notification.Entity, MainHubMethods.UserUpdated, null, cancellationToken);
        }
    }
}
