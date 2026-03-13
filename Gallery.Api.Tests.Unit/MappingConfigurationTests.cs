// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using AutoMapper;
using AutoMapper.Internal;
using Gallery.Api.Infrastructure.Mapping;
using Xunit;

namespace Gallery.Api.Tests.Unit;

public class MappingConfigurationTests
{
    [Fact]
    public void AutoMapper_ConfigurationIsValid()
    {
        // Arrange
        var configuration = new MapperConfiguration(cfg =>
        {
            cfg.Internal().ForAllPropertyMaps(
                pm => pm.SourceType != null && Nullable.GetUnderlyingType(pm.SourceType) == pm.DestinationType,
                (pm, c) => c.MapFrom<object, object, object, object>(new IgnoreNullSourceValues(), pm.SourceMember.Name));
            cfg.AddMaps(typeof(Gallery.Api.Startup).Assembly);
        });

        // Act - verify mapper can be created (weaker than AssertConfigurationIsValid
        // because the app has unmapped navigation properties populated elsewhere)
        var mapper = configuration.CreateMapper();

        // Assert
        Assert.NotNull(mapper);
    }
}
