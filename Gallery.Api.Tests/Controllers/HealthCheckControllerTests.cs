// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Net;
using Gallery.Api.Tests.Support;

namespace Gallery.Api.Tests.Controllers;

/// <summary>
/// The anonymous health and version routes of <c>HealthCheckController</c>. <c>Startup</c> also maps
/// health-check endpoints on the same two paths; the controller's JSON body is what callers get.
/// </summary>
public class HealthCheckControllerTests(DatabaseFixture fixture, GalleryAppFactory factory) : ApiTestBase(fixture, factory)
{
    /// <summary>The host's database check is tagged live, so a reachable database reads as healthy.</summary>
    [Fact]
    public async Task Live_reports_healthy_without_an_identity()
    {
        var response = await Client().GetAsync("api/health/live", Ct);

        Assert.Equal("Healthy", (await ReadAsync<HealthStatus>(response)).Status);
    }

    [Fact]
    public async Task Ready_reports_healthy_without_an_identity()
    {
        var response = await Client().GetAsync("api/health/ready", Ct);

        Assert.Equal("Healthy", (await ReadAsync<HealthStatus>(response)).Status);
    }

    [Fact]
    public async Task Version_returns_the_informational_version_without_an_identity()
    {
        var response = await Client().GetAsync("api/version", Ct);

        await AssertStatus(HttpStatusCode.OK, response);
        Assert.StartsWith("0.0.0", await response.Content.ReadAsStringAsync(Ct));
    }

    private sealed record HealthStatus(string Status);
}
