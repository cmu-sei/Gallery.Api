// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Gallery.Api.Infrastructure.Authorization;

namespace Gallery.Api.Infrastructure.Extensions
{
    public static class AuthorizationPolicyExtensions
    {
        public static void AddAuthorizationPolicy(this IServiceCollection services)
        {
            services.AddAuthorization();
            services.AddSingleton<IAuthorizationHandler, FullRightsHandler>();
            services.AddSingleton<IAuthorizationHandler, ContentDeveloperHandler>();
            services.AddSingleton<IAuthorizationHandler, OperatorHandler>();
            services.AddSingleton<IAuthorizationHandler, CanIncrementIncidentHandler>();
            services.AddSingleton<IAuthorizationHandler, CanSubmitHandler>();
            services.AddSingleton<IAuthorizationHandler, CanModifyHandler>();
            services.AddSingleton<IAuthorizationHandler, BaseUserHandler>();
            services.AddSingleton<IAuthorizationHandler, ExhibitUserHandler>();
            services.AddSingleton<IAuthorizationHandler, TeamUserHandler>();
        }


    }
}
