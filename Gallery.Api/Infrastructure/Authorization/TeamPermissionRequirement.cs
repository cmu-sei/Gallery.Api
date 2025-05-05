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
        public Guid ExhibitId;

        public TeamPermissionRequirement(
            TeamPermission[] requiredPermissions,
            Guid projectId)
        {
            RequiredPermissions = requiredPermissions;
            ExhibitId = projectId;
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
                    if (claimValue.TeamId == requirement.ExhibitId)
                    {
                        teamPermissionsClaim = claimValue;
                        break;
                    }
                }

                if (teamPermissionsClaim == null)
                {
                    context.Fail();
                }
                else if (requirement.RequiredPermissions == null || requirement.RequiredPermissions.Length == 0)
                {
                    context.Succeed(requirement);
                }
                else if (requirement.RequiredPermissions.Any(x => teamPermissionsClaim.Permissions.Contains(x)))
                {
                    context.Succeed(requirement);
                }
            }

            return Task.CompletedTask;
        }
    }
}
