// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

// gallery.api: api/system-roles needs ViewRoles, which an actor with no role lacks (403), and its POST
// creates a role, whose name is uniquely indexed; api/users lists the users for a caller holding ViewUsers.

using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;

namespace Gallery.Api.Tests.Support;

/// <summary>
/// Tests for the HTTP harness itself: a harness that routes to the wrong database or authorizes everything
/// reads as a green suite.
/// </summary>
public class HttpHarnessTests(DatabaseFixture fixture, GalleryAppFactory factory) : ApiTestBase(fixture, factory)
{
    /// <summary>A system role name, uniquely indexed, so the two concurrency probes would collide on a shared database.</summary>
    private const string SharedName = "Http Isolation Probe";

    [Fact]
    public async Task The_swagger_document_is_served()
    {
        await AssertStatus(HttpStatusCode.OK, await Client().GetAsync("/swagger/v1/swagger.json", Ct));
    }

    [Fact]
    public async Task A_request_with_no_identity_is_unauthorized()
    {
        await AssertStatus(HttpStatusCode.Unauthorized, await Client().GetAsync("api/system-roles", Ct));
    }

    /// <summary>The claims transformer ran and derived nothing, rather than the pipeline granting by default.</summary>
    [Fact]
    public async Task An_actor_with_no_permissions_is_forbidden()
    {
        var actor = await Actor().SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).GetAsync("api/system-roles", Ct));
    }

    /// <summary>The seeded administrator role becomes real claims, and the request reads this test's database.</summary>
    [Fact]
    public async Task Root_reads_what_the_test_seeded()
    {
        var response = await RootClient.GetAsync("api/users", Ct);

        var users = await ReadAsync<List<IdOnly>>(response);
        Assert.Contains(Root.Id, users.Select(x => x.Id));
    }

    [Fact]
    public Task Concurrent_tests_share_the_host_but_not_the_database_first() => CreateSharedNameResource();

    [Fact]
    public Task Concurrent_tests_share_the_host_but_not_the_database_second() => CreateSharedNameResource();

    /// <summary>No request reaches the host's own database, which only <c>InitializeDatabase</c> uses.</summary>
    [Fact]
    public async Task A_request_never_writes_to_the_host_database()
    {
        await AssertStatus(HttpStatusCode.Created, await RootClient.PostAsJsonAsync("api/system-roles", new { name = SharedName }, Ct));

        await using var host = DatabaseFixture.HostDatabase().CreateContext();
        Assert.False(await host.SystemRoles.AnyAsync(x => x.Name == SharedName, Ct));
    }

    private async Task CreateSharedNameResource()
    {
        var response = await RootClient.PostAsJsonAsync("api/system-roles", new { name = SharedName }, Ct);

        await AssertStatus(HttpStatusCode.Created, response);

        await using var context = NewContext();
        Assert.Equal(1, await context.SystemRoles.CountAsync(x => x.Name == SharedName, Ct));
    }

    private sealed record IdOnly(Guid Id);
}
