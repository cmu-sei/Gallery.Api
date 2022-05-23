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
    public class ArticleHandler
    {
        protected readonly ApiDbContext _db;
        protected readonly IMapper _mapper;
        protected readonly IArticleService _ArticleService;
        protected readonly IHubContext<MainHub> _mainHub;

        public ArticleHandler(
            ApiDbContext db,
            IMapper mapper,
            IArticleService ArticleService,
            IHubContext<MainHub> mainHub)
        {
            _db = db;
            _mapper = mapper;
            _ArticleService = ArticleService;
            _mainHub = mainHub;
        }

        protected async Task<Guid[]> GetGroups(ArticleEntity articleEntity, CancellationToken cancellationToken)
        {
            var groupIds = new List<Guid>();
            groupIds.Add(articleEntity.Id);
            // add System Admins
            var systemAdminPermissionId = (await _db.Permissions.Where(p => p.Key == UserClaimTypes.SystemAdmin.ToString()).FirstOrDefaultAsync()).Id;
            groupIds.Add(systemAdminPermissionId);
            // add this article's users
            var userIdList = await _db.UserArticles
                .Where(ua => ua.ArticleId == articleEntity.Id)
                .Select(ua => ua.UserId)
                .ToListAsync();
            foreach (var userId in userIdList)
            {
                groupIds.Add(userId);
            }

            return groupIds.ToArray();
        }

        protected async Task HandleCreateOrUpdate(
            ArticleEntity articleEntity,
            string method,
            string[] modifiedProperties,
            CancellationToken cancellationToken)
        {
            var groupIds = await this.GetGroups(articleEntity, cancellationToken);
            var article = _mapper.Map<ViewModels.Article>(articleEntity);
            var tasks = new List<Task>();

            foreach (var groupId in groupIds)
            {
                tasks.Add(_mainHub.Clients.Group(groupId.ToString()).SendAsync(method, article, modifiedProperties, cancellationToken));
            }

            await Task.WhenAll(tasks);
        }
    }

    public class ArticleCreatedSignalRHandler : ArticleHandler, INotificationHandler<EntityCreated<ArticleEntity>>
    {
        public ArticleCreatedSignalRHandler(
            ApiDbContext db,
            IMapper mapper,
            IArticleService articleService,
            IHubContext<MainHub> mainHub) : base(db, mapper, articleService, mainHub) { }

        public async Task Handle(EntityCreated<ArticleEntity> notification, CancellationToken cancellationToken)
        {
            await base.HandleCreateOrUpdate(notification.Entity, MainHubMethods.ArticleCreated, null, cancellationToken);
        }
    }

    public class ArticleUpdatedSignalRHandler : ArticleHandler, INotificationHandler<EntityUpdated<ArticleEntity>>
    {
        public ArticleUpdatedSignalRHandler(
            ApiDbContext db,
            IMapper mapper,
            IArticleService articleService,
            IHubContext<MainHub> mainHub) : base(db, mapper, articleService, mainHub) { }

        public async Task Handle(EntityUpdated<ArticleEntity> notification, CancellationToken cancellationToken)
        {
            await base.HandleCreateOrUpdate(
                notification.Entity,
                MainHubMethods.ArticleUpdated,
                notification.ModifiedProperties.Select(x => x.TitleCaseToCamelCase()).ToArray(),
                cancellationToken);
        }
    }

    public class ArticleDeletedSignalRHandler : ArticleHandler, INotificationHandler<EntityDeleted<ArticleEntity>>
    {
        public ArticleDeletedSignalRHandler(
            ApiDbContext db,
            IMapper mapper,
            IArticleService articleService,
            IHubContext<MainHub> mainHub) : base(db, mapper, articleService, mainHub)
        {
        }

        public async Task Handle(EntityDeleted<ArticleEntity> notification, CancellationToken cancellationToken)
        {
            var groupIds = await base.GetGroups(notification.Entity, cancellationToken);
            var tasks = new List<Task>();

            foreach (var groupId in groupIds)
            {
                tasks.Add(_mainHub.Clients.Group(groupId.ToString()).SendAsync(MainHubMethods.ArticleDeleted, notification.Entity.Id, cancellationToken));
            }

            await Task.WhenAll(tasks);
        }
    }
}
