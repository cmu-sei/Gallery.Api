// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Net;
using System.Net.Http.Json;
using Gallery.Api.Data.Enumerations;
using Gallery.Api.Data.Models;
using Gallery.Api.Tests.Support;
using Gallery.Api.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Gallery.Api.Tests.Controllers;

/// <summary>
/// <c>TeamController</c> over HTTP. Reading one team is gated on <c>ViewTeam</c>, which only being on the
/// team (or observing its exhibit) grants; writes are gated on EditExhibits or EditExhibit.
/// </summary>
public class TeamControllerTests(DatabaseFixture fixture, GalleryAppFactory factory) : ApiTestBase(fixture, factory)
{
    // ---- Lists ---------------------------------------------------------------------------------

    [Fact]
    public async Task GetByExhibit_returns_the_teams_to_a_member_holding_ViewExhibit()
    {
        var (exhibit, team) = await SeedTeam();
        var actor = await Actor().OnExhibit(exhibit, [ExhibitPermission.ViewExhibit]).SeedAsync();

        var teams = await ReadAsync<List<Team>>(await Client(actor).GetAsync($"api/exhibits/{exhibit.Id}/teams", Ct));

        Assert.Equal(team.Id, Assert.Single(teams).Id);
    }

    [Fact]
    public async Task GetByExhibit_returns_the_teams_to_a_participant_of_the_exhibit()
    {
        var (exhibit, team) = await SeedTeam();
        var actor = await Actor().OnTeam(team).SeedAsync();

        var teams = await ReadAsync<List<Team>>(await Client(actor).GetAsync($"api/exhibits/{exhibit.Id}/teams", Ct));

        Assert.Equal(team.Id, Assert.Single(teams).Id);
    }

    [Fact]
    public async Task GetByExhibit_is_forbidden_for_a_caller_holding_ViewExhibit_only_on_another_exhibit()
    {
        var (exhibit, _) = await SeedTeam();
        var actor = await Actor().OnNewExhibit(exhibit.CollectionId, ExhibitPermission.ViewExhibit).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/exhibits/{exhibit.Id}/teams", Ct));
    }

    /// <summary>A request with no identity is answered with a 401; the anonymous client is the case under test.</summary>
    [Fact]
    public async Task GetByExhibit_without_an_identity_is_unauthorized()
    {
        await AssertStatus(HttpStatusCode.Unauthorized, await Client().GetAsync($"api/exhibits/{Guid.NewGuid()}/teams", Ct));
    }

    /// <summary>A participant sees only their own team; an observer sees every team of the exhibit.</summary>
    [Fact]
    public async Task GetMineByExhibit_returns_only_the_participant_s_team()
    {
        var (exhibit, team) = await SeedTeam();
        await Seed(TestData.Team(exhibit.Id, "Other Team"));
        var actor = await Actor().OnTeam(team).SeedAsync();

        var teams = await ReadAsync<List<Team>>(await Client(actor).GetAsync($"api/exhibits/{exhibit.Id}/my-teams", Ct));

        Assert.Equal(team.Id, Assert.Single(teams).Id);
    }

    [Fact]
    public async Task GetMineByExhibit_returns_every_team_to_an_observer()
    {
        var (exhibit, team) = await SeedTeam();
        await Seed(TestData.Team(exhibit.Id, "Other Team"));
        var actor = await Actor().OnTeam(team, observer: true).SeedAsync();

        var teams = await ReadAsync<List<Team>>(await Client(actor).GetAsync($"api/exhibits/{exhibit.Id}/my-teams", Ct));

        Assert.Equal(2, teams.Count);
    }

    // ---- Get -----------------------------------------------------------------------------------

    [Fact]
    public async Task Get_returns_the_team_to_a_participant_of_it()
    {
        var (_, team) = await SeedTeam("Readable Team");
        var actor = await Actor().OnTeam(team).SeedAsync();

        var got = await ReadAsync<Team>(await Client(actor).GetAsync($"api/teams/{team.Id}", Ct));

        Assert.Equal("Readable Team", got.Name);
    }

    [Fact]
    public async Task Get_is_forbidden_for_a_caller_holding_ViewTeam_only_on_another_team()
    {
        var (exhibit, team) = await SeedTeam();
        var actor = await Actor().OnNewTeam(exhibit.Id).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/teams/{team.Id}", Ct));
    }

    /// <summary>Holding every system permission does not pass a team gate; only being on the team does.</summary>
    [Fact]
    public async Task Get_is_forbidden_for_a_caller_holding_every_system_permission()
    {
        var (_, team) = await SeedTeam();

        await AssertProblem(HttpStatusCode.Forbidden, await RootClient.GetAsync($"api/teams/{team.Id}", Ct));
    }

    // ---- Create --------------------------------------------------------------------------------

    [Fact]
    public async Task Create_persists_the_team_for_a_member_holding_EditExhibit()
    {
        var (exhibit, _) = await SeedTeam();
        var actor = await Actor().OnExhibit(exhibit, [ExhibitPermission.EditExhibit]).SeedAsync();

        var response = await Client(actor).PostAsJsonAsync("api/teams", new { name = "Blue", shortName = "B", exhibitId = exhibit.Id }, Ct);

        await AssertStatus(HttpStatusCode.Created, response);
        var created = await ReadAsync<Team>(response);
        await using var db = NewContext();
        Assert.Equal(exhibit.Id, (await db.Teams.SingleAsync(x => x.Id == created.Id, Ct)).ExhibitId);
    }

    [Fact]
    public async Task Create_is_forbidden_for_a_caller_holding_only_ViewExhibit()
    {
        var (exhibit, _) = await SeedTeam();
        var actor = await Actor().OnExhibit(exhibit, [ExhibitPermission.ViewExhibit]).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).PostAsJsonAsync("api/teams",
            new { name = "Blue", exhibitId = exhibit.Id }, Ct));
    }

    [Fact]
    public async Task Create_is_forbidden_for_a_caller_holding_EditExhibit_only_on_another_exhibit()
    {
        var (exhibit, _) = await SeedTeam();
        var actor = await Actor().OnNewExhibit(exhibit.CollectionId, ExhibitPermission.EditExhibit).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).PostAsJsonAsync("api/teams",
            new { name = "Blue", exhibitId = exhibit.Id }, Ct));
    }

    // ---- Update --------------------------------------------------------------------------------

    [Fact]
    public async Task Update_persists_the_change_for_a_caller_holding_EditExhibits()
    {
        var (exhibit, team) = await SeedTeam("Before");
        var actor = await Actor().WithSystemPermissions(SystemPermission.EditExhibits).SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).PutAsJsonAsync($"api/teams/{team.Id}",
            new { id = team.Id, name = "After", exhibitId = exhibit.Id }, Ct));

        await using var db = NewContext();
        Assert.Equal("After", (await db.Teams.SingleAsync(x => x.Id == team.Id, Ct)).Name);
    }

    [Fact]
    public async Task Update_is_forbidden_for_a_caller_holding_only_ViewExhibits()
    {
        var (exhibit, team) = await SeedTeam("Before");
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewExhibits).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).PutAsJsonAsync($"api/teams/{team.Id}",
            new { id = team.Id, name = "After", exhibitId = exhibit.Id }, Ct));
    }

    /// <summary>The gate reads the exhibit the body names, so the team moves into the caller's exhibit.</summary>
    [Fact]
    public async Task Update_moves_another_exhibit_s_team_when_the_body_names_one_the_caller_edits()
    {
        var (exhibit, team) = await SeedTeam("Theirs");
        var actor = await Actor().OnNewExhibit(exhibit.CollectionId, ExhibitPermission.EditExhibit).SeedAsync();
        var mine = Assert.Single(actor.NewExhibitIds);

        await AssertStatus(HttpStatusCode.OK, await Client(actor).PutAsJsonAsync($"api/teams/{team.Id}",
            new { id = team.Id, name = "Taken", exhibitId = mine }, Ct));

        await using var db = NewContext();
        var stored = await db.Teams.SingleAsync(x => x.Id == team.Id, Ct);
        Assert.Equal(("Taken", mine), (stored.Name, stored.ExhibitId.Value));
    }

    /// <summary>A body whose id differs from the route's is answered with a 403.</summary>
    [Fact]
    public async Task Update_answers_a_body_naming_another_team_with_forbidden()
    {
        var (exhibit, team) = await SeedTeam();

        var problem = await AssertProblem(HttpStatusCode.Forbidden, await RootClient.PutAsJsonAsync($"api/teams/{team.Id}",
            new { id = Guid.NewGuid(), name = "Renamed", exhibitId = exhibit.Id }, Ct));

        Assert.Equal("You cannot change the team Id", problem.Title);
    }

    // ---- Delete --------------------------------------------------------------------------------

    [Fact]
    public async Task Delete_removes_the_team_for_a_member_holding_EditExhibit()
    {
        var (exhibit, team) = await SeedTeam();
        var actor = await Actor().OnExhibit(exhibit, [ExhibitPermission.EditExhibit]).SeedAsync();

        await AssertStatus(HttpStatusCode.NoContent, await Client(actor).DeleteAsync($"api/teams/{team.Id}", Ct));

        await using var db = NewContext();
        Assert.False(await db.Teams.AnyAsync(x => x.Id == team.Id, Ct));
    }

    [Fact]
    public async Task Delete_is_forbidden_for_a_participant_of_the_team()
    {
        var (_, team) = await SeedTeam();
        var actor = await Actor().OnTeam(team).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).DeleteAsync($"api/teams/{team.Id}", Ct));

        await using var db = NewContext();
        Assert.True(await db.Teams.AnyAsync(x => x.Id == team.Id, Ct));
    }

    private async Task<(ExhibitEntity Exhibit, TeamEntity Team)> SeedTeam(string name = "Test Team")
    {
        var collection = TestData.Collection();
        var exhibit = TestData.Exhibit(collection.Id);
        var team = TestData.Team(exhibit.Id, name);
        await Seed(collection, exhibit, team);

        return (exhibit, team);
    }
}
