// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Net;
using Gallery.Api.Data.Enumerations;
using Gallery.Api.Tests.Support;
using Gallery.Api.ViewModels;

namespace Gallery.Api.Tests.Controllers;

/// <summary>
/// <c>CollectionRolesController</c> and <c>ExhibitRolesController</c>: the seeded membership roles, read
/// with <c>ViewRoles</c>.
/// </summary>
public class MembershipRoleControllerTests(DatabaseFixture fixture, GalleryAppFactory factory) : ApiTestBase(fixture, factory)
{
    private Task<TestActor> RoleViewer() => Actor().WithSystemPermissions(SystemPermission.ViewRoles).SeedAsync();

    private Task<TestActor> GroupViewer() => Actor().WithSystemPermissions(SystemPermission.ViewGroups).SeedAsync();

    [Fact]
    public async Task GetAllCollectionRoles_returns_the_three_seeded_roles_to_a_caller_holding_ViewRoles()
    {
        var roles = await ReadAsync<List<CollectionRole>>(await Client(await RoleViewer()).GetAsync("api/collection-roles", Ct));

        Assert.Equal(["Manager", "Member", "Observer"], roles.Select(x => x.Name).Order());
    }

    [Fact]
    public async Task GetAllCollectionRoles_is_forbidden_for_a_caller_holding_only_ViewGroups()
    {
        await AssertProblem(HttpStatusCode.Forbidden, await Client(await GroupViewer()).GetAsync("api/collection-roles", Ct));
    }

    /// <summary>A request with no identity is answered with a 401; the anonymous client is the case under test.</summary>
    [Fact]
    public async Task GetAllCollectionRoles_without_an_identity_is_unauthorized()
    {
        await AssertStatus(HttpStatusCode.Unauthorized, await Client().GetAsync("api/collection-roles", Ct));
    }

    [Fact]
    public async Task GetCollectionRole_returns_the_role_to_a_caller_holding_ViewRoles()
    {
        var role = await ReadAsync<CollectionRole>(
            await Client(await RoleViewer()).GetAsync($"api/collection-roles/{TestData.MembershipRoles.Manager}", Ct));

        Assert.True(role.AllPermissions);
    }

    [Fact]
    public async Task GetCollectionRole_is_forbidden_for_a_caller_holding_only_ViewGroups()
    {
        await AssertProblem(HttpStatusCode.Forbidden,
            await Client(await GroupViewer()).GetAsync($"api/collection-roles/{TestData.MembershipRoles.Manager}", Ct));
    }

    [Fact]
    public async Task GetCollectionRole_reports_an_unknown_role_as_not_found()
    {
        await AssertProblem(HttpStatusCode.NotFound, await RootClient.GetAsync($"api/collection-roles/{Guid.NewGuid()}", Ct));
    }

    [Fact]
    public async Task GetAllExhibitRoles_returns_the_three_seeded_roles_to_a_caller_holding_ViewRoles()
    {
        var roles = await ReadAsync<List<ExhibitRole>>(await Client(await RoleViewer()).GetAsync("api/exhibit-roles", Ct));

        Assert.Equal(["Manager", "Member", "Observer"], roles.Select(x => x.Name).Order());
    }

    [Fact]
    public async Task GetAllExhibitRoles_is_forbidden_for_a_caller_holding_only_ViewGroups()
    {
        await AssertProblem(HttpStatusCode.Forbidden, await Client(await GroupViewer()).GetAsync("api/exhibit-roles", Ct));
    }

    [Fact]
    public async Task GetExhibitRole_returns_the_role_to_a_caller_holding_ViewRoles()
    {
        var role = await ReadAsync<ExhibitRole>(
            await Client(await RoleViewer()).GetAsync($"api/exhibit-roles/{TestData.MembershipRoles.Observer}", Ct));

        Assert.Equal([ExhibitPermission.ViewExhibit], role.Permissions);
    }

    [Fact]
    public async Task GetExhibitRole_is_forbidden_for_a_caller_holding_only_ViewGroups()
    {
        await AssertProblem(HttpStatusCode.Forbidden,
            await Client(await GroupViewer()).GetAsync($"api/exhibit-roles/{TestData.MembershipRoles.Observer}", Ct));
    }

    [Fact]
    public async Task GetExhibitRole_reports_an_unknown_role_as_not_found()
    {
        await AssertProblem(HttpStatusCode.NotFound, await RootClient.GetAsync($"api/exhibit-roles/{Guid.NewGuid()}", Ct));
    }
}
