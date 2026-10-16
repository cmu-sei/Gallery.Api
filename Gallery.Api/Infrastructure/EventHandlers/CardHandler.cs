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
using Gallery.Api.Infrastructure.Extensions;
using Crucible.Common.EntityEvents.Events;

namespace Gallery.Api.Infrastructure.EventHandlers
{
    public class CardHandler
    {
        protected readonly GalleryDbContext _db;
        protected readonly IMapper _mapper;
        protected readonly ICardService _CardService;
        protected readonly IHubContext<MainHub> _mainHub;

        public CardHandler(
            GalleryDbContext db,
            IMapper mapper,
            ICardService CardService,
            IHubContext<MainHub> mainHub)
        {
            _db = db;
            _mapper = mapper;
            _CardService = CardService;
            _mainHub = mainHub;
        }

        /// <summary>
        /// The groups a card event goes to. A card's content goes only to the participants of a team the card is
        /// assigned to, and to the observers of that team's exhibit, who can see every team.
        /// <paramref name="toEveryParticipant"/> is for a deletion, which carries only the card's id: its TeamCards
        /// are gone by then, so it goes to every participant of an exhibit on the collection.
        /// </summary>
        protected async Task<string[]> GetGroups(CardEntity cardEntity, CancellationToken cancellationToken, bool toEveryParticipant = false)
        {
            var groupIds = new List<string>();
            groupIds.Add(cardEntity.Id.ToString());
            // add this card's users
            var exhibitIdList = (IQueryable<Guid>)_db.Exhibits
                .Where(e => e.CollectionId == cardEntity.CollectionId)
                .Select(e => e.Id);
            IQueryable<TeamUserEntity> recipients;
            if (toEveryParticipant)
            {
                var teamIdList = _db.Teams
                    .Where(t => t.ExhibitId != null && exhibitIdList.Contains((Guid)t.ExhibitId))
                    .Select(t => t.Id);
                recipients = _db.TeamUsers.Where(tu => teamIdList.Contains(tu.TeamId));
            }
            else
            {
                var assignedTeamIdList = _db.TeamCards
                    .Where(tc => tc.CardId == cardEntity.Id)
                    .Select(tc => tc.TeamId);
                var assignedExhibitIdList = _db.TeamCards
                    .Where(tc => tc.CardId == cardEntity.Id)
                    .Select(tc => tc.Team.ExhibitId);
                recipients = _db.TeamUsers.Where(tu =>
                    assignedTeamIdList.Contains(tu.TeamId) ||
                    (tu.IsObserver && assignedExhibitIdList.Contains(tu.Team.ExhibitId)));
            }
            // A user belongs to one team per exhibit, so a user on teams in several
            // exhibits of this collection appears once per exhibit. Without Distinct
            // the same user group is sent the same card event several times, and each
            // copy re-upserts the card and makes every Wall/Archive subscriber
            // recompute. Every recipient still receives the event, exactly once.
            var userIdList = await recipients
                .Select(tu => tu.UserId)
                .Distinct()
                .ToListAsync();
            foreach (var userId in userIdList)
            {
                groupIds.Add(userId.ToString());
            }
            groupIds.Add(MainHub.COLLECTION_GROUP);
            if (exhibitIdList.Count() > 0)
            {
                groupIds.Add(MainHub.EXHIBIT_GROUP);
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
                tasks.Add(_mainHub.Clients.Group(groupId).SendAsync(method, card, modifiedProperties, cancellationToken));
            }

            await Task.WhenAll(tasks);
        }
    }

    public class CardCreatedSignalRHandler : CardHandler, INotificationHandler<EntityCreated<CardEntity>>
    {
        public CardCreatedSignalRHandler(
            GalleryDbContext db,
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
            GalleryDbContext db,
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
            GalleryDbContext db,
            IMapper mapper,
            ICardService cardService,
            IHubContext<MainHub> mainHub) : base(db, mapper, cardService, mainHub)
        {
        }

        public async Task Handle(EntityDeleted<CardEntity> notification, CancellationToken cancellationToken)
        {
            var groupIds = await base.GetGroups(notification.Entity, cancellationToken, toEveryParticipant: true);
            var tasks = new List<Task>();

            foreach (var groupId in groupIds)
            {
                tasks.Add(_mainHub.Clients.Group(groupId.ToString()).SendAsync(MainHubMethods.CardDeleted, notification.Entity.Id, cancellationToken));
            }

            await Task.WhenAll(tasks);
        }
    }
}
