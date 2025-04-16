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
    public class UserArticleHandler
    {
        protected readonly GalleryDbContext _db;
        protected readonly IMapper _mapper;
        protected readonly IUserArticleService _UserArticleService;
        protected readonly IHubContext<MainHub> _mainHub;
        protected readonly IHubContext<CiteHub> _citeHub;

        public UserArticleHandler(
            GalleryDbContext db,
            IMapper mapper,
            IUserArticleService UserArticleService,
            IHubContext<MainHub> mainHub,
            IHubContext<CiteHub> citeHub)
        {
            _db = db;
            _mapper = mapper;
            _UserArticleService = UserArticleService;
            _mainHub = mainHub;
            _citeHub = citeHub;
        }

        protected async Task HandleCreateOrUpdate(
            UserArticleEntity userArticleEntity,
            string method,
            string[] modifiedProperties,
            CancellationToken cancellationToken)
        {
            // make sure that user should be updated with this UserArticle
            // based on the exhibit current move and inject
            userArticleEntity = await _db.UserArticles
                .Include(ua => ua.Article)
                .SingleOrDefaultAsync(ua => ua.Id == userArticleEntity.Id);
            var exhibit = await _db.Exhibits
                .SingleOrDefaultAsync(e => e.Id == userArticleEntity.ExhibitId);
            if (userArticleEntity.Article.Move < exhibit.CurrentMove ||
                (userArticleEntity.Article.Move == exhibit.CurrentMove && userArticleEntity.Article.Inject <= exhibit.CurrentInject))
            {
                var userArticle = _mapper.Map<ViewModels.UserArticle>(userArticleEntity);
                var tasks = new List<Task>();
                // Main Hub task
                tasks.Add(_mainHub.Clients.Group(userArticleEntity.UserId.ToString()).SendAsync(method, userArticle, modifiedProperties, cancellationToken));

                await Task.WhenAll(tasks);
            }
        }

        protected async Task HandleUnreadCount(
            UserArticleEntity userArticleEntity,
            CancellationToken cancellationToken)
        {
            var tasks = new List<Task>();
            var exhibit = await _db.Exhibits.FirstAsync(e => e.Id == userArticleEntity.ExhibitId);
            var unreadArticles = await _db.UserArticles
                .Where(ua =>
                    ua.UserId == userArticleEntity.UserId &&
                    ua.ExhibitId == exhibit.Id &&
                    (
                        (ua.Article.Move < exhibit.CurrentMove) ||
                        (ua.Article.Move == exhibit.CurrentMove && ua.Article.Inject <= exhibit.CurrentInject)
                    ) &&
                    !ua.IsRead
                )
                .CountAsync();
            // Cite Hub task
            tasks.Add(_citeHub.Clients.Group(userArticleEntity.UserId.ToString() + CiteHubMethods.GroupNameSuffix).SendAsync(CiteHubMethods.UnreadCountUpdated, unreadArticles, null, cancellationToken));

            await Task.WhenAll(tasks);
        }

    }

    public class UserArticleCreatedSignalRHandler : UserArticleHandler, INotificationHandler<EntityCreated<UserArticleEntity>>
    {
        public UserArticleCreatedSignalRHandler(
            GalleryDbContext db,
            IMapper mapper,
            IUserArticleService userArticleService,
            IHubContext<MainHub> mainHub,
            IHubContext<CiteHub> citeHub) : base(db, mapper, userArticleService, mainHub, citeHub) { }

        public async Task Handle(EntityCreated<UserArticleEntity> notification, CancellationToken cancellationToken)
        {
            // Main hub task
            await base.HandleCreateOrUpdate(notification.Entity, MainHubMethods.UserArticleCreated, null, cancellationToken);
            // Cite hub task
            await base.HandleUnreadCount(notification.Entity, cancellationToken);
        }
    }

    public class UserArticleUpdatedSignalRHandler : UserArticleHandler, INotificationHandler<EntityUpdated<UserArticleEntity>>
    {
        public UserArticleUpdatedSignalRHandler(
            GalleryDbContext db,
            IMapper mapper,
            IUserArticleService userArticleService,
            IHubContext<MainHub> mainHub,
            IHubContext<CiteHub> citeHub) : base(db, mapper, userArticleService, mainHub, citeHub) { }

        public async Task Handle(EntityUpdated<UserArticleEntity> notification, CancellationToken cancellationToken)
        {
            // Main hub task
            await base.HandleCreateOrUpdate(
                notification.Entity,
                MainHubMethods.UserArticleUpdated,
                notification.ModifiedProperties.Select(x => x.TitleCaseToCamelCase()).ToArray(),
                cancellationToken);
            // Cite hub task
            await base.HandleUnreadCount(notification.Entity, cancellationToken);
        }
    }

    public class UserArticleDeletedSignalRHandler : UserArticleHandler, INotificationHandler<EntityDeleted<UserArticleEntity>>
    {
        public UserArticleDeletedSignalRHandler(
            GalleryDbContext db,
            IMapper mapper,
            IUserArticleService userArticleService,
            IHubContext<MainHub> mainHub,
            IHubContext<CiteHub> citeHub) : base(db, mapper, userArticleService, mainHub, citeHub)
        {
        }

        public async Task Handle(EntityDeleted<UserArticleEntity> notification, CancellationToken cancellationToken)
        {
            var tasks = new List<Task>();

            // Main Hub task
            tasks.Add(_mainHub.Clients.Group(notification.Entity.UserId.ToString()).SendAsync(MainHubMethods.UserArticleDeleted, notification.Entity.Id, cancellationToken));
            // Cite Hub task
            await base.HandleUnreadCount(notification.Entity, cancellationToken);

            await Task.WhenAll(tasks);
        }
    }
}
