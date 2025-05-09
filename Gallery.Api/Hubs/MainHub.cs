// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;
using Gallery.Api.Data;
using Gallery.Api.Data.Enumerations;
using Gallery.Api.Services;
using Gallery.Api.Infrastructure.Authorization;
using Gallery.Api.Infrastructure.Options;
using Gallery.Api.ViewModels;

namespace Gallery.Api.Hubs
{
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class MainHub : Hub
    {
        private readonly ITeamService _teamService;
        private readonly IExhibitService _exhibitService;
        private readonly GalleryDbContext _context;
        private readonly DatabaseOptions _options;
        private readonly CancellationToken _ct;
        private readonly IGalleryAuthorizationService _authorizationService;
        public const string EXHIBIT_GROUP = "AdminExhibitGroup";
        public const string COLLECTION_GROUP = "AdminCollectionGroup";
        public const string GROUP_GROUP = "AdminGroupGroup";
        public const string ROLE_GROUP = "AdminRoleGroup";
        public const string USER_GROUP = "AdminUserGroup";

        public MainHub(
            ITeamService teamService,
            IExhibitService exhibitService,
            GalleryDbContext context,
            DatabaseOptions options,
            IGalleryAuthorizationService authorizationService
        )
        {
            _teamService = teamService;
            _exhibitService = exhibitService;
            _context = context;
            _options = options;
            CancellationTokenSource source = new CancellationTokenSource();
            _ct = source.Token;
            _authorizationService = authorizationService;
        }

        public async Task Join()
        {
            var userId = Context.User.Identities.First().Claims.First(c => c.Type == "sub")?.Value;
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);
        }

        public async Task Leave()
        {
            var userId = Context.User.Identities.First().Claims.First(c => c.Type == "sub")?.Value;
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
        }

        public async Task SwitchTeam(Guid[] args)
        {
            if (args.Count() == 2)
            {
                var oldTeamId = args[0];
                var newTeamId = args[1];
                // leave the old team
                var idList = await GetTeamIdList(oldTeamId);
                foreach (var id in idList)
                {
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, id.ToString());
                }
                // join the new team
                idList = await GetTeamIdList(newTeamId);
                foreach (var id in idList)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, id.ToString());
                }
            }
        }

        public async Task JoinAdmin()
        {
            var idList = await GetAdminIdList();
            foreach (var id in idList)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, id.ToString());
            }
        }

        public async Task LeaveAdmin()
        {
            var idList = await GetAdminIdList();
            foreach (var id in idList)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, id.ToString());
            }
        }

        private async Task<List<string>> GetTeamIdList(Guid teamId)
        {
            var idList = new List<string>();
            // add the user's ID
            var userId = Context.User.Identities.First().Claims.First(c => c.Type == "sub")?.Value;
            // make sure that the user has access to the requested team
            var team = await _context.Teams.SingleOrDefaultAsync(t => t.Id == teamId);
            if (team != null)
            {
                var ct = new CancellationToken();
                var canSee = await _authorizationService.AuthorizeAsync<Team>(teamId, [TeamPermission.ViewTeam], ct);
                if (canSee)
                {
                    idList.Add(teamId.ToString());
                    idList.Add(team.ExhibitId.ToString());
                }
            }

            return idList;
        }

        private async Task<List<string>> GetAdminIdList()
        {
            var ct = new CancellationToken();
            var idList = new List<string>();
            var userId = Context.User.Identities.First().Claims.First(c => c.Type == "sub")?.Value;
            idList.Add(userId);
            if (await _authorizationService.AuthorizeAsync([SystemPermission.ViewExhibits], ct))
            {
                idList.Add(EXHIBIT_GROUP);
            }
            else
            {
                var exhibitIds = await _context.ExhibitMemberships
                    .Where(x => x.UserId.ToString() == userId)
                    .Select(x => x.ExhibitId)
                    .ToListAsync(ct);
                foreach (var item in exhibitIds)
                {
                    idList.Add(item.ToString());
                }
            }
            if (await _authorizationService.AuthorizeAsync([SystemPermission.ViewCollections], ct))
            {
                idList.Add(COLLECTION_GROUP);
            }
            else
            {
                var collectionIds = await _context.CollectionMemberships
                    .Where(x => x.UserId.ToString() == userId)
                    .Select(x => x.CollectionId)
                    .ToListAsync(ct);
                foreach (var item in collectionIds)
                {
                    idList.Add(item.ToString());
                }
            }
            if (await _authorizationService.AuthorizeAsync([SystemPermission.ViewGroups], ct))
            {
                idList.Add(GROUP_GROUP);
            }
            if (await _authorizationService.AuthorizeAsync([SystemPermission.ViewRoles], ct))
            {
                idList.Add(ROLE_GROUP);
            }
            if (await _authorizationService.AuthorizeAsync([SystemPermission.ViewUsers], ct))
            {
                idList.Add(USER_GROUP);
            }

            return idList;
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
        public const string GroupMembershipCreated = "GroupMembershipCreated";
        public const string GroupMembershipUpdated = "GroupMembershipUpdated";
        public const string GroupMembershipDeleted = "GroupMembershipDeleted";
        public const string CollectionMembershipCreated = "CollectionMembershipCreated";
        public const string CollectionMembershipUpdated = "CollectionMembershipUpdated";
        public const string CollectionMembershipDeleted = "CollectionMembershipDeleted";
        public const string ExhibitMembershipCreated = "ExhibitMembershipCreated";
        public const string ExhibitMembershipUpdated = "ExhibitMembershipUpdated";
        public const string ExhibitMembershipDeleted = "ExhibitMembershipDeleted";
    }
}
