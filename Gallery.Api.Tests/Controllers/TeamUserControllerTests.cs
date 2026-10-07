// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Net;
using System.Net.Http.Json;
using Gallery.Api.Data.Enumerations;
using Gallery.Api.Data.Models;
using Gallery.Api.Hubs;
using Gallery.Api.Tests.Support;
using Gallery.Api.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Gallery.Api.Tests.Controllers;

/// <summary><c>TeamUserController</c> over HTTP: putting users on teams and taking them off.</summary>
public class TeamUserControllerTests(DatabaseFixture fixture, GalleryAppFactory factory) : ApiTestBase(fixture, factory)
{
    // ---- Reads ---------------------------------------------------------------------------------

    [Fact]
    public async Task GetByExhibit_returns_the_team_users_to_a_member_holding_ViewExhibit()
    {
        var (exhibit, team) = await SeedTeam();
        var teamUser = await SeedUserOn(team);
        var actor = await Actor().OnExhibit(exhibit, [ExhibitPermission.ViewExhibit]).SeedAsync();

        var teamUsers = await ReadAsync<List<TeamUser>>(await Client(actor).GetAsync($"api/exhibits/{exhibit.Id}/teamusers", Ct));

        Assert.Equal(teamUser.Id, Assert.Single(teamUsers).Id);
    }

    [Fact]
    public async Task GetByExhibit_is_forbidden_for_a_caller_holding_ViewExhibit_only_on_another_exhibit()
    {
        var (exhibit, _) = await SeedTeam();
        var actor = await Actor().OnNewExhibit(exhibit.CollectionId, ExhibitPermission.ViewExhibit).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/exhibits/{exhibit.Id}/teamusers", Ct));
    }

    /// <summary>A request with no identity is answered with a 401; the anonymous client is the case under test.</summary>
    [Fact]
    public async Task GetByExhibit_without_an_identity_is_unauthorized()
    {
        await AssertStatus(HttpStatusCode.Unauthorized, await Client().GetAsync($"api/exhibits/{Guid.NewGuid()}/teamusers", Ct));
    }

    [Fact]
    public async Task GetByTeam_returns_the_team_users_to_a_participant_of_the_team()
    {
        var (_, team) = await SeedTeam();
        var actor = await Actor().OnTeam(team).SeedAsync();

        var teamUsers = await ReadAsync<List<TeamUser>>(await Client(actor).GetAsync($"api/teams/{team.Id}/teamusers", Ct));

        Assert.Equal(actor.Id, Assert.Single(teamUsers).UserId);
    }

    [Fact]
    public async Task GetByTeam_is_forbidden_for_a_caller_holding_ViewTeam_only_on_another_team()
    {
        var (exhibit, team) = await SeedTeam();
        var actor = await Actor().OnNewTeam(exhibit.Id).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/teams/{team.Id}/teamusers", Ct));
    }

    [Fact]
    public async Task Get_returns_the_team_user_to_a_participant_of_the_team()
    {
        var (_, team) = await SeedTeam();
        var teamUser = await SeedUserOn(team);
        var actor = await Actor().OnTeam(team).SeedAsync();

        var got = await ReadAsync<TeamUser>(await Client(actor).GetAsync($"api/teamusers/{teamUser.Id}", Ct));

        Assert.Equal(teamUser.UserId, got.UserId);
    }

    [Fact]
    public async Task Get_is_forbidden_for_a_caller_holding_ViewTeam_only_on_another_team()
    {
        var (exhibit, team) = await SeedTeam();
        var teamUser = await SeedUserOn(team);
        var actor = await Actor().OnNewTeam(exhibit.Id).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/teamusers/{teamUser.Id}", Ct));
    }

    // ---- Create --------------------------------------------------------------------------------

    [Fact]
    public async Task Create_puts_the_user_on_the_team_for_a_caller_holding_ViewExhibits()
    {
        // Same case as Create_puts_the_caller_on_a_team_for_a_member_holding_only_ViewExhibit.
        var (_, team) = await SeedTeam();
        var user = TestData.User();
        await Seed(user);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewExhibits).SeedAsync();

        await AssertStatus(HttpStatusCode.Created, await Client(actor).PostAsJsonAsync("api/teamusers",
            new { teamId = team.Id, userId = user.Id }, Ct));

        await using var db = NewContext();
        Assert.True(await db.TeamUsers.AnyAsync(x => x.TeamId == team.Id && x.UserId == user.Id, Ct));
    }

    /// <summary>A read-only exhibit membership is enough to put a user, the caller included, on a team.</summary>
    [Fact]
    public async Task Create_puts_the_caller_on_a_team_for_a_member_holding_only_ViewExhibit()
    {
        var (exhibit, team) = await SeedTeam();
        var actor = await Actor().OnExhibit(exhibit, [ExhibitPermission.ViewExhibit]).SeedAsync();

        await AssertStatus(HttpStatusCode.Created, await Client(actor).PostAsJsonAsync("api/teamusers",
            new { teamId = team.Id, userId = actor.Id }, Ct));

        await using var db = NewContext();
        Assert.True(await db.TeamUsers.AnyAsync(x => x.TeamId == team.Id && x.UserId == actor.Id, Ct));
    }

    [Fact]
    public async Task Create_is_forbidden_for_a_caller_holding_ViewExhibit_only_on_another_exhibit()
    {
        var (exhibit, team) = await SeedTeam();
        var user = TestData.User();
        await Seed(user);
        var actor = await Actor().OnNewExhibit(exhibit.CollectionId, ExhibitPermission.ViewExhibit).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).PostAsJsonAsync("api/teamusers",
            new { teamId = team.Id, userId = user.Id }, Ct));
    }

    /// <summary>A user already on a team of the exhibit is answered with a 500 naming that team.</summary>
    [Fact]
    public async Task Create_answers_a_user_already_on_a_team_of_the_exhibit_with_a_server_error()
    {
        var (exhibit, team) = await SeedTeam("First Team");
        var other = TestData.Team(exhibit.Id, "Second Team");
        await Seed(other);
        var teamUser = await SeedUserOn(team);

        var problem = await AssertProblem(HttpStatusCode.InternalServerError, await RootClient.PostAsJsonAsync("api/teamusers",
            new { teamId = other.Id, userId = teamUser.UserId }, Ct));

        Assert.Equal($"The selected user ({teamUser.UserId}) is already on team First Team", problem.Detail);
    }

    /// <summary>Putting a user on a team broadcasts nothing to the team, its exhibit or the user.</summary>
    [Fact]
    public async Task Create_broadcasts_nothing_to_the_team_the_exhibit_or_the_user()
    {
        var (exhibit, team) = await SeedTeam();
        var user = TestData.User();
        await Seed(user);

        await AssertStatus(HttpStatusCode.Created, await RootClient.PostAsJsonAsync("api/teamusers", new { teamId = team.Id, userId = user.Id }, Ct));

        var hub = Factory.Hub<MainHub>();
        Assert.Empty(hub.ToGroup(team.Id));
        Assert.Empty(hub.ToGroup(exhibit.Id));
        Assert.Empty(hub.ToGroup(user.Id));
    }

    // ---- Observer ------------------------------------------------------------------------------

    [Fact]
    public async Task SetObserver_marks_the_team_user_an_observer_for_a_member_holding_EditExhibit()
    {
        var (exhibit, team) = await SeedTeam();
        var teamUser = await SeedUserOn(team);
        var actor = await Actor().OnExhibit(exhibit, [ExhibitPermission.EditExhibit]).SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).PutAsync($"api/teamusers/{teamUser.Id}/observer/set", null, Ct));

        await using var db = NewContext();
        Assert.True((await db.TeamUsers.SingleAsync(x => x.Id == teamUser.Id, Ct)).IsObserver);
    }

    [Fact]
    public async Task SetObserver_is_forbidden_for_a_caller_holding_only_ViewExhibit()
    {
        var (exhibit, team) = await SeedTeam();
        var teamUser = await SeedUserOn(team);
        var actor = await Actor().OnExhibit(exhibit, [ExhibitPermission.ViewExhibit]).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).PutAsync($"api/teamusers/{teamUser.Id}/observer/set", null, Ct));

        await using var db = NewContext();
        Assert.False((await db.TeamUsers.SingleAsync(x => x.Id == teamUser.Id, Ct)).IsObserver);
    }

    [Fact]
    public async Task ClearObserver_clears_the_flag_for_a_caller_holding_EditExhibits()
    {
        var (_, team) = await SeedTeam();
        var teamUser = await SeedUserOn(team, observer: true);
        var actor = await Actor().WithSystemPermissions(SystemPermission.EditExhibits).SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).PutAsync($"api/teamusers/{teamUser.Id}/observer/clear", null, Ct));

        await using var db = NewContext();
        Assert.False((await db.TeamUsers.SingleAsync(x => x.Id == teamUser.Id, Ct)).IsObserver);
    }

    [Fact]
    public async Task ClearObserver_is_forbidden_for_a_caller_holding_EditExhibit_only_on_another_exhibit()
    {
        var (exhibit, team) = await SeedTeam();
        var teamUser = await SeedUserOn(team, observer: true);
        var actor = await Actor().OnNewExhibit(exhibit.CollectionId, ExhibitPermission.EditExhibit).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).PutAsync($"api/teamusers/{teamUser.Id}/observer/clear", null, Ct));
    }

    // ---- Delete --------------------------------------------------------------------------------

    [Fact]
    public async Task Delete_takes_the_user_off_the_team_for_a_member_holding_EditExhibit()
    {
        var (exhibit, team) = await SeedTeam();
        var teamUser = await SeedUserOn(team);
        var actor = await Actor().OnExhibit(exhibit, [ExhibitPermission.EditExhibit]).SeedAsync();

        await AssertStatus(HttpStatusCode.NoContent, await Client(actor).DeleteAsync($"api/teamusers/{teamUser.Id}", Ct));

        await using var db = NewContext();
        Assert.False(await db.TeamUsers.AnyAsync(x => x.Id == teamUser.Id, Ct));
    }

    [Fact]
    public async Task Delete_is_forbidden_for_a_caller_holding_only_ViewExhibit()
    {
        var (exhibit, team) = await SeedTeam();
        var teamUser = await SeedUserOn(team);
        var actor = await Actor().OnExhibit(exhibit, [ExhibitPermission.ViewExhibit]).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).DeleteAsync($"api/teamusers/{teamUser.Id}", Ct));
    }

    [Fact]
    public async Task DeleteByIds_takes_the_user_off_the_team_for_a_caller_holding_EditExhibits()
    {
        var (_, team) = await SeedTeam();
        var teamUser = await SeedUserOn(team);
        var actor = await Actor().WithSystemPermissions(SystemPermission.EditExhibits).SeedAsync();

        await AssertStatus(HttpStatusCode.NoContent, await Client(actor).DeleteAsync($"api/teams/{team.Id}/users/{teamUser.UserId}", Ct));

        await using var db = NewContext();
        Assert.False(await db.TeamUsers.AnyAsync(x => x.Id == teamUser.Id, Ct));
    }

    [Fact]
    public async Task DeleteByIds_is_forbidden_for_a_caller_holding_only_ViewExhibits()
    {
        var (_, team) = await SeedTeam();
        var teamUser = await SeedUserOn(team);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewExhibits).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).DeleteAsync($"api/teams/{team.Id}/users/{teamUser.UserId}", Ct));
    }

    [Fact]
    public async Task DeleteByIds_reports_a_user_not_on_the_team_as_not_found()
    {
        var (_, team) = await SeedTeam();

        await AssertProblem(HttpStatusCode.NotFound, await RootClient.DeleteAsync($"api/teams/{team.Id}/users/{Guid.NewGuid()}", Ct));
    }

    private async Task<(ExhibitEntity Exhibit, TeamEntity Team)> SeedTeam(string name = "Test Team")
    {
        var collection = TestData.Collection();
        var exhibit = TestData.Exhibit(collection.Id);
        var team = TestData.Team(exhibit.Id, name);
        await Seed(collection, exhibit, team);

        return (exhibit, team);
    }

    private async Task<TeamUserEntity> SeedUserOn(TeamEntity team, bool observer = false)
    {
        var user = TestData.User(name: "Team Member");
        var teamUser = TestData.TeamUser(team.Id, user.Id, observer);
        await Seed(user, teamUser);

        return teamUser;
    }
}
