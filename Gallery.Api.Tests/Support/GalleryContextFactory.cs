// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

// gallery.api: GalleryDbContext takes only its options, and production adds no interceptor besides
// EntityEventInterceptor.

using Crucible.Common.EntityEvents.Interceptors;
using Gallery.Api.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Gallery.Api.Tests.Support;

/// <summary>Builds <see cref="GalleryDbContext"/> instances wired the way production wires them.</summary>
/// <remarks>
/// <see cref="GalleryDbContext"/> extends <c>EventPublishingDbContext</c>, whose <c>PublishEventsAsync</c>
/// resolves <see cref="IMediator"/> and a logger off the settable <c>ServiceProvider</c> property with
/// <c>GetRequiredService</c>. Both must be registered or the first event-publishing save throws.
/// </remarks>
internal static class GalleryContextFactory
{
    /// <summary>
    /// The provider a session shares across its contexts, and the substituted mediator tests assert on.
    /// A substitute is right here: each session gets its own, and only its own test reads it.
    /// </summary>
    public static (IServiceProvider Services, IMediator Mediator) CreateServices()
    {
        var mediator = Substitute.For<IMediator>();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(mediator);

        return (services.BuildServiceProvider(), mediator);
    }

    /// <summary>
    /// A context over the given provider configuration, with the entity event interceptor attached so
    /// SaveChanges publishes events exactly as it does in production.
    /// </summary>
    public static GalleryDbContext CreateContext(
        Action<DbContextOptionsBuilder<GalleryDbContext>> configureProvider,
        IServiceProvider services)
    {
        var builder = new DbContextOptionsBuilder<GalleryDbContext>();
        configureProvider(builder);
        builder.AddInterceptors(new EntityEventInterceptor(NullLogger<EntityEventInterceptor>.Instance));

        return new GalleryDbContext(builder.Options) { ServiceProvider = services };
    }
}
