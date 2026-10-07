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
/// <c>ExhibitTeamController</c>, the exhibit-team link rows that predate teams belonging to one exhibit.
/// </summary>
public class ExhibitTeamControllerTests(DatabaseFixture fixture, GalleryAppFactory factory) : ApiTestBase(fixture, factory)
{
    /// <summary>The list is refused to a caller holding ViewExhibits.</summary>
    [Fact]
    public async Task GetAll_is_forbidden_for_a_caller_holding_ViewExhibits()
    {
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewExhibits).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).GetAsync("api/exhibitteams", Ct));
    }

    /// <summary>The list is served to a caller holding no view permission at all.</summary>
    [Fact]
    public async Task GetAll_returns_every_link_to_a_caller_holding_only_ViewGroups()
    {
        // Same case as GetAll_is_forbidden_for_a_caller_holding_ViewExhibits.
        var (_, link) = await SeedLink();
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewGroups).SeedAsync();

        var links = await ReadAsync<List<ExhibitTeam>>(await Client(actor).GetAsync("api/exhibitteams", Ct));

        Assert.Contains(links, x => x.Id == link.Id);
    }

    /// <summary>A request with no identity is answered with a 401; the anonymous client is the case under test.</summary>
    [Fact]
    public async Task GetAll_without_an_identity_is_unauthorized()
    {
        await AssertStatus(HttpStatusCode.Unauthorized, await Client().GetAsync("api/exhibitteams", Ct));
    }

    /// <summary>A link is refused even to a participant of the linked team.</summary>
    [Fact]
    public async Task Get_is_forbidden_for_a_participant_of_the_linked_team()
    {
        var (team, link) = await SeedLink();
        var actor = await Actor().OnTeam(team).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/exhibitteams/{link.Id}", Ct));
    }

    [Fact]
    public async Task GetByExhibit_returns_the_links_to_a_member_holding_ViewExhibit()
    {
        var (team, link) = await SeedLink();
        var actor = await Actor().OnExhibit(await Exhibit(link), [ExhibitPermission.ViewExhibit]).SeedAsync();

        var links = await ReadAsync<List<ExhibitTeam>>(await Client(actor).GetAsync($"api/exhibits/{link.ExhibitId}/exhibitteams", Ct));

        Assert.Equal(link.Id, Assert.Single(links).Id);
    }

    [Fact]
    public async Task GetByExhibit_is_forbidden_for_a_caller_holding_ViewExhibit_only_on_another_exhibit()
    {
        var (_, link) = await SeedLink();
        var exhibit = await Exhibit(link);
        var actor = await Actor().OnNewExhibit(exhibit.CollectionId, ExhibitPermission.ViewExhibit).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/exhibits/{link.ExhibitId}/exhibitteams", Ct));
    }

    [Fact]
    public async Task Create_persists_the_link_for_a_member_holding_EditExhibit()
    {
        var (team, link) = await SeedLink();
        var exhibit = await Exhibit(link);
        var other = TestData.Exhibit(exhibit.CollectionId, "Linked Too");
        await Seed(other);
        var actor = await Actor().OnExhibit(other, [ExhibitPermission.EditExhibit]).SeedAsync();

        await AssertStatus(HttpStatusCode.Created, await Client(actor).PostAsJsonAsync("api/exhibitteams",
            new { exhibitId = other.Id, teamId = team.Id }, Ct));

        await using var db = NewContext();
        Assert.True(await db.ExhibitTeams.AnyAsync(x => x.ExhibitId == other.Id && x.TeamId == team.Id, Ct));
    }

    [Fact]
    public async Task Create_is_forbidden_for_a_caller_holding_only_ViewExhibit()
    {
        var (team, link) = await SeedLink();
        var exhibit = await Exhibit(link);
        var other = TestData.Exhibit(exhibit.CollectionId, "Not Linked");
        await Seed(other);
        var actor = await Actor().OnExhibit(other, [ExhibitPermission.ViewExhibit]).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).PostAsJsonAsync("api/exhibitteams",
            new { exhibitId = other.Id, teamId = team.Id }, Ct));
    }

    [Fact]
    public async Task Delete_removes_the_link_for_a_member_holding_EditExhibit()
    {
        var (_, link) = await SeedLink();
        var actor = await Actor().OnExhibit(await Exhibit(link), [ExhibitPermission.EditExhibit]).SeedAsync();

        await AssertStatus(HttpStatusCode.NoContent, await Client(actor).DeleteAsync($"api/exhibitteams/{link.Id}", Ct));

        await using var db = NewContext();
        Assert.False(await db.ExhibitTeams.AnyAsync(x => x.Id == link.Id, Ct));
    }

    [Fact]
    public async Task Delete_is_forbidden_for_a_caller_holding_only_ViewExhibit()
    {
        var (_, link) = await SeedLink();
        var actor = await Actor().OnExhibit(await Exhibit(link), [ExhibitPermission.ViewExhibit]).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).DeleteAsync($"api/exhibitteams/{link.Id}", Ct));
    }

    [Fact]
    public async Task DeleteByIds_removes_the_link_for_a_member_holding_EditExhibit()
    {
        var (team, link) = await SeedLink();
        var actor = await Actor().OnExhibit(await Exhibit(link), [ExhibitPermission.EditExhibit]).SeedAsync();

        await AssertStatus(HttpStatusCode.NoContent, await Client(actor).DeleteAsync($"api/exhibits/{link.ExhibitId}/teams/{team.Id}", Ct));

        await using var db = NewContext();
        Assert.False(await db.ExhibitTeams.AnyAsync(x => x.Id == link.Id, Ct));
    }

    [Fact]
    public async Task DeleteByIds_is_forbidden_for_a_caller_holding_EditExhibit_only_on_another_exhibit()
    {
        var (team, link) = await SeedLink();
        var exhibit = await Exhibit(link);
        var actor = await Actor().OnNewExhibit(exhibit.CollectionId, ExhibitPermission.EditExhibit).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).DeleteAsync($"api/exhibits/{link.ExhibitId}/teams/{team.Id}", Ct));
    }

    private async Task<(TeamEntity Team, ExhibitTeamEntity Link)> SeedLink()
    {
        var collection = TestData.Collection();
        var exhibit = TestData.Exhibit(collection.Id);
        var team = TestData.Team(exhibit.Id);
        var link = TestData.ExhibitTeam(exhibit.Id, team.Id);
        await Seed(collection, exhibit, team, link);

        return (team, link);
    }

    private async Task<ExhibitEntity> Exhibit(ExhibitTeamEntity link) =>
        await Db.Exhibits.SingleAsync(x => x.Id == link.ExhibitId, Ct);
}
