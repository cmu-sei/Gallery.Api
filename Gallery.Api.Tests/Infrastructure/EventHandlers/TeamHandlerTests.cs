// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Net;
using System.Net.Http.Json;
using Crucible.Common.EntityEvents.Events;
using Gallery.Api.Data.Models;
using Gallery.Api.Hubs;
using Gallery.Api.Infrastructure.EventHandlers;
using Gallery.Api.Tests.Support;
using Gallery.Api.ViewModels;

namespace Gallery.Api.Tests.Infrastructure.EventHandlers;

/// <summary>
/// The team handlers, reached through a request, or driven directly with a team whose team users are
/// loaded, which no request leaves on the saved entity.
/// </summary>
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

    /// <summary>A TeamCreated or TeamUpdated payload carries the users of the team users the entity has loaded.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task A_created_or_updated_team_s_payload_carries_its_users(bool created)
    {
        var team = TestData.Team(Guid.NewGuid());
        var user = TestData.User(name: "On The Team");
        user.Email = "member@example.com";
        team.TeamUsers.Add(new TeamUserEntity { Id = Guid.NewGuid(), TeamId = team.Id, UserId = user.Id, User = user });
        var hub = Factory.Hub<MainHub>();

        if (created)
            await new TeamCreatedSignalRHandler(null, null, hub).Handle(new EntityCreated<TeamEntity>(team), Ct);
        else
            await new TeamUpdatedSignalRHandler(null, null, hub).Handle(new EntityUpdated<TeamEntity>(team, ["Name"]), Ct);

        var payload = Assert.IsType<Team>(Assert.Single(hub.ToGroup(team.Id)).Arguments[0]);
        var member = Assert.Single(payload.Users);
        Assert.Equal((user.Id, "On The Team", "member@example.com"), (member.Id, member.Name, member.Email));
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
