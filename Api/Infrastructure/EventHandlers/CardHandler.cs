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
    public class CardHandler
    {
        protected readonly ApiDbContext _db;
        protected readonly IMapper _mapper;
        protected readonly ICardService _CardService;
        protected readonly IHubContext<MainHub> _mainHub;

        public CardHandler(
            ApiDbContext db,
            IMapper mapper,
            ICardService CardService,
            IHubContext<MainHub> mainHub)
        {
            _db = db;
            _mapper = mapper;
            _CardService = CardService;
            _mainHub = mainHub;
        }

        protected async Task<Guid[]> GetGroups(CardEntity cardEntity, CancellationToken cancellationToken)
        {
            var groupIds = new List<Guid>();
            groupIds.Add(cardEntity.Id);
            // add System Admins
            var systemAdminPermissionId = (await _db.Permissions.Where(p => p.Key == UserClaimTypes.SystemAdmin.ToString()).FirstOrDefaultAsync()).Id;
            groupIds.Add(systemAdminPermissionId);
            // add this card's users
            var exhibitIdList = _db.Exhibits
                .Where(e => e.CollectionId == cardEntity.CollectionId)
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
            CardEntity cardEntity,
            string method,
            string[] modifiedProperties,
            CancellationToken cancellationToken)
        {
            var groupIds = await this.GetGroups(cardEntity, cancellationToken);
            var card = _mapper.Map<ViewModels.Card>(cardEntity);
            var tasks = new List<Task>();

            foreach (var groupId in groupIds)
            {
                tasks.Add(_mainHub.Clients.Group(groupId.ToString()).SendAsync(method, card, modifiedProperties, cancellationToken));
            }

            await Task.WhenAll(tasks);
        }
    }

    public class CardCreatedSignalRHandler : CardHandler, INotificationHandler<EntityCreated<CardEntity>>
    {
        public CardCreatedSignalRHandler(
            ApiDbContext db,
            IMapper mapper,
            ICardService cardService,
            IHubContext<MainHub> mainHub) : base(db, mapper, cardService, mainHub) { }

        public async Task Handle(EntityCreated<CardEntity> notification, CancellationToken cancellationToken)
        {
            await base.HandleCreateOrUpdate(notification.Entity, MainHubMethods.CardCreated, null, cancellationToken);
        }
    }

    public class CardUpdatedSignalRHandler : CardHandler, INotificationHandler<EntityUpdated<CardEntity>>
    {
        public CardUpdatedSignalRHandler(
            ApiDbContext db,
            IMapper mapper,
            ICardService cardService,
            IHubContext<MainHub> mainHub) : base(db, mapper, cardService, mainHub) { }

        public async Task Handle(EntityUpdated<CardEntity> notification, CancellationToken cancellationToken)
        {
            await base.HandleCreateOrUpdate(
                notification.Entity,
                MainHubMethods.CardUpdated,
                notification.ModifiedProperties.Select(x => x.TitleCaseToCamelCase()).ToArray(),
                cancellationToken);
        }
    }

    public class CardDeletedSignalRHandler : CardHandler, INotificationHandler<EntityDeleted<CardEntity>>
    {
        public CardDeletedSignalRHandler(
            ApiDbContext db,
            IMapper mapper,
            ICardService cardService,
            IHubContext<MainHub> mainHub) : base(db, mapper, cardService, mainHub)
        {
        }

        public async Task Handle(EntityDeleted<CardEntity> notification, CancellationToken cancellationToken)
        {
            var groupIds = await base.GetGroups(notification.Entity, cancellationToken);
            var tasks = new List<Task>();

            foreach (var groupId in groupIds)
            {
                tasks.Add(_mainHub.Clients.Group(groupId.ToString()).SendAsync(MainHubMethods.CardDeleted, notification.Entity.Id, cancellationToken));
            }

            await Task.WhenAll(tasks);
        }
    }
}
