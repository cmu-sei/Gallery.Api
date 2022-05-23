// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;

namespace Api.Infrastructure.Authorization
{
    public class TeamUserRequirement : IAuthorizationRequirement
    {
        public readonly Guid TeamId;

        public TeamUserRequirement(Guid teamId)
        {
            TeamId = teamId;
        }
    }

    public class TeamUserHandler : AuthorizationHandler<TeamUserRequirement>, IAuthorizationHandler
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, TeamUserRequirement requirement)
        {
            if (context.User.HasClaim(c => c.Type == UserClaimTypes.SystemAdmin.ToString()) ||
                context.User.HasClaim(c => c.Type == UserClaimTypes.ContentDeveloper.ToString()) ||
                (
                    context.User.HasClaim(c =>
                        c.Type == UserClaimTypes.TeamUser.ToString() &&
                        c.Value.Contains(requirement.TeamId.ToString())
                    )
                )
            )
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}

