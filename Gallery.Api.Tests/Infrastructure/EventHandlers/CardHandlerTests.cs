// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Net;
using System.Net.Http.Json;
using Gallery.Api.Hubs;
using Gallery.Api.Tests.Support;
using Gallery.Api.ViewModels;

namespace Gallery.Api.Tests.Infrastructure.EventHandlers;

/// <summary>The card handlers, reached through a request: what each write broadcasts, and to whom.</summary>
public class CardHandlerTests(DatabaseFixture fixture, GalleryAppFactory factory) : ApiTestBase(fixture, factory)
{
    [Fact]
    public async Task Creating_a_card_sends_CardCreated_with_the_card_and_no_modified_properties()
    {
        var collection = TestData.Collection();
        await Seed(collection);

        var created = await ReadAsync<Card>(await RootClient.PostAsJsonAsync("api/cards", new { name = "Wall Card", collectionId = collection.Id }, Ct));

        var broadcast = Assert.Single(Factory.Hub<MainHub>().ToGroup(created.Id));
        Assert.Equal(MainHubMethods.CardCreated, broadcast.Method);
        Assert.Equal("Wall Card", Assert.IsType<Card>(broadcast.Arguments[0]).Name);
        Assert.Null(broadcast.Arguments[1]);
    }

    /// <summary>
    /// A card's content goes to the participants of a team the card is assigned to and to the exhibit's observers,
    /// not to a participant of a team that does not have the card.
    /// </summary>
    [Fact]
    public async Task Updating_a_card_sends_CardUpdated_to_the_participants_of_teams_the_card_is_assigned_to_only()
    {
        var collection = TestData.Collection();
        var exhibit = TestData.Exhibit(collection.Id);
        var team = TestData.Team(exhibit.Id);
        var otherTeam = TestData.Team(exhibit.Id, "Other Team");
        var observerTeam = TestData.Team(exhibit.Id, "Observers");
        var card = TestData.Card(collection.Id);
        var participant = TestData.User(name: "Participant");
        var unassigned = TestData.User(name: "Unassigned");
        var observer = TestData.User(name: "Observer");
        await Seed(collection, exhibit, team, otherTeam, observerTeam, card, participant, unassigned, observer,
            TestData.TeamUser(team.Id, participant.Id),
            TestData.TeamUser(otherTeam.Id, unassigned.Id),
            TestData.TeamUser(observerTeam.Id, observer.Id, observer: true),
            TestData.TeamCard(team.Id, card.Id));

        await AssertStatus(HttpStatusCode.OK, await RootClient.PutAsJsonAsync($"api/cards/{card.Id}", new { name = "Renamed Card" }, Ct));

        var hub = Factory.Hub<MainHub>();
        var broadcast = Assert.Single(hub.ToGroup(participant.Id));
        Assert.Equal(MainHubMethods.CardUpdated, broadcast.Method);
        Assert.Contains("name", Assert.IsType<string[]>(broadcast.Arguments[1]));
        Assert.Equal(MainHubMethods.CardUpdated, Assert.Single(hub.ToGroup(observer.Id)).Method);
        Assert.Empty(hub.ToGroup(unassigned.Id));
    }

    [Fact]
    public async Task Deleting_a_card_sends_CardDeleted_with_the_bare_id()
    {
        var collection = TestData.Collection();
        var card = TestData.Card(collection.Id);
        await Seed(collection, card);

        await AssertStatus(HttpStatusCode.NoContent, await RootClient.DeleteAsync($"api/cards/{card.Id}", Ct));

        var broadcast = Assert.Single(Factory.Hub<MainHub>().ToGroup(card.Id));
        Assert.Equal((MainHubMethods.CardDeleted, (object)card.Id), (broadcast.Method, broadcast.Argument));
    }
}
