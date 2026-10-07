// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Net;
using System.Net.Http.Json;
using Gallery.Api.Data.Enumerations;
using Gallery.Api.Tests.Support;
using Gallery.Api.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Gallery.Api.Tests.Controllers;

/// <summary>
/// <c>SystemRolesController</c> over HTTP: reads need <c>ViewRoles</c>, writes <c>ManageRoles</c>. Role names
/// are uniquely indexed, so every test names its own role.
/// </summary>
public class SystemRoleControllerTests(DatabaseFixture fixture, GalleryAppFactory factory) : ApiTestBase(fixture, factory)
{
    // ---- GetAll --------------------------------------------------------------------------------

    [Fact]
    public async Task GetAll_returns_the_seeded_roles_to_a_caller_holding_ViewRoles()
    {
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewRoles).SeedAsync();

        var roles = await ReadAsync<List<SystemRole>>(await Client(actor).GetAsync("api/system-roles", Ct));

        Assert.Contains(roles, x => x.Id == TestData.Roles.Administrator && x.Immutable);
    }

    [Fact]
    public async Task GetAll_is_forbidden_for_a_caller_holding_only_ViewGroups()
    {
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewGroups).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).GetAsync("api/system-roles", Ct));
    }

    /// <summary>A request with no identity is answered with a 401; the anonymous client is the case under test.</summary>
    [Fact]
    public async Task GetAll_without_an_identity_is_unauthorized()
    {
        await AssertStatus(HttpStatusCode.Unauthorized, await Client().GetAsync("api/system-roles", Ct));
    }

    // ---- Get -----------------------------------------------------------------------------------

    [Fact]
    public async Task Get_returns_the_role_to_a_caller_holding_ViewRoles()
    {
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewRoles).SeedAsync();

        var role = await ReadAsync<SystemRole>(
            await Client(actor).GetAsync($"api/system-roles/{TestData.Roles.ContentDeveloper}", Ct));

        Assert.Equal("Content Developer", role.Name);
    }

    [Fact]
    public async Task Get_is_forbidden_for_a_caller_holding_only_ManageGroups()
    {
        var actor = await Actor().WithSystemPermissions(SystemPermission.ManageGroups).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden,
            await Client(actor).GetAsync($"api/system-roles/{TestData.Roles.ContentDeveloper}", Ct));
    }

    // ---- Create --------------------------------------------------------------------------------

    [Fact]
    public async Task Create_persists_the_role_for_a_caller_holding_ManageRoles()
    {
        var actor = await Actor().WithSystemPermissions(SystemPermission.ManageRoles).SeedAsync();

        var response = await Client(actor).PostAsJsonAsync("api/system-roles",
            new { name = "Auditor", permissions = new[] { "ViewUsers" } }, Ct);

        await AssertStatus(HttpStatusCode.Created, response);
        var created = await ReadAsync<SystemRole>(response);
        await using var db = NewContext();
        var stored = await db.SystemRoles.SingleAsync(x => x.Id == created.Id, Ct);
        Assert.Equal("Auditor", stored.Name);
        Assert.Equal([SystemPermission.ViewUsers], stored.Permissions);
    }

    [Fact]
    public async Task Create_is_forbidden_for_a_caller_holding_only_ViewRoles()
    {
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewRoles).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden,
            await Client(actor).PostAsJsonAsync("api/system-roles", new { name = "Nope" }, Ct));
    }

    /// <summary>The unique index on the name refuses a duplicate, and the middleware maps it to a conflict.</summary>
    [Fact]
    public async Task Create_answers_a_name_already_taken_with_a_conflict()
    {
        var problem = await AssertProblem(HttpStatusCode.Conflict,
            await RootClient.PostAsJsonAsync("api/system-roles", new { name = "Administrator" }, Ct));

        Assert.Equal("A record with this identifier already exists.", problem.Title);
    }

    // ---- Update --------------------------------------------------------------------------------

    [Fact]
    public async Task Update_persists_the_change_for_a_caller_holding_ManageRoles()
    {
        var role = TestData.SystemRole("Editable");
        await Seed(role);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ManageRoles).SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).PutAsJsonAsync($"api/system-roles/{role.Id}",
            new { id = role.Id, name = "Edited", permissions = new[] { "ViewGroups" } }, Ct));

        await using var db = NewContext();
        var stored = await db.SystemRoles.SingleAsync(x => x.Id == role.Id, Ct);
        Assert.Equal("Edited", stored.Name);
        Assert.Equal([SystemPermission.ViewGroups], stored.Permissions);
    }

    [Fact]
    public async Task Update_is_forbidden_for_a_caller_holding_only_ViewRoles()
    {
        var role = TestData.SystemRole("Untouched");
        await Seed(role);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewRoles).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).PutAsJsonAsync($"api/system-roles/{role.Id}",
            new { id = role.Id, name = "Changed" }, Ct));
    }

    [Fact]
    public async Task Update_reports_an_unknown_role_as_not_found()
    {
        var id = Guid.NewGuid();

        await AssertProblem(HttpStatusCode.NotFound,
            await RootClient.PutAsJsonAsync($"api/system-roles/{id}", new { id, name = "Ghost" }, Ct));
    }

    /// <summary>The seeded, immutable Administrator role accepts an edit that takes its permissions away.</summary>
    [Fact]
    public async Task Update_rewrites_the_immutable_administrator_role()
    {
        await AssertStatus(HttpStatusCode.OK, await RootClient.PutAsJsonAsync($"api/system-roles/{TestData.Roles.Administrator}",
            new { id = TestData.Roles.Administrator, name = "Administrator", allPermissions = false, immutable = true }, Ct));

        await using var db = NewContext();
        Assert.False((await db.SystemRoles.SingleAsync(x => x.Id == TestData.Roles.Administrator, Ct)).AllPermissions);
    }

    // ---- Delete --------------------------------------------------------------------------------

    [Fact]
    public async Task Delete_removes_the_role_for_a_caller_holding_ManageRoles()
    {
        var role = TestData.SystemRole("Doomed");
        await Seed(role);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ManageRoles).SeedAsync();

        await AssertStatus(HttpStatusCode.NoContent, await Client(actor).DeleteAsync($"api/system-roles/{role.Id}", Ct));

        await using var db = NewContext();
        Assert.False(await db.SystemRoles.AnyAsync(x => x.Id == role.Id, Ct));
    }

    [Fact]
    public async Task Delete_is_forbidden_for_a_caller_holding_only_ViewRoles()
    {
        var role = TestData.SystemRole("Kept");
        await Seed(role);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewRoles).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).DeleteAsync($"api/system-roles/{role.Id}", Ct));

        await using var db = NewContext();
        Assert.True(await db.SystemRoles.AnyAsync(x => x.Id == role.Id, Ct));
    }

    [Fact]
    public async Task Delete_reports_an_unknown_role_as_not_found()
    {
        await AssertProblem(HttpStatusCode.NotFound, await RootClient.DeleteAsync($"api/system-roles/{Guid.NewGuid()}", Ct));
    }

    /// <summary>A role that a user holds is refused with a 400 naming a missing referenced entity.</summary>
    [Fact]
    public async Task Delete_of_a_role_a_user_holds_is_answered_with_a_bad_request()
    {
        var role = TestData.SystemRole("Held");
        await Seed(role, TestData.User(roleId: role.Id));

        var problem = await AssertProblem(HttpStatusCode.BadRequest,
            await RootClient.DeleteAsync($"api/system-roles/{role.Id}", Ct));

        Assert.Equal("Referenced entity does not exist. Please verify all referenced entities exist.", problem.Title);
    }
}
