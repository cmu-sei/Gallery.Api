// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;

namespace Api.Infrastructure.Authorization
{
    public class ExhibitUserRequirement : IAuthorizationRequirement
    {
        public readonly Guid ExhibitId;

        public ExhibitUserRequirement(Guid exhibitId)
        {
            ExhibitId = exhibitId;
        }
    }

    public class ExhibitUserHandler : AuthorizationHandler<ExhibitUserRequirement>, IAuthorizationHandler
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ExhibitUserRequirement requirement)
        {
            if (context.User.HasClaim(c => c.Type == UserClaimTypes.SystemAdmin.ToString()) ||
                context.User.HasClaim(c => c.Type == UserClaimTypes.ContentDeveloper.ToString()) ||
                (
                    context.User.HasClaim(c =>
                        c.Type == UserClaimTypes.ExhibitUser.ToString() &&
                        c.Value.Contains(requirement.ExhibitId.ToString())
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

