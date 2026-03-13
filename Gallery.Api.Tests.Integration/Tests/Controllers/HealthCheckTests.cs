// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Net;
using Gallery.Api.Tests.Integration.Fixtures;
using Xunit;

namespace Gallery.Api.Tests.Integration.Tests.Controllers;

public class HealthCheckTests : IClassFixture<GalleryTestContext>
{
    private readonly HttpClient _client;

    public HealthCheckTests(GalleryTestContext factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task LiveHealthCheck_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/health/live");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ReadyHealthCheck_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/health/ready");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
