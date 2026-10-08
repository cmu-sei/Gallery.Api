// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Net;
using System.Net.Http.Json;
using Gallery.Api.Data.Enumerations;
using Gallery.Api.Tests.Support;
using Gallery.Api.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Gallery.Api.Tests.Controllers;

/// <summary><c>UserController</c> over HTTP.</summary>
public class UserControllerTests(DatabaseFixture fixture, GalleryAppFactory factory) : ApiTestBase(fixture, factory)
{
    // ---- GetAll --------------------------------------------------------------------------------

    [Fact]
    public async Task GetAll_returns_every_user_to_a_caller_holding_ViewUsers()
    {
        var other = TestData.User(name: "Listed");
        await Seed(other);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewUsers).SeedAsync();

        var users = await ReadAsync<List<User>>(await Client(actor).GetAsync("api/users", Ct));

        Assert.Contains(users, x => x.Id == other.Id);
    }

    /// <summary>A content view permission is enough to list users, for picking team members.</summary>
    [Fact]
    public async Task GetAll_returns_every_user_to_a_caller_holding_only_ViewExhibits()
    {
        var other = TestData.User(name: "Listed");
        await Seed(other);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewExhibits).SeedAsync();

        var users = await ReadAsync<List<User>>(await Client(actor).GetAsync("api/users", Ct));

        Assert.Contains(users, x => x.Id == other.Id);
    }

    /// <summary>The list is not refused but filtered to nothing for a caller with no view permission.</summary>
    [Fact]
    public async Task GetAll_returns_nothing_to_a_caller_holding_only_ViewGroups()
    {
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewGroups).SeedAsync();

        var users = await ReadAsync<List<User>>(await Client(actor).GetAsync("api/users", Ct));

        Assert.Empty(users);
    }

    /// <summary>A request with no identity is answered with a 401; the anonymous client is the case under test.</summary>
    [Fact]
    public async Task GetAll_without_an_identity_is_unauthorized()
    {
        await AssertStatus(HttpStatusCode.Unauthorized, await Client().GetAsync("api/users", Ct));
    }

    // ---- Get -----------------------------------------------------------------------------------

    [Fact]
    public async Task Get_returns_the_user_to_a_caller_holding_ViewUsers()
    {
        var other = TestData.User(name: "Single");
        await Seed(other);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewUsers).SeedAsync();

        var user = await ReadAsync<User>(await Client(actor).GetAsync($"api/users/{other.Id}", Ct));

        Assert.Equal("Single", user.Name);
    }

    [Fact]
    public async Task Get_is_forbidden_for_a_caller_holding_only_ViewExhibits()
    {
        var other = TestData.User();
        await Seed(other);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewExhibits).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/users/{other.Id}", Ct));
    }

    [Fact]
    public async Task Get_reports_an_unknown_user_as_not_found()
    {
        var problem = await AssertProblem(HttpStatusCode.NotFound, await RootClient.GetAsync($"api/users/{Guid.NewGuid()}", Ct));

        Assert.Equal("User not found", problem.Title);
    }

    // ---- GetByTeam -----------------------------------------------------------------------------

    [Fact]
    public async Task GetByTeam_returns_the_team_users_to_a_member_of_the_team()
    {
        var (exhibit, team) = await SeedTeam();
        var member = await Actor().OnTeam(team).SeedAsync();

        var users = await ReadAsync<List<User>>(await Client(member).GetAsync($"api/teams/{team.Id}/users", Ct));

        Assert.Equal(member.Id, Assert.Single(users).Id);
    }

    [Fact]
    public async Task GetByTeam_is_forbidden_for_a_caller_holding_ViewTeam_only_on_another_team()
    {
        var (exhibit, team) = await SeedTeam();
        var sibling = await Actor().OnNewTeam(exhibit.Id).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(sibling).GetAsync($"api/teams/{team.Id}/users", Ct));
    }

    // ---- Create --------------------------------------------------------------------------------

    [Fact]
    public async Task Create_persists_the_user_for_a_caller_holding_ManageUsers()
    {
        var id = Guid.NewGuid();
        var actor = await Actor().WithSystemPermissions(SystemPermission.ManageUsers).SeedAsync();

        var response = await Client(actor).PostAsJsonAsync("api/users", new { id, name = "New User" }, Ct);

        await AssertStatus(HttpStatusCode.Created, response);
        await using var db = NewContext();
        Assert.Equal("New User", (await db.Users.SingleAsync(x => x.Id == id, Ct)).Name);
    }

    // Same case as Update_lets_a_caller_holding_only_ManageUsers_give_itself_the_administrator_role.
    /// <summary>A caller holding only ManageUsers creates a user holding the Administrator role.</summary>
    [Fact]
    public async Task Create_lets_a_caller_holding_only_ManageUsers_create_an_administrator()
    {
        var id = Guid.NewGuid();
        var actor = await Actor().WithSystemPermissions(SystemPermission.ManageUsers).SeedAsync();

        await AssertStatus(HttpStatusCode.Created, await Client(actor).PostAsJsonAsync("api/users",
            new { id, name = "New Administrator", roleId = TestData.Roles.Administrator }, Ct));

        await using var db = NewContext();
        Assert.Equal(TestData.Roles.Administrator, (await db.Users.SingleAsync(x => x.Id == id, Ct)).RoleId);
    }

    [Fact]
    public async Task Create_is_forbidden_for_a_caller_holding_only_ViewUsers()
    {
        var id = Guid.NewGuid();
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewUsers).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).PostAsJsonAsync("api/users", new { id, name = "Nope" }, Ct));

        await using var db = NewContext();
        Assert.False(await db.Users.AnyAsync(x => x.Id == id, Ct));
    }

    /// <summary>A body with no id is answered with a 500 after the user row has been saved.</summary>
    [Fact]
    public async Task Create_without_an_id_answers_a_server_error_and_keeps_the_user()
    {
        var response = await RootClient.PostAsJsonAsync("api/users", new { name = "Idless User" }, Ct);

        Assert.Equal("Object reference not set to an instance of an object.", (await AssertProblem(HttpStatusCode.InternalServerError, response)).Detail);
        await using var db = NewContext();
        Assert.True(await db.Users.AnyAsync(x => x.Name == "Idless User", Ct));
    }

    // ---- Update --------------------------------------------------------------------------------

    [Fact]
    public async Task Update_persists_the_change_for_a_caller_holding_ManageUsers()
    {
        var user = TestData.User(name: "Before");
        await Seed(user);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ManageUsers).SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).PutAsJsonAsync($"api/users/{user.Id}",
            new { id = user.Id, name = "After" }, Ct));

        await using var db = NewContext();
        var stored = await db.Users.SingleAsync(x => x.Id == user.Id, Ct);
        Assert.Equal(("After", (Guid?)null), (stored.Name, stored.RoleId));
    }

    [Fact]
    public async Task Update_is_forbidden_for_a_caller_holding_only_ViewUsers()
    {
        var user = TestData.User(name: "Before");
        await Seed(user);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewUsers).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).PutAsJsonAsync($"api/users/{user.Id}",
            new { id = user.Id, name = "After", roleId = TestData.Roles.Administrator }, Ct));

        await using var db = NewContext();
        Assert.Null((await db.Users.SingleAsync(x => x.Id == user.Id, Ct)).RoleId);
    }

    /// <summary>A caller holding only ManageUsers gives itself the Administrator role.</summary>
    [Fact]
    public async Task Update_lets_a_caller_holding_only_ManageUsers_give_itself_the_administrator_role()
    {
        var actor = await Actor().WithSystemPermissions(SystemPermission.ManageUsers).SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).PutAsJsonAsync($"api/users/{actor.Id}",
            new { id = actor.Id, name = actor.Name, roleId = TestData.Roles.Administrator }, Ct));

        await using var db = NewContext();
        Assert.Equal(TestData.Roles.Administrator, (await db.Users.SingleAsync(x => x.Id == actor.Id, Ct)).RoleId);
    }

    /// <summary>A body whose id differs from the route's is answered with a 403.</summary>
    [Fact]
    public async Task Update_answers_a_body_naming_another_id_with_forbidden()
    {
        var user = TestData.User();
        await Seed(user);

        var problem = await AssertProblem(HttpStatusCode.Forbidden, await RootClient.PutAsJsonAsync($"api/users/{user.Id}",
            new { id = Guid.NewGuid(), name = "Renamed" }, Ct));

        Assert.Equal("You cannot change the UserId", problem.Title);
    }

    // ---- Delete --------------------------------------------------------------------------------

    [Fact]
    public async Task Delete_removes_the_user_and_their_collection_memberships_for_a_caller_holding_ManageUsers()
    {
        var user = TestData.User();
        var collection = TestData.Collection();
        await Seed(user, collection, TestData.CollectionMembership(collection.Id, user.Id));
        var actor = await Actor().WithSystemPermissions(SystemPermission.ManageUsers).SeedAsync();

        await AssertStatus(HttpStatusCode.NoContent, await Client(actor).DeleteAsync($"api/users/{user.Id}", Ct));

        await using var db = NewContext();
        Assert.False(await db.Users.AnyAsync(x => x.Id == user.Id, Ct));
        Assert.False(await db.CollectionMemberships.AnyAsync(x => x.UserId == user.Id, Ct));
    }

    [Fact]
    public async Task Delete_is_forbidden_for_a_caller_holding_only_ViewUsers()
    {
        var user = TestData.User();
        await Seed(user);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewUsers).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).DeleteAsync($"api/users/{user.Id}", Ct));
    }

    [Fact]
    public async Task Delete_refuses_the_caller_s_own_account()
    {
        var problem = await AssertProblem(HttpStatusCode.Forbidden, await RootClient.DeleteAsync($"api/users/{Root.Id}", Ct));

        Assert.Equal("You cannot delete your own account", problem.Title);
    }

    [Fact]
    public async Task Delete_reports_an_unknown_user_as_not_found()
    {
        await AssertProblem(HttpStatusCode.NotFound, await RootClient.DeleteAsync($"api/users/{Guid.NewGuid()}", Ct));
    }

    /// <summary>A user with an exhibit membership is refused with a 400 naming a missing referenced entity, and kept.</summary>
    [Fact]
    public async Task Delete_of_a_user_with_an_exhibit_membership_is_answered_with_a_bad_request()
    {
        var user = TestData.User();
        var collection = TestData.Collection();
        var exhibit = TestData.Exhibit(collection.Id);
        await Seed(user, collection, exhibit, TestData.ExhibitMembership(exhibit.Id, user.Id));

        await AssertProblem(HttpStatusCode.BadRequest, await RootClient.DeleteAsync($"api/users/{user.Id}", Ct));

        await using var db = NewContext();
        Assert.True(await db.Users.AnyAsync(x => x.Id == user.Id, Ct));
    }

    private async Task<(Gallery.Api.Data.Models.ExhibitEntity Exhibit, Gallery.Api.Data.Models.TeamEntity Team)> SeedTeam()
    {
        var collection = TestData.Collection();
        var exhibit = TestData.Exhibit(collection.Id);
        var team = TestData.Team(exhibit.Id);
        await Seed(collection, exhibit, team);

        return (exhibit, team);
    }
}
