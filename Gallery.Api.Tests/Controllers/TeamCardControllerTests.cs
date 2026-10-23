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
    public async Task GetByExhibit_returns_the_team_cards_to_a_caller_holding_ViewExhibits()
    {
        var (exhibit, _, teamCard) = await SeedTeamCard();
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewExhibits).SeedAsync();

        var teamCards = await ReadAsync<List<TeamCard>>(await Client(actor).GetAsync($"api/exhibits/{exhibit.Id}/teamcards", Ct));

        Assert.Equal(teamCard.Id, Assert.Single(teamCards).Id);
    }

    [Fact]
    public async Task GetByExhibit_is_forbidden_for_a_caller_holding_only_ViewCollections()
    {
        var (exhibit, _, _) = await SeedTeamCard();
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewCollections).SeedAsync();

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

    /// <summary>An observer of the exhibit holds ViewTeam alone on the teams it is not on.</summary>
    [Fact]
    public async Task GetByExhibitTeam_returns_the_team_s_cards_to_an_observer_on_another_team_of_the_exhibit()
    {
        var (exhibit, team, teamCard) = await SeedTeamCard();
        var observers = TestData.Team(exhibit.Id, "Observers");
        await Seed(observers);
        var actor = await Actor().OnTeam(observers, observer: true).SeedAsync();

        var teamCards = await ReadAsync<List<TeamCard>>(await Client(actor).GetAsync($"api/exhibits/{exhibit.Id}/teams/{team.Id}/teamcards", Ct));

        Assert.Equal(teamCard.Id, Assert.Single(teamCards).Id);
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

    /// <summary>An observer of the exhibit holds ViewTeam alone on the teams it is not on.</summary>
    [Fact]
    public async Task Get_returns_the_team_card_to_an_observer_on_another_team_of_the_exhibit()
    {
        var (exhibit, _, teamCard) = await SeedTeamCard();
        var observers = TestData.Team(exhibit.Id, "Observers");
        await Seed(observers);
        var actor = await Actor().OnTeam(observers, observer: true).SeedAsync();

        var got = await ReadAsync<TeamCard>(await Client(actor).GetAsync($"api/teamcards/{teamCard.Id}", Ct));

        Assert.Equal(teamCard.CardId, got.CardId);
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

    /// <summary>A TeamCard can name only a card of its team's exhibit collection.</summary>
    [Fact]
    public async Task Create_answers_a_card_of_another_collection_with_not_found_and_saves_nothing()
    {
        var (exhibit, team, _) = await SeedTeamCard();
        var other = TestData.Collection("Other");
        var foreignCard = TestData.Card(other.Id, "Foreign");
        await Seed(other, foreignCard);
        var actor = await Actor().OnExhibit(exhibit, [ExhibitPermission.ManageExhibit]).SeedAsync();

        await AssertProblem(HttpStatusCode.NotFound, await Client(actor).PostAsJsonAsync("api/teamcards",
            new { teamId = team.Id, cardId = foreignCard.Id, canPostArticles = true }, Ct));

        await using var db = NewContext();
        Assert.False(await db.TeamCards.AnyAsync(x => x.CardId == foreignCard.Id, Ct));
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
    public async Task Create_persists_the_team_card_for_a_caller_holding_ManageExhibits()
    {
        var (exhibit, team, _) = await SeedTeamCard();
        var card = TestData.Card(exhibit.CollectionId, "Second Card");
        await Seed(card);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ManageExhibits).SeedAsync();

        await AssertStatus(HttpStatusCode.Created, await Client(actor).PostAsJsonAsync("api/teamcards",
            new { teamId = team.Id, cardId = card.Id, isShownOnWall = true, canPostArticles = true }, Ct));

        await using var db = NewContext();
        Assert.True((await db.TeamCards.SingleAsync(x => x.CardId == card.Id, Ct)).CanPostArticles);
    }

    [Fact]
    public async Task Create_is_forbidden_for_a_caller_holding_only_EditExhibits()
    {
        var (exhibit, team, _) = await SeedTeamCard();
        var card = TestData.Card(exhibit.CollectionId, "Second Card");
        await Seed(card);
        var actor = await Actor().WithSystemPermissions(SystemPermission.EditExhibits).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).PostAsJsonAsync("api/teamcards",
            new { teamId = team.Id, cardId = card.Id }, Ct));

        await using var db = NewContext();
        Assert.False(await db.TeamCards.AnyAsync(x => x.CardId == card.Id, Ct));
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

    [Fact]
    public async Task Update_persists_the_change_for_a_caller_holding_ManageExhibits()
    {
        var (_, team, teamCard) = await SeedTeamCard();
        var actor = await Actor().WithSystemPermissions(SystemPermission.ManageExhibits).SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).PutAsJsonAsync($"api/teamcards/{teamCard.Id}",
            new { id = teamCard.Id, teamId = team.Id, cardId = teamCard.CardId, move = 3, isShownOnWall = false }, Ct));

        await using var db = NewContext();
        var stored = await db.TeamCards.SingleAsync(x => x.Id == teamCard.Id, Ct);
        Assert.Equal((3, false), (stored.Move, stored.IsShownOnWall));
    }

    [Fact]
    public async Task Update_is_forbidden_for_a_caller_holding_only_EditExhibits()
    {
        var (_, team, teamCard) = await SeedTeamCard();
        var actor = await Actor().WithSystemPermissions(SystemPermission.EditExhibits).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).PutAsJsonAsync($"api/teamcards/{teamCard.Id}",
            new { id = teamCard.Id, teamId = team.Id, cardId = teamCard.CardId, move = 3 }, Ct));

        await using var db = NewContext();
        Assert.Equal(teamCard.Move, (await db.TeamCards.SingleAsync(x => x.Id == teamCard.Id, Ct)).Move);
    }

    /// <summary>The gate reads the stored TeamCard's exhibit, so a manager of another exhibit cannot take the TeamCard.</summary>
    [Fact]
    public async Task Update_is_forbidden_for_a_manager_of_the_exhibit_the_body_s_team_is_in_and_leaves_the_team_card()
    {
        var (exhibit, team, teamCard) = await SeedTeamCard();
        var mine = TestData.Exhibit(exhibit.CollectionId, "Mine");
        var myTeam = TestData.Team(mine.Id, "My Team");
        await Seed(mine, myTeam);
        var actor = await Actor().OnExhibit(mine, [ExhibitPermission.ManageExhibit]).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).PutAsJsonAsync($"api/teamcards/{teamCard.Id}",
            new { id = teamCard.Id, teamId = myTeam.Id, cardId = teamCard.CardId, move = 9 }, Ct));

        await using var db = NewContext();
        var stored = await db.TeamCards.SingleAsync(x => x.Id == teamCard.Id, Ct);
        Assert.Equal((team.Id, 0), (stored.TeamId, stored.Move));
    }

    /// <summary>A TeamCard can move to another team of its own exhibit.</summary>
    [Fact]
    public async Task Update_moves_the_team_card_to_another_team_of_its_exhibit()
    {
        var (exhibit, _, teamCard) = await SeedTeamCard();
        var other = TestData.Team(exhibit.Id, "Other Team");
        await Seed(other);
        var actor = await Actor().OnExhibit(exhibit, [ExhibitPermission.ManageExhibit]).SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).PutAsJsonAsync($"api/teamcards/{teamCard.Id}",
            new { teamId = other.Id, cardId = teamCard.CardId, move = 9 }, Ct));

        await using var db = NewContext();
        var stored = await db.TeamCards.SingleAsync(x => x.Id == teamCard.Id, Ct);
        Assert.Equal((other.Id, 9), (stored.TeamId, stored.Move));
    }

    /// <summary>A manager of both exhibits still cannot move a TeamCard to a team of the other exhibit.</summary>
    [Fact]
    public async Task Update_answers_a_team_of_another_exhibit_with_not_found_and_leaves_the_team_card()
    {
        var (exhibit, team, teamCard) = await SeedTeamCard();
        var sibling = TestData.Exhibit(exhibit.CollectionId, "Sibling");
        var siblingTeam = TestData.Team(sibling.Id, "Sibling Team");
        await Seed(sibling, siblingTeam);

        await AssertProblem(HttpStatusCode.NotFound, await RootClient.PutAsJsonAsync($"api/teamcards/{teamCard.Id}",
            new { teamId = siblingTeam.Id, cardId = teamCard.CardId }, Ct));

        await using var db = NewContext();
        Assert.Equal(team.Id, (await db.TeamCards.SingleAsync(x => x.Id == teamCard.Id, Ct)).TeamId);
    }

    /// <summary>A TeamCard can name only a card of its exhibit's collection.</summary>
    [Fact]
    public async Task Update_answers_a_card_of_another_collection_with_not_found_and_leaves_the_team_card()
    {
        var (_, team, teamCard) = await SeedTeamCard();
        var other = TestData.Collection("Other");
        var foreignCard = TestData.Card(other.Id, "Foreign");
        await Seed(other, foreignCard);

        await AssertProblem(HttpStatusCode.NotFound, await RootClient.PutAsJsonAsync($"api/teamcards/{teamCard.Id}",
            new { teamId = team.Id, cardId = foreignCard.Id }, Ct));

        await using var db = NewContext();
        Assert.Equal(teamCard.CardId, (await db.TeamCards.SingleAsync(x => x.Id == teamCard.Id, Ct)).CardId);
    }

    /// <summary>A manager of the exhibit cannot link its TeamCard to a card of a collection they cannot view.</summary>
    [Fact]
    public async Task Update_answers_a_card_of_a_collection_the_caller_cannot_view_with_not_found()
    {
        var (exhibit, team, teamCard) = await SeedTeamCard();
        var other = TestData.Collection("Other");
        var card = TestData.Card(other.Id);
        await Seed(other, card);
        var actor = await Actor().OnExhibit(exhibit, [ExhibitPermission.ManageExhibit]).SeedAsync();

        await AssertProblem(HttpStatusCode.NotFound, await Client(actor).PutAsJsonAsync($"api/teamcards/{teamCard.Id}",
            new { id = teamCard.Id, teamId = team.Id, cardId = card.Id }, Ct));

        await using var db = NewContext();
        var stored = await db.TeamCards.SingleAsync(x => x.Id == teamCard.Id, Ct);
        Assert.Equal((team.Id, teamCard.CardId), (stored.TeamId, stored.CardId));
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
    public async Task Delete_removes_the_team_card_for_a_member_holding_ManageExhibit()
    {
        var (exhibit, _, teamCard) = await SeedTeamCard();
        var actor = await Actor().OnExhibit(exhibit, [ExhibitPermission.ManageExhibit]).SeedAsync();

        await AssertStatus(HttpStatusCode.NoContent, await Client(actor).DeleteAsync($"api/teamcards/{teamCard.Id}", Ct));

        await using var db = NewContext();
        Assert.False(await db.TeamCards.AnyAsync(x => x.Id == teamCard.Id, Ct));
    }

    [Fact]
    public async Task Delete_is_forbidden_for_a_caller_holding_only_EditExhibit()
    {
        var (exhibit, _, teamCard) = await SeedTeamCard();
        var actor = await Actor().OnExhibit(exhibit, [ExhibitPermission.EditExhibit]).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).DeleteAsync($"api/teamcards/{teamCard.Id}", Ct));

        await using var db = NewContext();
        Assert.True(await db.TeamCards.AnyAsync(x => x.Id == teamCard.Id, Ct));
    }

    [Fact]
    public async Task Delete_is_forbidden_for_a_caller_holding_only_EditExhibits()
    {
        var (_, _, teamCard) = await SeedTeamCard();
        var actor = await Actor().WithSystemPermissions(SystemPermission.EditExhibits).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).DeleteAsync($"api/teamcards/{teamCard.Id}", Ct));

        await using var db = NewContext();
        Assert.True(await db.TeamCards.AnyAsync(x => x.Id == teamCard.Id, Ct));
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

    [Fact]
    public async Task DeleteByIds_removes_the_team_card_for_a_caller_holding_ManageExhibits()
    {
        var (_, team, teamCard) = await SeedTeamCard();
        var actor = await Actor().WithSystemPermissions(SystemPermission.ManageExhibits).SeedAsync();

        await AssertStatus(HttpStatusCode.NoContent, await Client(actor).DeleteAsync($"api/teams/{team.Id}/cards/{teamCard.CardId}", Ct));

        await using var db = NewContext();
        Assert.False(await db.TeamCards.AnyAsync(x => x.Id == teamCard.Id, Ct));
    }

    [Fact]
    public async Task DeleteByIds_is_forbidden_for_a_caller_holding_only_EditExhibits()
    {
        var (_, team, teamCard) = await SeedTeamCard();
        var actor = await Actor().WithSystemPermissions(SystemPermission.EditExhibits).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).DeleteAsync($"api/teams/{team.Id}/cards/{teamCard.CardId}", Ct));

        await using var db = NewContext();
        Assert.True(await db.TeamCards.AnyAsync(x => x.Id == teamCard.Id, Ct));
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
