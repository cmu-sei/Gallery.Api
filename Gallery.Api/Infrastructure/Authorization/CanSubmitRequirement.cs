// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;

namespace Gallery.Api.Infrastructure.Authorization
{
    public class CanSubmitRequirement : IAuthorizationRequirement
    {
        public CanSubmitRequirement()
        {
        }
    }

    public class CanSubmitHandler : AuthorizationHandler<CanSubmitRequirement>, IAuthorizationHandler
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, CanSubmitRequirement requirement)
        {
            if (context.User.HasClaim(c => c.Type == UserClaimTypes.SystemAdmin.ToString()) ||
                context.User.HasClaim(c => c.Type == UserClaimTypes.CanSubmit.ToString()))
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}

