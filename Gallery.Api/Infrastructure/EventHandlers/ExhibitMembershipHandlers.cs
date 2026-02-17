// Copyright 2024 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Gallery.Api.Data.Models;
using Gallery.Api.Hubs;
using Crucible.Common.EntityEvents.Events;

namespace Gallery.Api.Infrastructure.EventHandlers
{
    public class ExhibitMembershipCreatedSignalRHandler : INotificationHandler<EntityCreated<ExhibitMembershipEntity>>
    {
        private readonly IHubContext<MainHub> _mainHub;
        private readonly IMapper _mapper;

        public ExhibitMembershipCreatedSignalRHandler(
            IHubContext<MainHub> mainHub,
            IMapper mapper)
        {
            _mainHub = mainHub;
            _mapper = mapper;
        }

        public async Task Handle(EntityCreated<ExhibitMembershipEntity> notification, CancellationToken cancellationToken)
        {
            var scenarioMembership = _mapper.Map<ViewModels.ExhibitMembership>(notification.Entity);
            await _mainHub.Clients
                .Groups(MainHub.EXHIBIT_GROUP)
                .SendAsync(MainHubMethods.ExhibitMembershipCreated, scenarioMembership);
        }
    }

    public class ExhibitMembershipDeletedSignalRHandler : INotificationHandler<EntityDeleted<ExhibitMembershipEntity>>
    {
        private readonly IHubContext<MainHub> _mainHub;

        public ExhibitMembershipDeletedSignalRHandler(
            IHubContext<MainHub> mainHub)
        {
            _mainHub = mainHub;
        }

        public async Task Handle(EntityDeleted<ExhibitMembershipEntity> notification, CancellationToken cancellationToken)
        {
            await _mainHub.Clients
                .Groups(MainHub.EXHIBIT_GROUP)
                .SendAsync(MainHubMethods.ExhibitMembershipDeleted, notification.Entity.Id);
        }
    }

    public class ExhibitMembershipUpdatedSignalRHandler : INotificationHandler<EntityUpdated<ExhibitMembershipEntity>>
    {
        private readonly IHubContext<MainHub> _mainHub;
        private readonly IMapper _mapper;

        public ExhibitMembershipUpdatedSignalRHandler(
            IHubContext<MainHub> mainHub,
            IMapper mapper)
        {
            _mainHub = mainHub;
            _mapper = mapper;
        }

        public async Task Handle(EntityUpdated<ExhibitMembershipEntity> notification, CancellationToken cancellationToken)
        {
            var scenarioMembership = _mapper.Map<ViewModels.ExhibitMembership>(notification.Entity);
            await _mainHub.Clients
                .Groups(MainHub.EXHIBIT_GROUP)
                .SendAsync(MainHubMethods.ExhibitMembershipUpdated, scenarioMembership);
        }
    }
}
