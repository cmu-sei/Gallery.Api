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
    public class CollectionPermissionRequirement : IAuthorizationRequirement
    {
        public CollectionPermission[] RequiredPermissions;
        public Guid ExhibitId;

        public CollectionPermissionRequirement(
            CollectionPermission[] requiredPermissions,
            Guid projectId)
        {
            RequiredPermissions = requiredPermissions;
            ExhibitId = projectId;
        }
    }

    public class CollectionPermissionHandler : AuthorizationHandler<CollectionPermissionRequirement>, IAuthorizationHandler
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, CollectionPermissionRequirement requirement)
        {
            if (context.User == null)
            {
                context.Fail();
            }
            else
            {
                CollectionPermissionClaim collectionPermissionsClaim = null;

                var claims = context.User.Claims
                    .Where(x => x.Type == AuthorizationConstants.CollectionPermissionClaimType)
                    .ToList();

                foreach (var claim in claims)
                {
                    var claimValue = CollectionPermissionClaim.FromString(claim.Value);
                    if (claimValue.CollectionId == requirement.ExhibitId)
                    {
                        collectionPermissionsClaim = claimValue;
                        break;
                    }
                }

                if (collectionPermissionsClaim == null)
                {
                    context.Fail();
                }
                else if (requirement.RequiredPermissions == null || requirement.RequiredPermissions.Length == 0)
                {
                    context.Succeed(requirement);
                }
                else if (requirement.RequiredPermissions.Any(x => collectionPermissionsClaim.Permissions.Contains(x)))
                {
                    context.Succeed(requirement);
                }
            }

            return Task.CompletedTask;
        }
    }
}
