// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Net;
using System.Net.Http.Json;
using Gallery.Api.Hubs;
using Gallery.Api.Tests.Support;
using Gallery.Api.ViewModels;

namespace Gallery.Api.Tests.Infrastructure.EventHandlers;

/// <summary>The team handlers, reached through a request.</summary>
public class TeamHandlerTests(DatabaseFixture fixture, GalleryAppFactory factory) : ApiTestBase(fixture, factory)
{
    [Fact]
    public async Task Creating_a_team_sends_TeamCreated_to_its_group()
    {
        var collection = TestData.Collection();
        var exhibit = TestData.Exhibit(collection.Id);
        await Seed(collection, exhibit);

        var created = await ReadAsync<Team>(await RootClient.PostAsJsonAsync("api/teams", new { name = "Red", exhibitId = exhibit.Id }, Ct));

        var broadcast = Assert.Single(Factory.Hub<MainHub>().ToGroup(created.Id));
        Assert.Equal(MainHubMethods.TeamCreated, broadcast.Method);
        Assert.Equal("Red", Assert.IsType<Team>(broadcast.Arguments[0]).Name);
        Assert.Null(broadcast.Arguments[1]);
    }

    [Fact]
    public async Task Deleting_a_team_sends_TeamDeleted_with_the_bare_id()
    {
        var collection = TestData.Collection();
        var exhibit = TestData.Exhibit(collection.Id);
        var team = TestData.Team(exhibit.Id);
        await Seed(collection, exhibit, team);

        await AssertStatus(HttpStatusCode.NoContent, await RootClient.DeleteAsync($"api/teams/{team.Id}", Ct));

        var broadcast = Assert.Single(Factory.Hub<MainHub>().ToGroup(team.Id));
        Assert.Equal((MainHubMethods.TeamDeleted, (object)team.Id), (broadcast.Method, broadcast.Argument));
    }
}
