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
using Gallery.Api.Data;
using Gallery.Api.Data.Models;
using Gallery.Api.Services;
using Gallery.Api.Hubs;
using Gallery.Api.Infrastructure.Extensions;

namespace Gallery.Api.Infrastructure.EventHandlers
{
    public class UserHandler
    {
        protected readonly IMapper _mapper;
        protected readonly IHubContext<MainHub> _mainHub;

        public UserHandler(
            IMapper mapper,
            IHubContext<MainHub> mainHub)
        {
            _mapper = mapper;
            _mainHub = mainHub;
        }

        protected Guid[] GetGroups(UserEntity userEntity)
        {
            var groupIds = new List<Guid>();
            groupIds.Add(userEntity.CreatedBy);

            return groupIds.ToArray();
        }

        protected async Task HandleCreateOrUpdate(
            UserEntity userEntity,
            string method,
            string[] modifiedProperties,
            CancellationToken cancellationToken)
        {
            var groupIds = GetGroups(userEntity);
            var user = _mapper.Map<ViewModels.User>(userEntity);
            var tasks = new List<Task>();

            foreach (var groupId in groupIds)
            {
                tasks.Add(_mainHub.Clients.Group(groupId.ToString()).SendAsync(method, user, modifiedProperties, cancellationToken));
            }

            await Task.WhenAll(tasks);
        }
    }

    public class UserCreatedSignalRHandler : UserHandler, INotificationHandler<EntityCreated<UserEntity>>
    {
        public UserCreatedSignalRHandler(
            IMapper mapper,
            IHubContext<MainHub> mainHub) : base(mapper, mainHub) { }

        public async Task Handle(EntityCreated<UserEntity> notification, CancellationToken cancellationToken)
        {
            await base.HandleCreateOrUpdate(notification.Entity, MainHubMethods.UserCreated, null, cancellationToken);
        }
    }

    public class UserUpdatedSignalRHandler : UserHandler, INotificationHandler<EntityUpdated<UserEntity>>
    {
        public UserUpdatedSignalRHandler(
            IMapper mapper,
            IHubContext<MainHub> mainHub) : base(mapper, mainHub) { }

        public async Task Handle(EntityUpdated<UserEntity> notification, CancellationToken cancellationToken)
        {
            await base.HandleCreateOrUpdate(
                notification.Entity,
                MainHubMethods.UserUpdated,
                notification.ModifiedProperties.Select(x => x.TitleCaseToCamelCase()).ToArray(),
                cancellationToken);
        }
    }

    public class UserDeletedSignalRHandler : UserHandler, INotificationHandler<EntityDeleted<UserEntity>>
    {
        public UserDeletedSignalRHandler(
            IMapper mapper,
            IHubContext<MainHub> mainHub) : base(mapper, mainHub)
        {
        }

        public async Task Handle(EntityDeleted<UserEntity> notification, CancellationToken cancellationToken)
        {
            var groupIds = base.GetGroups(notification.Entity);
            var tasks = new List<Task>();

            foreach (var groupId in groupIds)
            {
                tasks.Add(_mainHub.Clients.Group(groupId.ToString()).SendAsync(MainHubMethods.UserDeleted, notification.Entity.Id, cancellationToken));
            }

            await Task.WhenAll(tasks);
        }
    }
}
