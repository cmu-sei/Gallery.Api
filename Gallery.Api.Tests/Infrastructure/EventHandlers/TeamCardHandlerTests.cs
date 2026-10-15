// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Net;
using System.Net.Http.Json;
using Gallery.Api.Hubs;
using Gallery.Api.Tests.Support;
using Gallery.Api.ViewModels;

namespace Gallery.Api.Tests.Infrastructure.EventHandlers;

/// <summary>The team card handlers, reached through a request: the wall of the team's users.</summary>
public class TeamCardHandlerTests(DatabaseFixture fixture, GalleryAppFactory factory) : ApiTestBase(fixture, factory)
{
    /// <summary>A team card change goes to the team's users, so their wall updates.</summary>
    [Fact]
    public async Task Creating_a_team_card_sends_TeamCardCreated_to_the_team_s_users()
    {
        var collection = TestData.Collection();
        var exhibit = TestData.Exhibit(collection.Id);
        var team = TestData.Team(exhibit.Id);
        var card = TestData.Card(collection.Id);
        var participant = TestData.User(name: "Participant");
        await Seed(collection, exhibit, team, card, participant, TestData.TeamUser(team.Id, participant.Id));

        await AssertStatus(HttpStatusCode.Created, await RootClient.PostAsJsonAsync("api/teamcards", new { teamId = team.Id, cardId = card.Id, isShownOnWall = true }, Ct));

        var broadcast = Assert.Single(Factory.Hub<MainHub>().ToGroup(participant.Id));
        Assert.Equal(MainHubMethods.TeamCardCreated, broadcast.Method);
        Assert.Equal(card.Id, Assert.IsType<TeamCard>(broadcast.Arguments[0]).CardId);
        Assert.Null(broadcast.Arguments[1]);
    }

    [Fact]
    public async Task Deleting_a_team_card_sends_TeamCardDeleted_with_the_bare_id()
    {
        var collection = TestData.Collection();
        var exhibit = TestData.Exhibit(collection.Id);
        var team = TestData.Team(exhibit.Id);
        var card = TestData.Card(collection.Id);
        var teamCard = TestData.TeamCard(team.Id, card.Id);
        await Seed(collection, exhibit, team, card, teamCard);

        await AssertStatus(HttpStatusCode.NoContent, await RootClient.DeleteAsync($"api/teamcards/{teamCard.Id}", Ct));

        var broadcast = Assert.Single(Factory.Hub<MainHub>().ToGroup(teamCard.Id));
        Assert.Equal((MainHubMethods.TeamCardDeleted, (object)teamCard.Id), (broadcast.Method, broadcast.Argument));
    }
}
