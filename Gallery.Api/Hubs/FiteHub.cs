// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Threading;
using System.Threading.Tasks;
using Gallery.Api.Data;
using Microsoft.Extensions.Logging;

namespace Gallery.Api.Hubs
{
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class CiteHub : Hub
    {
        private readonly GalleryDbContext _context;
        private readonly CancellationToken _ct;
        private readonly ILogger<CiteHub> _logger;

        public CiteHub(
            GalleryDbContext context,
            ILogger<CiteHub> logger
        )
        {
            _context = context;
            CancellationTokenSource source = new CancellationTokenSource();
            _ct = source.Token;
            _logger = logger;
        }

        public async Task Join()
        {
            // add to personal user group
            var userId = Context.User.Identities.First().Claims.First(c => c.Type == "sub")?.Value;
            await Groups.AddToGroupAsync(Context.ConnectionId, userId + CiteHubMethods.GroupNameSuffix);
            _logger.LogDebug("CiteHub.Join: complete");
        }

        public async Task Leave()
        {
            var userId = Context.User.Identities.First().Claims.First(c => c.Type == "sub")?.Value;
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId + CiteHubMethods.GroupNameSuffix);
        }

    }

    public static class CiteHubMethods
    {
        public const string UnreadCountUpdated = "UnreadCountUpdated";
        public const string GroupNameSuffix = "-cite";
    }
}
