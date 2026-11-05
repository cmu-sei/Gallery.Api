// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Security.Claims;
using Gallery.Api.Services;
using Gallery.Api.Tests.Support;
using Microsoft.Extensions.Logging.Abstractions;

namespace Gallery.Api.Tests.Services;

/// <summary>
/// <c>TeamService</c> driven directly, for <c>GetAsync</c> without an id: no route reaches it, so the
/// projection it reads with is tested here.
/// </summary>
public class TeamServiceTests(DatabaseFixture fixture) : DatabaseTestBase(fixture)
{
    /// <summary>Every team is read with its members and without its users, which this projection leaves empty.</summary>
    [Fact]
    public async Task GetAll_returns_each_team_with_its_members_and_no_users()
    {
        var collection = TestData.Collection();
        var exhibit = TestData.Exhibit(collection.Id);
        var team = TestData.Team(exhibit.Id, "Listed Team", "listed@example.com");
        team.ShortName = "LT";
        var user = TestData.User(name: "On The Team");
        await Seed(collection, exhibit, team, user, TestData.TeamUser(team.Id, user.Id));
        await using var context = NewContext();

        var teams = await new TeamService(context, new ClaimsPrincipal(), null, NullLogger<ITeamService>.Instance).GetAsync(Ct);

        var got = Assert.Single(teams, x => x.Id == team.Id);
        Assert.Equal(("Listed Team", "LT", "listed@example.com", (Guid?)exhibit.Id), (got.Name, got.ShortName, got.Email, got.ExhibitId));
        Assert.Empty(got.Users);
    }
}
