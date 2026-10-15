// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

// gallery.api: the four handlers AuthorizationPolicyExtensions.AddAuthorizationPolicy registers, and the
// app's AuthorizationService (IGalleryAuthorizationService) over the framework service.

using System.Security.Claims;
using Gallery.Api.Data;
using Gallery.Api.Infrastructure.Authorization;
using Gallery.Api.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Gallery.Api.Tests.Support;

/// <summary>The authorization stack wired as production wires it, for testing handlers directly.</summary>
public static class AuthorizationHarness
{
    /// <summary>The framework authorization service with the app's handlers registered.</summary>
    public static IAuthorizationService CreateFrameworkAuthorizationService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization();
        services.AddSingleton<IAuthorizationHandler, SystemPermissionHandler>();
        services.AddSingleton<IAuthorizationHandler, CollectionPermissionHandler>();
        services.AddSingleton<IAuthorizationHandler, ExhibitPermissionHandler>();
        services.AddSingleton<IAuthorizationHandler, TeamPermissionHandler>();

        return services.BuildServiceProvider().GetRequiredService<IAuthorizationService>();
    }

    /// <summary>
    /// The app's <see cref="IGalleryAuthorizationService"/> for <paramref name="user"/>, over the real
    /// handlers and <paramref name="db"/>, which it reads to resolve an exhibit's collection.
    /// </summary>
    public static IGalleryAuthorizationService CreateGalleryAuthorizationService(ClaimsPrincipal user, GalleryDbContext db)
    {
        var framework = CreateFrameworkAuthorizationService();
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = user } };

        return new AuthorizationService(framework, new IdentityResolver(accessor, framework), db);
    }

    /// <summary>
    /// Runs a requirement through a handler directly and returns the resulting context.
    /// </summary>
    public static async Task<AuthorizationHandlerContext> HandleAsync<TRequirement>(
        IAuthorizationHandler handler,
        TRequirement requirement,
        ClaimsPrincipal user,
        object resource = null)
        where TRequirement : IAuthorizationRequirement
    {
        var context = new AuthorizationHandlerContext([requirement], user, resource);
        await handler.HandleAsync(context);

        return context;
    }
}
