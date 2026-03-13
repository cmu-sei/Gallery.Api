// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Net;
using Gallery.Api.Tests.Integration.Fixtures;
using TUnit.Core;

namespace Gallery.Api.Tests.Integration.Tests.Controllers;

[Category("Integration")]
[ClassDataSource<GalleryTestContext>(Shared = SharedType.PerTestSession)]
public class HealthCheckTests(GalleryTestContext factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Test]
    public async Task LiveHealthCheck_WhenHealthy_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/health/live");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task ReadyHealthCheck_WhenHealthy_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/health/ready");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }
}
