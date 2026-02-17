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
using Crucible.Common.EntityEvents.Events;

namespace Gallery.Api.Infrastructure.EventHandlers
{
    public class ExhibitHandler
    {
        protected readonly GalleryDbContext _db;
        protected readonly IMapper _mapper;
        protected readonly IExhibitService _ExhibitService;
        protected readonly IUserArticleService _UserArticleService;
        protected readonly IHubContext<MainHub> _mainHub;
        protected readonly IHubContext<CiteHub> _citeHub;

        public ExhibitHandler(
            GalleryDbContext db,
            IMapper mapper,
            IExhibitService ExhibitService,
            IUserArticleService UserArticleService,
            IHubContext<MainHub> mainHub,
            IHubContext<CiteHub> citeHub)
        {
            _db = db;
            _mapper = mapper;
            _ExhibitService = ExhibitService;
            _UserArticleService = UserArticleService;
            _mainHub = mainHub;
            _citeHub = citeHub;
        }

        protected async Task<List<string>> GetMainGroups(ExhibitEntity exhibitEntity, CancellationToken cancellationToken)
        {
            var groupIds = new List<string>();
            groupIds.Add(exhibitEntity.Id.ToString());
            groupIds.Add(MainHub.EXHIBIT_GROUP);

            return groupIds;
        }

        protected async Task<List<UnreadArticles>> GetExhibitUnreadArticles(ExhibitEntity exhibitEntity, CancellationToken cancellationToken)
        {
            var userIds = await _db.Teams
                .Join(
                    _db.TeamUsers,
                    t => t.Id,
                    tu => tu.TeamId,
                    (et, tu) => new
                    {
                        ExhibitId = et.ExhibitId,
                        UserId = tu.UserId
                    }
                )
                .Where(r => r.ExhibitId == exhibitEntity.Id)
                .Select(r => r.UserId)
                .ToListAsync();
            var unreadArticlesList = new List<UnreadArticles>();
            foreach (var userId in userIds)
            {
                unreadArticlesList.Add(await _UserArticleService.GetUnreadCountAsync(exhibitEntity.Id, userId, cancellationToken));
            }

            return unreadArticlesList;
        }



        protected async Task HandleCreateOrUpdate(
            ExhibitEntity exhibitEntity,
            string method,
            string[] modifiedProperties,
            CancellationToken cancellationToken)
        {
            var groupIds = await this.GetMainGroups(exhibitEntity, cancellationToken);
            var exhibit = _mapper.Map<ViewModels.Exhibit>(exhibitEntity);
            var tasks = new List<Task>();
            // Main Hub tasks
            foreach (var groupId in groupIds)
            {
                tasks.Add(_mainHub.Clients.Group(groupId.ToString()).SendAsync(method, exhibit, modifiedProperties, cancellationToken));
            }

            await Task.WhenAll(tasks);
        }

        protected async Task HandleUnreadCount(
            ExhibitEntity exhibitEntity,
            CancellationToken cancellationToken)
        {
            // Cite Hub task
            var unreadArticlesList = await this.GetExhibitUnreadArticles(exhibitEntity, cancellationToken);
            var tasks = new List<Task>();
            foreach (var unreadArticles in unreadArticlesList)
            {
                tasks.Add(_citeHub.Clients.Group(unreadArticles.UserId.ToString() + CiteHubMethods.GroupNameSuffix).SendAsync(CiteHubMethods.UnreadCountUpdated, unreadArticles, null, cancellationToken));
            }

            await Task.WhenAll(tasks);
        }

    }

    public class ExhibitCreatedSignalRHandler : ExhibitHandler, INotificationHandler<EntityCreated<ExhibitEntity>>
    {
        public ExhibitCreatedSignalRHandler(
            GalleryDbContext db,
            IMapper mapper,
            IExhibitService exhibitService,
            IUserArticleService userArticleService,
            IHubContext<MainHub> mainHub,
            IHubContext<CiteHub> citeHub) : base(db, mapper, exhibitService, userArticleService, mainHub, citeHub) { }

        public async Task Handle(EntityCreated<ExhibitEntity> notification, CancellationToken cancellationToken)
        {
            await base.HandleCreateOrUpdate(notification.Entity, MainHubMethods.ExhibitCreated, null, cancellationToken);
        }
    }

    public class ExhibitUpdatedSignalRHandler : ExhibitHandler, INotificationHandler<EntityUpdated<ExhibitEntity>>
    {
        public ExhibitUpdatedSignalRHandler(
            GalleryDbContext db,
            IMapper mapper,
            IExhibitService exhibitService,
            IUserArticleService userArticleService,
            IHubContext<MainHub> mainHub,
            IHubContext<CiteHub> citeHub) : base(db, mapper, exhibitService, userArticleService, mainHub, citeHub) { }

        public async Task Handle(EntityUpdated<ExhibitEntity> notification, CancellationToken cancellationToken)
        {
            // Main hub tasks
            await base.HandleCreateOrUpdate(
                notification.Entity,
                MainHubMethods.ExhibitUpdated,
                notification.ModifiedProperties.Select(x => x.TitleCaseToCamelCase()).ToArray(),
                cancellationToken);
            // Cite hub tasks
            await base.HandleUnreadCount(notification.Entity, cancellationToken);
        }
    }

    public class ExhibitDeletedSignalRHandler : ExhibitHandler, INotificationHandler<EntityDeleted<ExhibitEntity>>
    {
        public ExhibitDeletedSignalRHandler(
            GalleryDbContext db,
            IMapper mapper,
            IExhibitService exhibitService,
            IUserArticleService userArticleService,
            IHubContext<MainHub> mainHub,
            IHubContext<CiteHub> citeHub) : base(db, mapper, exhibitService, userArticleService, mainHub, citeHub)
        {
        }

        public async Task Handle(EntityDeleted<ExhibitEntity> notification, CancellationToken cancellationToken)
        {
            var groupIds = await base.GetMainGroups(notification.Entity, cancellationToken);
            var tasks = new List<Task>();

            foreach (var groupId in groupIds)
            {
                tasks.Add(_mainHub.Clients.Group(groupId).SendAsync(MainHubMethods.ExhibitDeleted, notification.Entity.Id, cancellationToken));
            }

            await Task.WhenAll(tasks);
        }
    }
}
