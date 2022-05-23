// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Api.Infrastructure.Authorization
{
    public class OperatorRequirement : IAuthorizationRequirement
    {
        public OperatorRequirement()
        {
        }
    }

    public class OperatorHandler : AuthorizationHandler<OperatorRequirement>, IAuthorizationHandler
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, OperatorRequirement requirement)
        {
            if (context.User.HasClaim(c => c.Type == UserClaimTypes.SystemAdmin.ToString()) ||
                context.User.HasClaim(c => c.Type == UserClaimTypes.ContentDeveloper.ToString()) ||
                context.User.HasClaim(c => c.Type == UserClaimTypes.Operator.ToString()))
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}

