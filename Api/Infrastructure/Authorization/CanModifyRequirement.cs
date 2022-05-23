// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;

namespace Api.Infrastructure.Authorization
{
    public class CanModifyRequirement : IAuthorizationRequirement
    {
        public CanModifyRequirement()
        {
        }
    }

    public class CanModifyHandler : AuthorizationHandler<CanModifyRequirement>, IAuthorizationHandler
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, CanModifyRequirement requirement)
        {
            if (context.User.HasClaim(c => c.Type == UserClaimTypes.SystemAdmin.ToString()) ||
                context.User.HasClaim(c => c.Type == UserClaimTypes.CanModify.ToString()))
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}

