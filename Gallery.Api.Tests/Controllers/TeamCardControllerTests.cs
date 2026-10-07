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
/// <c>TeamCardController</c> over HTTP: which cards a team sees on its wall. Writes are gated on
/// ManageExhibits or ManageExhibit on the team's exhibit.
/// </summary>
public class TeamCardControllerTests(DatabaseFixture fixture, GalleryAppFactory factory) : ApiTestBase(fixture, factory)
{
    [Fact]
    public async Task GetAll_returns_every_team_card_to_a_caller_holding_ManageExhibits()
    {
        var (_, _, teamCard) = await SeedTeamCard();
        var actor = await Actor().WithSystemPermissions(SystemPermission.ManageExhibits).SeedAsync();

        var teamCards = await ReadAsync<List<TeamCard>>(await Client(actor).GetAsync("api/teamcards", Ct));

        Assert.Contains(teamCards, x => x.Id == teamCard.Id);
    }

    [Fact]
    public async Task GetAll_is_forbidden_for_a_caller_holding_only_EditExhibits()
    {
        var actor = await Actor().WithSystemPermissions(SystemPermission.EditExhibits).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).GetAsync("api/teamcards", Ct));
    }

    /// <summary>A request with no identity is answered with a 401; the anonymous client is the case under test.</summary>
    [Fact]
    public async Task GetAll_without_an_identity_is_unauthorized()
    {
        await AssertStatus(HttpStatusCode.Unauthorized, await Client().GetAsync("api/teamcards", Ct));
    }

    [Fact]
    public async Task GetByExhibit_returns_the_team_cards_to_a_member_holding_ViewExhibit()
    {
        var (exhibit, _, teamCard) = await SeedTeamCard();
        var actor = await Actor().OnExhibit(exhibit, [ExhibitPermission.ViewExhibit]).SeedAsync();

        var teamCards = await ReadAsync<List<TeamCard>>(await Client(actor).GetAsync($"api/exhibits/{exhibit.Id}/teamcards", Ct));

        Assert.Equal(teamCard.Id, Assert.Single(teamCards).Id);
    }

    [Fact]
    public async Task GetByExhibit_is_forbidden_for_a_caller_holding_ViewExhibit_only_on_another_exhibit()
    {
        var (exhibit, _, _) = await SeedTeamCard();
        var actor = await Actor().OnNewExhibit(exhibit.CollectionId, ExhibitPermission.ViewExhibit).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/exhibits/{exhibit.Id}/teamcards", Ct));
    }

    [Fact]
    public async Task GetByExhibitTeam_returns_the_team_s_cards_to_a_participant_of_the_team()
    {
        var (exhibit, team, teamCard) = await SeedTeamCard();
        var actor = await Actor().OnTeam(team).SeedAsync();

        var teamCards = await ReadAsync<List<TeamCard>>(await Client(actor).GetAsync($"api/exhibits/{exhibit.Id}/teams/{team.Id}/teamcards", Ct));

        Assert.Equal(teamCard.Id, Assert.Single(teamCards).Id);
    }

    [Fact]
    public async Task GetByExhibitTeam_is_forbidden_for_a_caller_holding_ViewTeam_only_on_another_team()
    {
        var (exhibit, team, _) = await SeedTeamCard();
        var actor = await Actor().OnNewTeam(exhibit.Id).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/exhibits/{exhibit.Id}/teams/{team.Id}/teamcards", Ct));
    }

    [Fact]
    public async Task Get_returns_the_team_card_to_a_participant_of_the_team()
    {
        var (_, team, teamCard) = await SeedTeamCard();
        var actor = await Actor().OnTeam(team).SeedAsync();

        var got = await ReadAsync<TeamCard>(await Client(actor).GetAsync($"api/teamcards/{teamCard.Id}", Ct));

        Assert.Equal(teamCard.CardId, got.CardId);
    }

    [Fact]
    public async Task Get_is_forbidden_for_a_caller_holding_ViewTeam_only_on_another_team()
    {
        var (exhibit, _, teamCard) = await SeedTeamCard();
        var actor = await Actor().OnNewTeam(exhibit.Id).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/teamcards/{teamCard.Id}", Ct));
    }

    [Fact]
    public async Task Get_reports_an_unknown_team_card_as_not_found()
    {
        await AssertProblem(HttpStatusCode.NotFound, await RootClient.GetAsync($"api/teamcards/{Guid.NewGuid()}", Ct));
    }

    [Fact]
    public async Task Create_persists_the_team_card_for_a_member_holding_ManageExhibit()
    {
        var (exhibit, team, _) = await SeedTeamCard();
        var card = TestData.Card(exhibit.CollectionId, "Second Card");
        await Seed(card);
        var actor = await Actor().OnExhibit(exhibit, [ExhibitPermission.ManageExhibit]).SeedAsync();

        await AssertStatus(HttpStatusCode.Created, await Client(actor).PostAsJsonAsync("api/teamcards",
            new { teamId = team.Id, cardId = card.Id, isShownOnWall = true, canPostArticles = true }, Ct));

        await using var db = NewContext();
        Assert.True((await db.TeamCards.SingleAsync(x => x.CardId == card.Id, Ct)).CanPostArticles);
    }

    [Fact]
    public async Task Create_is_forbidden_for_a_caller_holding_only_EditExhibit()
    {
        var (exhibit, team, _) = await SeedTeamCard();
        var card = TestData.Card(exhibit.CollectionId, "Second Card");
        await Seed(card);
        var actor = await Actor().OnExhibit(exhibit, [ExhibitPermission.EditExhibit]).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).PostAsJsonAsync("api/teamcards",
            new { teamId = team.Id, cardId = card.Id }, Ct));
    }

    [Fact]
    public async Task Create_answers_a_second_card_for_the_same_team_with_a_conflict()
    {
        var (_, team, teamCard) = await SeedTeamCard();

        await AssertProblem(HttpStatusCode.Conflict, await RootClient.PostAsJsonAsync("api/teamcards",
            new { teamId = team.Id, cardId = teamCard.CardId }, Ct));
    }

    [Fact]
    public async Task Update_persists_the_change_for_a_member_holding_ManageExhibit()
    {
        var (exhibit, team, teamCard) = await SeedTeamCard();
        var actor = await Actor().OnExhibit(exhibit, [ExhibitPermission.ManageExhibit]).SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).PutAsJsonAsync($"api/teamcards/{teamCard.Id}",
            new { id = teamCard.Id, teamId = team.Id, cardId = teamCard.CardId, move = 3, isShownOnWall = false }, Ct));

        await using var db = NewContext();
        var stored = await db.TeamCards.SingleAsync(x => x.Id == teamCard.Id, Ct);
        Assert.Equal((3, false), (stored.Move, stored.IsShownOnWall));
    }

    [Fact]
    public async Task Update_is_forbidden_for_a_caller_holding_ManageExhibit_only_on_another_exhibit()
    {
        var (exhibit, team, teamCard) = await SeedTeamCard();
        var actor = await Actor().OnNewExhibit(exhibit.CollectionId, ExhibitPermission.ManageExhibit).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).PutAsJsonAsync($"api/teamcards/{teamCard.Id}",
            new { id = teamCard.Id, teamId = team.Id, cardId = teamCard.CardId, move = 3 }, Ct));
    }

    /// <summary>The gate reads the team the body names, so the team card moves onto the caller's team.</summary>
    [Fact]
    public async Task Update_rewrites_another_exhibit_s_team_card_when_the_body_names_a_team_the_caller_manages()
    {
        var (exhibit, _, teamCard) = await SeedTeamCard();
        var mine = TestData.Exhibit(exhibit.CollectionId, "Mine");
        var myTeam = TestData.Team(mine.Id, "My Team");
        await Seed(mine, myTeam);
        var actor = await Actor().OnExhibit(mine, [ExhibitPermission.ManageExhibit]).SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).PutAsJsonAsync($"api/teamcards/{teamCard.Id}",
            new { id = teamCard.Id, teamId = myTeam.Id, cardId = teamCard.CardId, move = 9 }, Ct));

        await using var db = NewContext();
        var stored = await db.TeamCards.SingleAsync(x => x.Id == teamCard.Id, Ct);
        Assert.Equal((myTeam.Id, 9), (stored.TeamId, stored.Move));
    }

    [Fact]
    public async Task Delete_removes_the_team_card_for_a_caller_holding_ManageExhibits()
    {
        var (_, _, teamCard) = await SeedTeamCard();
        var actor = await Actor().WithSystemPermissions(SystemPermission.ManageExhibits).SeedAsync();

        await AssertStatus(HttpStatusCode.NoContent, await Client(actor).DeleteAsync($"api/teamcards/{teamCard.Id}", Ct));

        await using var db = NewContext();
        Assert.False(await db.TeamCards.AnyAsync(x => x.Id == teamCard.Id, Ct));
    }

    [Fact]
    public async Task Delete_is_forbidden_for_a_participant_of_the_team()
    {
        var (_, team, teamCard) = await SeedTeamCard();
        var actor = await Actor().OnTeam(team).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).DeleteAsync($"api/teamcards/{teamCard.Id}", Ct));
    }

    [Fact]
    public async Task DeleteByIds_removes_the_team_card_for_a_member_holding_ManageExhibit()
    {
        var (exhibit, team, teamCard) = await SeedTeamCard();
        var actor = await Actor().OnExhibit(exhibit, [ExhibitPermission.ManageExhibit]).SeedAsync();

        await AssertStatus(HttpStatusCode.NoContent, await Client(actor).DeleteAsync($"api/teams/{team.Id}/cards/{teamCard.CardId}", Ct));

        await using var db = NewContext();
        Assert.False(await db.TeamCards.AnyAsync(x => x.Id == teamCard.Id, Ct));
    }

    [Fact]
    public async Task DeleteByIds_is_forbidden_for_a_caller_holding_only_ViewExhibit()
    {
        var (exhibit, team, teamCard) = await SeedTeamCard();
        var actor = await Actor().OnExhibit(exhibit, [ExhibitPermission.ViewExhibit]).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).DeleteAsync($"api/teams/{team.Id}/cards/{teamCard.CardId}", Ct));
    }

    private async Task<(ExhibitEntity Exhibit, TeamEntity Team, TeamCardEntity TeamCard)> SeedTeamCard()
    {
        var collection = TestData.Collection();
        var exhibit = TestData.Exhibit(collection.Id);
        var team = TestData.Team(exhibit.Id);
        var card = TestData.Card(collection.Id);
        var teamCard = TestData.TeamCard(team.Id, card.Id);
        await Seed(collection, exhibit, team, card, teamCard);

        return (exhibit, team, teamCard);
    }
}
