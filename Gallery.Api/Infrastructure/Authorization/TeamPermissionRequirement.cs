// Copyright 2024 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Gallery.Api.Data.Enumerations;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Gallery.Api.Infrastructure.Authorization
{
    public class TeamPermissionRequirement : IAuthorizationRequirement
    {
        public TeamPermission[] RequiredPermissions;
        public Guid TeamId;

        public TeamPermissionRequirement(
            TeamPermission[] requiredPermissions,
            Guid teamId)
        {
            RequiredPermissions = requiredPermissions;
            TeamId = teamId;
        }
    }

    public class TeamPermissionHandler : AuthorizationHandler<TeamPermissionRequirement>, IAuthorizationHandler
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, TeamPermissionRequirement requirement)
        {
            if (context.User == null)
            {
                context.Fail();
            }
            else
            {
                TeamPermissionClaim teamPermissionsClaim = null;

                var claims = context.User.Claims
                    .Where(x => x.Type == AuthorizationConstants.TeamPermissionClaimType)
                    .ToList();

                foreach (var claim in claims)
                {
                    var claimValue = TeamPermissionClaim.FromString(claim.Value);
                    if (claimValue.TeamId == requirement.TeamId)
                    {
                        teamPermissionsClaim = claimValue;
                        break;
                    }
                }

                if (teamPermissionsClaim == null)
                {
                    context.Fail();
                }
                // an empty or missing list grants nothing, even to a caller with a claim for the scope
                else if (requirement.RequiredPermissions != null && requirement.RequiredPermissions.Any(x => teamPermissionsClaim.Permissions.Contains(x)))
                {
                    context.Succeed(requirement);
                }
            }

            return Task.CompletedTask;
        }
    }
}
