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

/// <summary><c>CardController</c> over HTTP: the cards of a collection, gated on the collection.</summary>
public class CardControllerTests(DatabaseFixture fixture, GalleryAppFactory factory) : ApiTestBase(fixture, factory)
{
    [Fact]
    public async Task GetAll_returns_every_card_to_a_caller_holding_ViewExhibits()
    {
        var (_, card) = await SeedCard();
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewExhibits).SeedAsync();

        var cards = await ReadAsync<List<Card>>(await Client(actor).GetAsync("api/cards", Ct));

        Assert.Contains(cards, x => x.Id == card.Id);
    }

    [Fact]
    public async Task GetAll_is_forbidden_for_a_caller_holding_only_CreateCollections()
    {
        var actor = await Actor().WithSystemPermissions(SystemPermission.CreateCollections).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).GetAsync("api/cards", Ct));
    }

    /// <summary>A request with no identity is answered with a 401; the anonymous client is the case under test.</summary>
    [Fact]
    public async Task GetAll_without_an_identity_is_unauthorized()
    {
        await AssertStatus(HttpStatusCode.Unauthorized, await Client().GetAsync("api/cards", Ct));
    }

    [Fact]
    public async Task GetByCollection_returns_the_cards_to_a_member_holding_ViewCollection()
    {
        var (collection, card) = await SeedCard();
        var actor = await Actor().OnCollection(collection, [CollectionPermission.ViewCollection]).SeedAsync();

        var cards = await ReadAsync<List<Card>>(await Client(actor).GetAsync($"api/collections/{collection.Id}/cards", Ct));

        Assert.Equal(card.Id, Assert.Single(cards).Id);
    }

    [Fact]
    public async Task GetByCollection_is_forbidden_for_a_caller_holding_ViewCollection_only_on_another_collection()
    {
        var (collection, _) = await SeedCard();
        var actor = await Actor().OnNewCollection(CollectionPermission.ViewCollection).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/collections/{collection.Id}/cards", Ct));
    }

    [Fact]
    public async Task GetByExhibit_returns_the_collection_s_cards_to_a_member_holding_ViewExhibit()
    {
        var (collection, card) = await SeedCard();
        var exhibit = TestData.Exhibit(collection.Id);
        await Seed(exhibit);
        var actor = await Actor().OnExhibit(exhibit, [ExhibitPermission.ViewExhibit]).SeedAsync();

        var cards = await ReadAsync<List<Card>>(await Client(actor).GetAsync($"api/exhibits/{exhibit.Id}/cards", Ct));

        Assert.Equal(card.Id, Assert.Single(cards).Id);
    }

    [Fact]
    public async Task GetByExhibit_is_forbidden_for_a_caller_holding_only_ViewCollection_on_its_collection()
    {
        var (collection, _) = await SeedCard();
        var exhibit = TestData.Exhibit(collection.Id);
        await Seed(exhibit);
        var actor = await Actor().OnCollection(collection, [CollectionPermission.ViewCollection]).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/exhibits/{exhibit.Id}/cards", Ct));
    }

    [Fact]
    public async Task GetByExhibitTeam_returns_the_team_s_cards_to_a_participant_of_the_team()
    {
        var (collection, card) = await SeedCard();
        var exhibit = TestData.Exhibit(collection.Id);
        var team = TestData.Team(exhibit.Id);
        await Seed(exhibit, team, TestData.TeamCard(team.Id, card.Id));
        var actor = await Actor().OnTeam(team).SeedAsync();

        var cards = await ReadAsync<List<Card>>(await Client(actor).GetAsync($"api/exhibits/{exhibit.Id}/teams/{team.Id}/cards", Ct));

        Assert.Equal(card.Id, Assert.Single(cards).Id);
    }

    [Fact]
    public async Task GetByExhibitTeam_is_forbidden_for_a_caller_holding_ViewTeam_only_on_another_team()
    {
        var (collection, card) = await SeedCard();
        var exhibit = TestData.Exhibit(collection.Id);
        var team = TestData.Team(exhibit.Id);
        await Seed(exhibit, team, TestData.TeamCard(team.Id, card.Id));
        var actor = await Actor().OnNewTeam(exhibit.Id).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/exhibits/{exhibit.Id}/teams/{team.Id}/cards", Ct));
    }

    [Fact]
    public async Task Get_returns_the_card_to_a_caller_holding_ViewCollections()
    {
        var (_, card) = await SeedCard("Readable Card");
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewCollections).SeedAsync();

        var got = await ReadAsync<Card>(await Client(actor).GetAsync($"api/cards/{card.Id}", Ct));

        Assert.Equal("Readable Card", got.Name);
    }

    [Fact]
    public async Task Get_is_forbidden_for_a_caller_holding_only_ViewExhibits()
    {
        var (_, card) = await SeedCard();
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewExhibits).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/cards/{card.Id}", Ct));
    }

    [Fact]
    public async Task Get_reports_an_unknown_card_as_not_found()
    {
        await AssertProblem(HttpStatusCode.NotFound, await RootClient.GetAsync($"api/cards/{Guid.NewGuid()}", Ct));
    }

    [Fact]
    public async Task Create_persists_the_card_for_a_member_holding_EditCollection()
    {
        var (collection, _) = await SeedCard();
        var actor = await Actor().OnCollection(collection, [CollectionPermission.EditCollection]).SeedAsync();

        var response = await Client(actor).PostAsJsonAsync("api/cards", new { name = "New Card", collectionId = collection.Id, move = 1 }, Ct);

        await AssertStatus(HttpStatusCode.Created, response);
        var created = await ReadAsync<Card>(response);
        await using var db = NewContext();
        Assert.Equal(collection.Id, (await db.Cards.SingleAsync(x => x.Id == created.Id, Ct)).CollectionId);
    }

    [Fact]
    public async Task Create_is_forbidden_for_a_caller_holding_only_ViewCollection()
    {
        var (collection, _) = await SeedCard();
        var actor = await Actor().OnCollection(collection, [CollectionPermission.ViewCollection]).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).PostAsJsonAsync("api/cards",
            new { name = "New Card", collectionId = collection.Id }, Ct));
    }

    [Fact]
    public async Task Create_is_forbidden_for_a_caller_holding_EditCollection_only_on_another_collection()
    {
        var (collection, _) = await SeedCard();
        var actor = await Actor().OnNewCollection(CollectionPermission.EditCollection).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).PostAsJsonAsync("api/cards",
            new { name = "New Card", collectionId = collection.Id }, Ct));
    }

    [Fact]
    public async Task Update_persists_the_change_for_a_member_holding_EditCollection()
    {
        var (collection, card) = await SeedCard("Before");
        var actor = await Actor().OnCollection(collection, [CollectionPermission.EditCollection]).SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).PutAsJsonAsync($"api/cards/{card.Id}",
            new { id = card.Id, name = "After", collectionId = collection.Id }, Ct));

        await using var db = NewContext();
        Assert.Equal("After", (await db.Cards.SingleAsync(x => x.Id == card.Id, Ct)).Name);
    }

    [Fact]
    public async Task Update_is_forbidden_for_a_caller_holding_only_ViewCollection()
    {
        var (collection, card) = await SeedCard("Before");
        var actor = await Actor().OnCollection(collection, [CollectionPermission.ViewCollection]).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).PutAsJsonAsync($"api/cards/{card.Id}",
            new { id = card.Id, name = "After", collectionId = collection.Id }, Ct));
    }

    [Fact]
    public async Task Update_persists_the_change_for_a_caller_holding_EditCollections()
    {
        var (collection, card) = await SeedCard("Before");
        var actor = await Actor().WithSystemPermissions(SystemPermission.EditCollections).SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).PutAsJsonAsync($"api/cards/{card.Id}",
            new { id = card.Id, name = "After", collectionId = collection.Id }, Ct));

        await using var db = NewContext();
        Assert.Equal("After", (await db.Cards.SingleAsync(x => x.Id == card.Id, Ct)).Name);
    }

    [Fact]
    public async Task Update_is_forbidden_for_a_caller_holding_only_ViewCollections()
    {
        var (collection, card) = await SeedCard("Before");
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewCollections).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).PutAsJsonAsync($"api/cards/{card.Id}",
            new { id = card.Id, name = "After", collectionId = collection.Id }, Ct));

        await using var db = NewContext();
        Assert.Equal("Before", (await db.Cards.SingleAsync(x => x.Id == card.Id, Ct)).Name);
    }

    /// <summary>The gate reads the collection the body names, so the card moves into the caller's collection.</summary>
    [Fact]
    public async Task Update_moves_another_collection_s_card_when_the_body_names_a_collection_the_caller_edits()
    {
        var (_, card) = await SeedCard("Theirs");
        var actor = await Actor().OnNewCollection(CollectionPermission.EditCollection).SeedAsync();
        var mine = Assert.Single(actor.NewCollectionIds);

        await AssertStatus(HttpStatusCode.OK, await Client(actor).PutAsJsonAsync($"api/cards/{card.Id}",
            new { id = card.Id, name = "Taken", collectionId = mine }, Ct));

        await using var db = NewContext();
        var stored = await db.Cards.SingleAsync(x => x.Id == card.Id, Ct);
        Assert.Equal(("Taken", mine), (stored.Name, stored.CollectionId));
    }

    [Fact]
    public async Task Delete_removes_the_card_for_a_member_holding_EditCollection()
    {
        var (collection, card) = await SeedCard();
        var actor = await Actor().OnCollection(collection, [CollectionPermission.EditCollection]).SeedAsync();

        await AssertStatus(HttpStatusCode.NoContent, await Client(actor).DeleteAsync($"api/cards/{card.Id}", Ct));

        await using var db = NewContext();
        Assert.False(await db.Cards.AnyAsync(x => x.Id == card.Id, Ct));
    }

    [Fact]
    public async Task Delete_is_forbidden_for_a_caller_holding_EditCollection_only_on_another_collection()
    {
        var (_, card) = await SeedCard();
        var actor = await Actor().OnNewCollection(CollectionPermission.EditCollection).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).DeleteAsync($"api/cards/{card.Id}", Ct));

        await using var db = NewContext();
        Assert.True(await db.Cards.AnyAsync(x => x.Id == card.Id, Ct));
    }

    [Fact]
    public async Task Delete_removes_the_card_for_a_caller_holding_EditCollections()
    {
        var (_, card) = await SeedCard();
        var actor = await Actor().WithSystemPermissions(SystemPermission.EditCollections).SeedAsync();

        await AssertStatus(HttpStatusCode.NoContent, await Client(actor).DeleteAsync($"api/cards/{card.Id}", Ct));

        await using var db = NewContext();
        Assert.False(await db.Cards.AnyAsync(x => x.Id == card.Id, Ct));
    }

    [Fact]
    public async Task Delete_is_forbidden_for_a_caller_holding_only_ViewCollections()
    {
        var (_, card) = await SeedCard();
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewCollections).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).DeleteAsync($"api/cards/{card.Id}", Ct));

        await using var db = NewContext();
        Assert.True(await db.Cards.AnyAsync(x => x.Id == card.Id, Ct));
    }

    [Fact]
    public async Task Delete_of_a_card_with_articles_is_answered_with_a_conflict()
    {
        var (collection, card) = await SeedCard();
        await Seed(TestData.Article(collection.Id, card.Id));

        var problem = await AssertProblem(HttpStatusCode.Conflict, await RootClient.DeleteAsync($"api/cards/{card.Id}", Ct));

        Assert.Equal("This card has 1 article. Delete or reassign them before deleting the card.", problem.Title);
    }

    [Fact]
    public async Task Delete_reports_an_unknown_card_as_not_found_to_a_caller_holding_EditCollections()
    {
        var actor = await Actor().WithSystemPermissions(SystemPermission.EditCollections).SeedAsync();

        await AssertProblem(HttpStatusCode.NotFound, await Client(actor).DeleteAsync($"api/cards/{Guid.NewGuid()}", Ct));
    }

    /// <summary>An id that names no card is answered with a 500 for a caller whose edit right is a collection membership.</summary>
    [Fact]
    public async Task Delete_of_an_unknown_card_answers_a_collection_editor_with_a_server_error()
    {
        var actor = await Actor().OnNewCollection(CollectionPermission.EditCollection).SeedAsync();

        var problem = await AssertProblem(HttpStatusCode.InternalServerError, await Client(actor).DeleteAsync($"api/cards/{Guid.NewGuid()}", Ct));

        Assert.Equal("Object reference not set to an instance of an object.", problem.Detail);
    }

    private async Task<(CollectionEntity Collection, CardEntity Card)> SeedCard(string name = "Test Card")
    {
        var collection = TestData.Collection();
        var card = TestData.Card(collection.Id, name);
        await Seed(collection, card);

        return (collection, card);
    }
}
