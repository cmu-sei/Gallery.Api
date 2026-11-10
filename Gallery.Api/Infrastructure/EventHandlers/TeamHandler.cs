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
using Gallery.Api.Infrastructure.Authorization;
using Gallery.Api.Infrastructure.Extensions;
using Crucible.Common.EntityEvents.Events;
using Gallery.Api.Infrastructure.Mappings;

namespace Gallery.Api.Infrastructure.EventHandlers
{
    public class TeamHandler
    {
        protected readonly GalleryDbContext _db;
        protected readonly ITeamService _TeamService;
        protected readonly IHubContext<MainHub> _mainHub;

        public TeamHandler(
            GalleryDbContext db,
            ITeamService TeamService,
            IHubContext<MainHub> mainHub)
        {
            _db = db;
            _TeamService = TeamService;
            _mainHub = mainHub;
        }

        protected async Task<string[]> GetGroups(TeamEntity teamEntity, CancellationToken cancellationToken)
        {
            var groupIds = new List<string>();
            // add the team
            groupIds.Add(teamEntity.Id.ToString());
            groupIds.Add(MainHub.EXHIBIT_GROUP);

            return groupIds.ToArray();
        }

        protected async Task HandleCreateOrUpdate(
            TeamEntity teamEntity,
            string method,
            string[] modifiedProperties,
            CancellationToken cancellationToken)
        {
            var groupIds = await this.GetGroups(teamEntity, cancellationToken);
            var team = teamEntity.ToTeam();
            var tasks = new List<Task>();

            foreach (var groupId in groupIds)
            {
                tasks.Add(_mainHub.Clients.Group(groupId.ToString()).SendAsync(method, team, modifiedProperties, cancellationToken));
            }

            await Task.WhenAll(tasks);
        }
    }

    public class TeamCreatedSignalRHandler : TeamHandler, INotificationHandler<EntityCreated<TeamEntity>>
    {
        public TeamCreatedSignalRHandler(
            GalleryDbContext db,
            ITeamService teamService,
            IHubContext<MainHub> mainHub) : base(db, teamService, mainHub) { }

        public async Task Handle(EntityCreated<TeamEntity> notification, CancellationToken cancellationToken)
        {
            await base.HandleCreateOrUpdate(notification.Entity, MainHubMethods.TeamCreated, null, cancellationToken);
        }
    }

    public class TeamUpdatedSignalRHandler : TeamHandler, INotificationHandler<EntityUpdated<TeamEntity>>
    {
        public TeamUpdatedSignalRHandler(
            GalleryDbContext db,
            ITeamService teamService,
            IHubContext<MainHub> mainHub) : base(db, teamService, mainHub) { }

        public async Task Handle(EntityUpdated<TeamEntity> notification, CancellationToken cancellationToken)
        {
            await base.HandleCreateOrUpdate(
                notification.Entity,
                MainHubMethods.TeamUpdated,
                notification.ModifiedProperties.Select(x => x.TitleCaseToCamelCase()).ToArray(),
                cancellationToken);
        }
    }

    public class TeamDeletedSignalRHandler : TeamHandler, INotificationHandler<EntityDeleted<TeamEntity>>
    {
        public TeamDeletedSignalRHandler(
            GalleryDbContext db,
            ITeamService teamService,
            IHubContext<MainHub> mainHub) : base(db, teamService, mainHub)
        {
        }

        public async Task Handle(EntityDeleted<TeamEntity> notification, CancellationToken cancellationToken)
        {
            var groupIds = await base.GetGroups(notification.Entity, cancellationToken);
            var tasks = new List<Task>();

            foreach (var groupId in groupIds)
            {
                tasks.Add(_mainHub.Clients.Group(groupId).SendAsync(MainHubMethods.TeamDeleted, notification.Entity.Id, cancellationToken));
            }

            await Task.WhenAll(tasks);
        }
    }
}
