// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Net;
using System.Net.Http.Json;
using Gallery.Api.Data.Enumerations;
using Gallery.Api.Tests.Support;
using Gallery.Api.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Gallery.Api.Tests.Controllers;

/// <summary><c>GroupController</c> over HTTP: reads need <c>ViewGroups</c>, writes <c>ManageGroups</c>.</summary>
public class GroupControllerTests(DatabaseFixture fixture, GalleryAppFactory factory) : ApiTestBase(fixture, factory)
{
    private Task<TestActor> Viewer() => Actor().WithSystemPermissions(SystemPermission.ViewGroups).SeedAsync();

    private Task<TestActor> Manager() => Actor().WithSystemPermissions(SystemPermission.ManageGroups).SeedAsync();

    /// <summary>The near miss of every group gate: the role permissions sit next to the group ones.</summary>
    private Task<TestActor> RoleViewer() => Actor().WithSystemPermissions(SystemPermission.ViewRoles).SeedAsync();

    // ---- Groups --------------------------------------------------------------------------------

    [Fact]
    public async Task GetAll_returns_the_groups_to_a_caller_holding_ViewGroups()
    {
        var group = TestData.Group("Listed");
        await Seed(group);

        var groups = await ReadAsync<List<Group>>(await Client(await Viewer()).GetAsync("api/groups", Ct));

        Assert.Contains(groups, x => x.Id == group.Id);
    }

    [Fact]
    public async Task GetAll_is_forbidden_for_a_caller_holding_only_ViewRoles()
    {
        await AssertProblem(HttpStatusCode.Forbidden, await Client(await RoleViewer()).GetAsync("api/groups", Ct));
    }

    /// <summary>A request with no identity is answered with a 401; the anonymous client is the case under test.</summary>
    [Fact]
    public async Task GetAll_without_an_identity_is_unauthorized()
    {
        await AssertStatus(HttpStatusCode.Unauthorized, await Client().GetAsync("api/groups", Ct));
    }

    [Fact]
    public async Task Get_returns_the_group_to_a_caller_holding_ViewGroups()
    {
        var group = TestData.Group("Single");
        await Seed(group);

        var got = await ReadAsync<Group>(await Client(await Viewer()).GetAsync($"api/groups/{group.Id}", Ct));

        Assert.Equal("Single", got.Name);
    }

    [Fact]
    public async Task Get_is_forbidden_for_a_caller_holding_only_ViewRoles()
    {
        var group = TestData.Group();
        await Seed(group);

        await AssertProblem(HttpStatusCode.Forbidden, await Client(await RoleViewer()).GetAsync($"api/groups/{group.Id}", Ct));
    }

    [Fact]
    public async Task Create_persists_the_group_for_a_caller_holding_ManageGroups()
    {
        var response = await Client(await Manager()).PostAsJsonAsync("api/groups", new { name = "White Cell" }, Ct);

        var created = await ReadAsync<Group>(response);
        await AssertStatus(HttpStatusCode.Created, response);
        await using var db = NewContext();
        Assert.Equal("White Cell", (await db.Groups.SingleAsync(x => x.Id == created.Id, Ct)).Name);
    }

    [Fact]
    public async Task Create_is_forbidden_for_a_caller_holding_only_ViewGroups()
    {
        await AssertProblem(HttpStatusCode.Forbidden,
            await Client(await Viewer()).PostAsJsonAsync("api/groups", new { name = "Nope" }, Ct));

        await using var db = NewContext();
        Assert.False(await db.Groups.AnyAsync(x => x.Name == "Nope", Ct));
    }

    [Fact]
    public async Task Update_persists_the_name_for_a_caller_holding_ManageGroups()
    {
        var group = TestData.Group("Before");
        await Seed(group);

        await AssertStatus(HttpStatusCode.OK, await Client(await Manager()).PutAsJsonAsync($"api/groups/{group.Id}",
            new { id = group.Id, name = "After" }, Ct));

        await using var db = NewContext();
        Assert.Equal("After", (await db.Groups.SingleAsync(x => x.Id == group.Id, Ct)).Name);
    }

    [Fact]
    public async Task Update_is_forbidden_for_a_caller_holding_only_ViewGroups()
    {
        var group = TestData.Group("Before");
        await Seed(group);

        await AssertProblem(HttpStatusCode.Forbidden, await Client(await Viewer()).PutAsJsonAsync($"api/groups/{group.Id}",
            new { id = group.Id, name = "After" }, Ct));
    }

    [Fact]
    public async Task Update_reports_an_unknown_group_as_not_found()
    {
        var id = Guid.NewGuid();

        await AssertProblem(HttpStatusCode.NotFound, await RootClient.PutAsJsonAsync($"api/groups/{id}", new { id, name = "Ghost" }, Ct));
    }

    [Fact]
    public async Task Delete_removes_the_group_for_a_caller_holding_ManageGroups()
    {
        var group = TestData.Group();
        await Seed(group);

        await AssertStatus(HttpStatusCode.NoContent, await Client(await Manager()).DeleteAsync($"api/groups/{group.Id}", Ct));

        await using var db = NewContext();
        Assert.False(await db.Groups.AnyAsync(x => x.Id == group.Id, Ct));
    }

    [Fact]
    public async Task Delete_is_forbidden_for_a_caller_holding_only_ViewGroups()
    {
        var group = TestData.Group();
        await Seed(group);

        await AssertProblem(HttpStatusCode.Forbidden, await Client(await Viewer()).DeleteAsync($"api/groups/{group.Id}", Ct));
    }

    [Fact]
    public async Task Delete_reports_an_unknown_group_as_not_found()
    {
        await AssertProblem(HttpStatusCode.NotFound, await RootClient.DeleteAsync($"api/groups/{Guid.NewGuid()}", Ct));
    }

    // ---- Memberships ---------------------------------------------------------------------------

    [Fact]
    public async Task GetMemberships_returns_the_group_members_to_a_caller_holding_ViewGroups()
    {
        var group = TestData.Group();
        var user = TestData.User();
        var membership = TestData.GroupMembership(group.Id, user.Id);
        await Seed(group, user, membership);

        var memberships = await ReadAsync<List<GroupMembership>>(
            await Client(await Viewer()).GetAsync($"api/groups/{group.Id}/memberships", Ct));

        Assert.Equal(user.Id, Assert.Single(memberships).UserId);
    }

    [Fact]
    public async Task GetMemberships_is_forbidden_for_a_caller_holding_only_ViewRoles()
    {
        var group = TestData.Group();
        await Seed(group);

        await AssertProblem(HttpStatusCode.Forbidden,
            await Client(await RoleViewer()).GetAsync($"api/groups/{group.Id}/memberships", Ct));
    }

    [Fact]
    public async Task GetMembership_returns_the_membership_to_a_caller_holding_ViewGroups()
    {
        var group = TestData.Group();
        var user = TestData.User();
        var membership = TestData.GroupMembership(group.Id, user.Id);
        await Seed(group, user, membership);

        var got = await ReadAsync<GroupMembership>(
            await Client(await Viewer()).GetAsync($"api/groups/memberships/{membership.Id}", Ct));

        Assert.Equal(group.Id, got.GroupId);
    }

    [Fact]
    public async Task GetMembership_is_forbidden_for_a_caller_holding_only_ViewRoles()
    {
        var group = TestData.Group();
        var user = TestData.User();
        var membership = TestData.GroupMembership(group.Id, user.Id);
        await Seed(group, user, membership);

        await AssertProblem(HttpStatusCode.Forbidden,
            await Client(await RoleViewer()).GetAsync($"api/groups/memberships/{membership.Id}", Ct));
    }

    [Fact]
    public async Task CreateMembership_persists_the_membership_for_a_caller_holding_ManageGroups()
    {
        var group = TestData.Group();
        var user = TestData.User();
        await Seed(group, user);

        await AssertStatus(HttpStatusCode.Created, await Client(await Manager()).PostAsJsonAsync(
            $"api/groups/{group.Id}/memberships", new { groupId = group.Id, userId = user.Id }, Ct));

        await using var db = NewContext();
        Assert.True(await db.GroupMemberships.AnyAsync(x => x.GroupId == group.Id && x.UserId == user.Id, Ct));
    }

    [Fact]
    public async Task CreateMembership_is_forbidden_for_a_caller_holding_only_ViewGroups()
    {
        var group = TestData.Group();
        var user = TestData.User();
        await Seed(group, user);

        await AssertProblem(HttpStatusCode.Forbidden, await Client(await Viewer()).PostAsJsonAsync(
            $"api/groups/{group.Id}/memberships", new { groupId = group.Id, userId = user.Id }, Ct));
    }

    /// <summary>The membership is created in the group the body names, whatever group the route names.</summary>
    [Fact]
    public async Task CreateMembership_adds_the_user_to_the_group_the_body_names()
    {
        var routed = TestData.Group("Routed");
        var named = TestData.Group("Named");
        var user = TestData.User();
        await Seed(routed, named, user);

        await AssertStatus(HttpStatusCode.Created, await RootClient.PostAsJsonAsync(
            $"api/groups/{routed.Id}/memberships", new { groupId = named.Id, userId = user.Id }, Ct));

        await using var db = NewContext();
        Assert.Equal(named.Id, (await db.GroupMemberships.SingleAsync(x => x.UserId == user.Id, Ct)).GroupId);
    }

    [Fact]
    public async Task CreateMembership_answers_a_second_membership_of_the_same_user_with_a_conflict()
    {
        var group = TestData.Group();
        var user = TestData.User();
        await Seed(group, user, TestData.GroupMembership(group.Id, user.Id));

        await AssertProblem(HttpStatusCode.Conflict, await RootClient.PostAsJsonAsync(
            $"api/groups/{group.Id}/memberships", new { groupId = group.Id, userId = user.Id }, Ct));
    }

    [Fact]
    public async Task DeleteMembership_removes_the_membership_for_a_caller_holding_ManageGroups()
    {
        var group = TestData.Group();
        var user = TestData.User();
        var membership = TestData.GroupMembership(group.Id, user.Id);
        await Seed(group, user, membership);

        await AssertStatus(HttpStatusCode.NoContent,
            await Client(await Manager()).DeleteAsync($"api/groups/memberships/{membership.Id}", Ct));

        await using var db = NewContext();
        Assert.False(await db.GroupMemberships.AnyAsync(x => x.Id == membership.Id, Ct));
    }

    [Fact]
    public async Task DeleteMembership_is_forbidden_for_a_caller_holding_only_ViewGroups()
    {
        var group = TestData.Group();
        var user = TestData.User();
        var membership = TestData.GroupMembership(group.Id, user.Id);
        await Seed(group, user, membership);

        await AssertProblem(HttpStatusCode.Forbidden,
            await Client(await Viewer()).DeleteAsync($"api/groups/memberships/{membership.Id}", Ct));
    }

    /// <summary>A group membership grants the group's collection memberships through the real claims transformer.</summary>
    [Fact]
    public async Task A_group_membership_grants_the_collection_permissions_of_the_group()
    {
        var group = TestData.Group();
        var collection = TestData.Collection("Shared Through A Group");
        await Seed(group, collection, new Gallery.Api.Data.Models.CollectionMembershipEntity(collection.Id, null, group.Id)
        {
            Id = Guid.NewGuid(),
            RoleId = TestData.MembershipRoles.Observer
        });
        var actor = await Actor().SeedAsync();
        await Seed(TestData.GroupMembership(group.Id, actor.Id));

        await AssertStatus(HttpStatusCode.OK, await Client(actor).GetAsync($"api/collections/{collection.Id}", Ct));
    }
}
