// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Threading;
using System.Threading.Tasks;
using Gallery.Api.Data;
using Gallery.Api.Infrastructure.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Gallery.Api.Hubs
{
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class MainHub : Hub
    {
        private readonly GalleryDbContext _context;
        private readonly CancellationToken _ct;
        private readonly ILogger<MainHub> _logger;

        public MainHub(
            GalleryDbContext context,
            ILogger<MainHub> logger
        )
        {
            _context = context;
            CancellationTokenSource source = new CancellationTokenSource();
            _ct = source.Token;
            _logger = logger;
        }

        public async Task<List<string>> GetGroupIdsAsync()
        {
            var groupIdList = new List<string>();
            // personal user group
            var userId = Context.User.Identities.First().Claims.First(c => c.Type == "sub")?.Value;
            var userIdString = Guid.Parse(userId);
            groupIdList.Add(userId);
            // system admins group
            var systemAdminPermissionId = _context.Permissions.Where(p => p.Key == UserClaimTypes.SystemAdmin.ToString()).FirstOrDefault().Id;
            if (_context.UserPermissions.Any(
                up => up.UserId == userIdString && up.PermissionId == systemAdminPermissionId))
            {
                groupIdList.Add(systemAdminPermissionId.ToString());
            }
            // add this user's exhibits
            var exhibitIdList = await _context.ExhibitTeams
                .Join(
                    _context.TeamUsers,
                    et => et.TeamId,
                    tu => tu.TeamId,
                    (et, tu) => new
                    {
                        ExhibitId = et.ExhibitId,
                        UserId = tu.UserId
                    }
                )
                .Where(n => n.UserId == userIdString)
                .Select(n => n.ExhibitId.ToString())
                .ToListAsync();
            if (exhibitIdList.Any())
            {
                groupIdList.AddRange(exhibitIdList);
            }

            return groupIdList;
        }

        public async Task Join()
        {
            var groupIdList = await GetGroupIdsAsync();
            foreach (var groupId in groupIdList)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, groupId);
            }
            _logger.LogDebug("MainHub.Join: complete");
        }

        public async Task Leave()
        {
            var groupIdList = await GetGroupIdsAsync();
            foreach (var groupId in groupIdList)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupId);
            }
        }
    }

    public static class MainHubMethods
    {
        public const string UserCreated = "UserCreated";
        public const string UserUpdated = "UserUpdated";
        public const string UserDeleted = "UserDeleted";
        public const string UserArticleCreated = "UserArticleCreated";
        public const string UserArticleUpdated = "UserArticleUpdated";
        public const string UserArticleDeleted = "UserArticleDeleted";
        public const string UserPermissionCreated = "UserPermissionCreated";
        public const string UserPermissionDeleted = "UserPermissionDeleted";
        public const string ArticleCreated = "ArticleCreated";
        public const string ArticleUpdated = "ArticleUpdated";
        public const string ArticleDeleted = "ArticleDeleted";
        public const string CardCreated = "CardCreated";
        public const string CardUpdated = "CardUpdated";
        public const string CardDeleted = "CardDeleted";
        public const string TeamCreated = "TeamCreated";
        public const string TeamUpdated = "TeamUpdated";
        public const string TeamDeleted = "TeamDeleted";
        public const string TeamCardCreated = "TeamCardCreated";
        public const string TeamCardUpdated = "TeamCardUpdated";
        public const string TeamCardDeleted = "TeamCardDeleted";
        public const string CollectionCreated = "CollectionCreated";
        public const string CollectionUpdated = "CollectionUpdated";
        public const string CollectionDeleted = "CollectionDeleted";
        public const string ExhibitCreated = "ExhibitCreated";
        public const string ExhibitUpdated = "ExhibitUpdated";
        public const string ExhibitDeleted = "ExhibitDeleted";
    }
}
