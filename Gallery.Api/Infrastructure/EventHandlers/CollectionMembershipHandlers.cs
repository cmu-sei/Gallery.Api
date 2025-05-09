// Copyright 2024 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Gallery.Api.Data.Models;
using Gallery.Api.Hubs;

namespace Gallery.Api.Infrastructure.EventHandlers
{
    public class CollectionMembershipCreatedSignalRHandler : INotificationHandler<EntityCreated<CollectionMembershipEntity>>
    {
        private readonly IHubContext<MainHub> _mainHub;
        private readonly IMapper _mapper;

        public CollectionMembershipCreatedSignalRHandler(
            IHubContext<MainHub> mainHub,
            IMapper mapper)
        {
            _mainHub = mainHub;
            _mapper = mapper;
        }

        public async Task Handle(EntityCreated<CollectionMembershipEntity> notification, CancellationToken cancellationToken)
        {
            var scenarioTemplateMembership = _mapper.Map<ViewModels.CollectionMembership>(notification.Entity);
            await _mainHub.Clients
                .Groups(MainHub.COLLECTION_GROUP)
                .SendAsync(MainHubMethods.CollectionMembershipCreated, scenarioTemplateMembership);
        }
    }

    public class CollectionMembershipDeletedSignalRHandler : INotificationHandler<EntityDeleted<CollectionMembershipEntity>>
    {
        private readonly IHubContext<MainHub> _mainHub;

        public CollectionMembershipDeletedSignalRHandler(
            IHubContext<MainHub> mainHub)
        {
            _mainHub = mainHub;
        }

        public async Task Handle(EntityDeleted<CollectionMembershipEntity> notification, CancellationToken cancellationToken)
        {
            await _mainHub.Clients
                .Groups(MainHub.COLLECTION_GROUP)
                .SendAsync(MainHubMethods.CollectionMembershipDeleted, notification.Entity.Id);
        }
    }

    public class CollectionMembershipUpdatedSignalRHandler : INotificationHandler<EntityUpdated<CollectionMembershipEntity>>
    {
        private readonly IHubContext<MainHub> _mainHub;
        private readonly IMapper _mapper;

        public CollectionMembershipUpdatedSignalRHandler(
            IHubContext<MainHub> mainHub,
            IMapper mapper)
        {
            _mainHub = mainHub;
            _mapper = mapper;
        }

        public async Task Handle(EntityUpdated<CollectionMembershipEntity> notification, CancellationToken cancellationToken)
        {
            var scenarioTemplateMembership = _mapper.Map<ViewModels.CollectionMembership>(notification.Entity);
            await _mainHub.Clients
                .Groups(MainHub.COLLECTION_GROUP)
                .SendAsync(MainHubMethods.CollectionMembershipUpdated, scenarioTemplateMembership);
        }
    }
}
