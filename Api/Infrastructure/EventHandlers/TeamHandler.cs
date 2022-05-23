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
using Api.Data;
using Api.Data.Models;
using Api.Services;
using Api.Hubs;
using Api.Infrastructure.Authorization;
using Api.Infrastructure.Extensions;

namespace Api.Infrastructure.EventHandlers
{
    public class TeamHandler
    {
        protected readonly ApiDbContext _db;
        protected readonly IMapper _mapper;
        protected readonly ITeamService _TeamService;
        protected readonly IHubContext<MainHub> _mainHub;

        public TeamHandler(
            ApiDbContext db,
            IMapper mapper,
            ITeamService TeamService,
            IHubContext<MainHub> mainHub)
        {
            _db = db;
            _mapper = mapper;
            _TeamService = TeamService;
            _mainHub = mainHub;
        }

        protected async Task<Guid[]> GetGroups(TeamEntity teamEntity, CancellationToken cancellationToken)
        {
            var groupIds = new List<Guid>();
            // add the team
            groupIds.Add(teamEntity.Id);
            // add System Admins
            var systemAdminPermissionId = (await _db.Permissions.Where(p => p.Key == UserClaimTypes.SystemAdmin.ToString()).FirstOrDefaultAsync()).Id;
            groupIds.Add(systemAdminPermissionId);

            return groupIds.ToArray();
        }

        protected async Task HandleCreateOrUpdate(
            TeamEntity teamEntity,
            string method,
            string[] modifiedProperties,
            CancellationToken cancellationToken)
        {
            var groupIds = await this.GetGroups(teamEntity, cancellationToken);
            var team = _mapper.Map<ViewModels.Team>(teamEntity);
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
            ApiDbContext db,
            IMapper mapper,
            ITeamService teamService,
            IHubContext<MainHub> mainHub) : base(db, mapper, teamService, mainHub) { }

        public async Task Handle(EntityCreated<TeamEntity> notification, CancellationToken cancellationToken)
        {
            await base.HandleCreateOrUpdate(notification.Entity, MainHubMethods.TeamCreated, null, cancellationToken);
        }
    }

    public class TeamUpdatedSignalRHandler : TeamHandler, INotificationHandler<EntityUpdated<TeamEntity>>
    {
        public TeamUpdatedSignalRHandler(
            ApiDbContext db,
            IMapper mapper,
            ITeamService teamService,
            IHubContext<MainHub> mainHub) : base(db, mapper, teamService, mainHub) { }

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
            ApiDbContext db,
            IMapper mapper,
            ITeamService teamService,
            IHubContext<MainHub> mainHub) : base(db, mapper, teamService, mainHub)
        {
        }

        public async Task Handle(EntityDeleted<TeamEntity> notification, CancellationToken cancellationToken)
        {
            var groupIds = await base.GetGroups(notification.Entity, cancellationToken);
            var tasks = new List<Task>();

            foreach (var groupId in groupIds)
            {
                tasks.Add(_mainHub.Clients.Group(groupId.ToString()).SendAsync(MainHubMethods.TeamDeleted, notification.Entity.Id, cancellationToken));
            }

            await Task.WhenAll(tasks);
        }
    }
}
