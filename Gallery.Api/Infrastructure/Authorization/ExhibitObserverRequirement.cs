// Copyright 2023 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license, please see LICENSE.md in the project root for license information or contact permission@sei.cmu.edu for full terms.

using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;

namespace Gallery.Api.Infrastructure.Authorization
{
    public class ExhibitObserverRequirement : IAuthorizationRequirement
    {
        public readonly Guid ExhibitId;

        public ExhibitObserverRequirement(Guid exhibitId)
        {
            ExhibitId = exhibitId;
        }
    }

    public class ExhibitObserverHandler : AuthorizationHandler<ExhibitObserverRequirement>, IAuthorizationHandler
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ExhibitObserverRequirement requirement)
        {
            if (context.User.HasClaim(c =>
                c.Type == UserClaimTypes.ExhibitObserver.ToString() &&
                c.Value.Contains(requirement.ExhibitId.ToString())
            ))
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}

