// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

// gallery.api: Startup's AddAutoMapper scans the Startup assembly and applies one global convention, a
// nullable source of the destination's underlying type maps through IgnoreNullSourceValues. That resolver
// is internal to the API, so a private copy lives here.

using AutoMapper;
using AutoMapper.Internal;

namespace Gallery.Api.Tests.Support;

/// <summary>The application's real AutoMapper configuration, built without starting the application.</summary>
public static class TestMapper
{
    private static readonly Lazy<MapperConfiguration> LazyConfiguration = new(() =>
        new MapperConfiguration(cfg =>
        {
            cfg.AddMaps(typeof(Gallery.Api.Startup).Assembly);
            cfg.Internal().ForAllPropertyMaps(
                pm => pm.SourceType != null && Nullable.GetUnderlyingType(pm.SourceType) == pm.DestinationType,
                (pm, c) => c.MapFrom<object, object, object, object>(new IgnoreNullSourceValues(), pm.SourceMember.Name));
        }));

    /// <summary>The shared configuration, built once for the run.</summary>
    public static MapperConfiguration Configuration => LazyConfiguration.Value;

    /// <summary>A mapper over <see cref="Configuration"/>. Thread-safe; tests share one.</summary>
    public static IMapper Mapper => LazyMapper.Value;

    private static readonly Lazy<IMapper> LazyMapper = new(() => LazyConfiguration.Value.CreateMapper());

    /// <summary>A copy of <c>Gallery.Api.Infrastructure.Mapping.IgnoreNullSourceValues</c>, which is internal.</summary>
    private sealed class IgnoreNullSourceValues : IMemberValueResolver<object, object, object, object>
    {
        public object Resolve(object source, object destination, object sourceMember, object destinationMember, ResolutionContext context) =>
            sourceMember ?? destinationMember;
    }
}
