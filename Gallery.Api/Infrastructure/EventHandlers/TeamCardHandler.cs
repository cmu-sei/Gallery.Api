// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

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
using Gallery.Api.Infrastructure.Extensions;

namespace Gallery.Api.Infrastructure.EventHandlers
{
    public class TeamCardHandler
    {
        protected readonly GalleryDbContext _db;
        protected readonly IMapper _mapper;
        protected readonly ITeamCardService _TeamCardService;
        protected readonly IHubContext<MainHub> _mainHub;

        public TeamCardHandler(
            GalleryDbContext db,
            IMapper mapper,
            ITeamCardService TeamCardService,
            IHubContext<MainHub> mainHub)
        {
            _db = db;
            _mapper = mapper;
            _TeamCardService = TeamCardService;
            _mainHub = mainHub;
        }

        protected async Task<string[]> GetGroups(TeamCardEntity teamCardEntity, CancellationToken cancellationToken)
        {
            var groupIds = new List<string>();
            groupIds.Add(teamCardEntity.Id.ToString());
            groupIds.Add(MainHub.EXHIBIT_GROUP);
            // add this teamCard's users
            var exhibitUserIdList = await _db.TeamUsers
                .Where(tu => tu.TeamId == teamCardEntity.TeamId)
                .Select(tu => tu.UserId)
                .ToListAsync();
            foreach (var exhibitUserId in exhibitUserIdList)
            {
                groupIds.Add(exhibitUserId.ToString());
            }

            return groupIds.ToArray();
        }

        protected async Task HandleCreateOrUpdate(
            TeamCardEntity teamCardEntity,
            string method,
            string[] modifiedProperties,
            CancellationToken cancellationToken)
        {
            var groupIds = await this.GetGroups(teamCardEntity, cancellationToken);
            var teamCard = _mapper.Map<ViewModels.TeamCard>(teamCardEntity);
            var tasks = new List<Task>();

            foreach (var groupId in groupIds)
            {
                tasks.Add(_mainHub.Clients.Group(groupId.ToString()).SendAsync(method, teamCard, modifiedProperties, cancellationToken));
            }

            await Task.WhenAll(tasks);
        }
    }

    public class TeamCardCreatedSignalRHandler : TeamCardHandler, INotificationHandler<EntityCreated<TeamCardEntity>>
    {
        public TeamCardCreatedSignalRHandler(
            GalleryDbContext db,
            IMapper mapper,
            ITeamCardService teamCardService,
            IHubContext<MainHub> mainHub) : base(db, mapper, teamCardService, mainHub) { }

        public async Task Handle(EntityCreated<TeamCardEntity> notification, CancellationToken cancellationToken)
        {
            await base.HandleCreateOrUpdate(notification.Entity, MainHubMethods.TeamCardCreated, null, cancellationToken);
        }
    }

    public class TeamCardUpdatedSignalRHandler : TeamCardHandler, INotificationHandler<EntityUpdated<TeamCardEntity>>
    {
        public TeamCardUpdatedSignalRHandler(
            GalleryDbContext db,
            IMapper mapper,
            ITeamCardService teamCardService,
            IHubContext<MainHub> mainHub) : base(db, mapper, teamCardService, mainHub) { }

        public async Task Handle(EntityUpdated<TeamCardEntity> notification, CancellationToken cancellationToken)
        {
            await base.HandleCreateOrUpdate(
                notification.Entity,
                MainHubMethods.TeamCardUpdated,
                notification.ModifiedProperties.Select(x => x.TitleCaseToCamelCase()).ToArray(),
                cancellationToken);
        }
    }

    public class TeamCardDeletedSignalRHandler : TeamCardHandler, INotificationHandler<EntityDeleted<TeamCardEntity>>
    {
        public TeamCardDeletedSignalRHandler(
            GalleryDbContext db,
            IMapper mapper,
            ITeamCardService teamCardService,
            IHubContext<MainHub> mainHub) : base(db, mapper, teamCardService, mainHub)
        {
        }

        public async Task Handle(EntityDeleted<TeamCardEntity> notification, CancellationToken cancellationToken)
        {
            var groupIds = await base.GetGroups(notification.Entity, cancellationToken);
            var tasks = new List<Task>();

            foreach (var groupId in groupIds)
            {
                tasks.Add(_mainHub.Clients.Group(groupId).SendAsync(MainHubMethods.TeamCardDeleted, notification.Entity.Id, cancellationToken));
            }

            await Task.WhenAll(tasks);
        }
    }
}
