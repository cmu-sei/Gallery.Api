// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Security.Claims;
using Gallery.Api.Data;
using Gallery.Api.Data.Enumerations;
using Gallery.Api.Hubs;
using Gallery.Api.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace Gallery.Api.Tests.Hubs;

/// <summary>
/// <see cref="MainHub"/>'s methods, driven directly with a <see cref="HubHarness"/>: which groups a
/// connection joins. The hub's authorization service is the real one over the real handlers, reading the
/// principal the claims transformer would have produced (built with <see cref="ClaimsPrincipalBuilder"/>).
/// </summary>
public class MainHubTests(DatabaseFixture fixture) : DatabaseTestBase(fixture)
{
    [Fact]
    public async Task Join_adds_the_connection_to_the_caller_s_own_group()
    {
        var principal = new ClaimsPrincipalBuilder().Build();
        var (hub, harness) = Hub(principal);

        await hub.Join();

        await harness.Groups.Received(1).AddToGroupAsync(HubHarness.ConnectionId, harness.UserId.ToString(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Leave_removes_the_connection_from_the_caller_s_own_group()
    {
        var principal = new ClaimsPrincipalBuilder().Build();
        var (hub, harness) = Hub(principal);

        await hub.Leave();

        await harness.Groups.Received(1).RemoveFromGroupAsync(HubHarness.ConnectionId, harness.UserId.ToString(), Arg.Any<CancellationToken>());
    }

    /// <summary>Switching to a team the caller may view joins the team's group and its exhibit's group.</summary>
    [Fact]
    public async Task SwitchTeam_joins_the_new_team_and_its_exhibit_for_a_caller_holding_ViewTeam_on_it()
    {
        var (exhibit, oldTeam, newTeam) = await SeedTeams();
        var principal = new ClaimsPrincipalBuilder()
            .WithTeam(oldTeam.Id, TeamPermission.ViewTeam)
            .WithTeam(newTeam.Id, TeamPermission.ViewTeam)
            .Build();
        var (hub, harness) = Hub(principal);

        await hub.SwitchTeam([oldTeam.Id, newTeam.Id]);

        await harness.Groups.Received(1).RemoveFromGroupAsync(HubHarness.ConnectionId, oldTeam.Id.ToString(), Arg.Any<CancellationToken>());
        await harness.Groups.Received(1).AddToGroupAsync(HubHarness.ConnectionId, newTeam.Id.ToString(), Arg.Any<CancellationToken>());
        await harness.Groups.Received(1).AddToGroupAsync(HubHarness.ConnectionId, exhibit.Id.ToString(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SwitchTeam_joins_nothing_for_a_caller_holding_ViewTeam_only_on_another_team()
    {
        var (_, oldTeam, newTeam) = await SeedTeams();
        var principal = new ClaimsPrincipalBuilder().WithTeam(oldTeam.Id, TeamPermission.ViewTeam).Build();
        var (hub, harness) = Hub(principal);

        await hub.SwitchTeam([oldTeam.Id, newTeam.Id]);

        await harness.Groups.DidNotReceive().AddToGroupAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task JoinAdmin_joins_every_admin_group_for_a_caller_holding_the_view_permissions()
    {
        var principal = new ClaimsPrincipalBuilder().WithSystemPermissions(
            SystemPermission.ViewExhibits, SystemPermission.ViewCollections, SystemPermission.ViewGroups,
            SystemPermission.ViewRoles, SystemPermission.ViewUsers).Build();
        var (hub, harness) = Hub(principal);

        await hub.JoinAdmin();

        string[] expected = [MainHub.EXHIBIT_GROUP, MainHub.COLLECTION_GROUP, MainHub.GROUP_GROUP, MainHub.ROLE_GROUP, MainHub.USER_GROUP, harness.UserId.ToString()];
        Assert.Equal(expected.Order(), Joined(harness).Order());
    }

    /// <summary>Without the system view permissions the caller joins the groups of the exhibits and collections they are a member of.</summary>
    [Fact]
    public async Task JoinAdmin_joins_the_member_s_exhibit_and_collection_groups_without_system_permissions()
    {
        var collection = TestData.Collection();
        var exhibit = TestData.Exhibit(collection.Id);
        var user = TestData.User();
        await Seed(collection, exhibit, user, TestData.ExhibitMembership(exhibit.Id, user.Id), TestData.CollectionMembership(collection.Id, user.Id));
        var principal = new ClaimsPrincipalBuilder().WithUserId(user.Id).Build();
        var (hub, harness) = Hub(principal);

        await hub.JoinAdmin();

        string[] expected = [user.Id.ToString(), exhibit.Id.ToString(), collection.Id.ToString()];
        Assert.Equal(expected.Order(), Joined(harness).Order());
    }

    [Fact]
    public async Task LeaveAdmin_leaves_every_admin_group_for_a_caller_holding_the_view_permissions()
    {
        var principal = new ClaimsPrincipalBuilder().WithSystemPermissions(
            SystemPermission.ViewExhibits, SystemPermission.ViewCollections, SystemPermission.ViewGroups,
            SystemPermission.ViewRoles, SystemPermission.ViewUsers).Build();
        var (hub, harness) = Hub(principal);

        await hub.LeaveAdmin();

        string[] expected = [MainHub.EXHIBIT_GROUP, MainHub.COLLECTION_GROUP, MainHub.GROUP_GROUP, MainHub.ROLE_GROUP, MainHub.USER_GROUP, harness.UserId.ToString()];
        Assert.Equal(expected.Order(), Left(harness).Order());
        await harness.Groups.DidNotReceive().AddToGroupAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LeaveAdmin_leaves_the_member_s_exhibit_group_without_system_permissions()
    {
        var collection = TestData.Collection();
        var exhibit = TestData.Exhibit(collection.Id);
        var user = TestData.User();
        await Seed(collection, exhibit, user, TestData.ExhibitMembership(exhibit.Id, user.Id));
        var principal = new ClaimsPrincipalBuilder().WithUserId(user.Id).Build();
        var (hub, harness) = Hub(principal);

        await hub.LeaveAdmin();

        string[] expected = [user.Id.ToString(), exhibit.Id.ToString()];
        Assert.Equal(expected.Order(), Left(harness).Order());
    }

    private (MainHub Hub, HubHarness Harness) Hub(ClaimsPrincipal principal)
    {
        var services = new ServiceCollection();
        services.AddScoped<GalleryDbContext>(_ => NewContext());
        var provider = services.BuildServiceProvider();

        var harness = new HubHarness(Guid.Parse(principal.FindFirst("sub").Value), principal);
        var hub = new MainHub(
            provider.GetRequiredService<IServiceScopeFactory>(),
            AuthorizationHarness.CreateGalleryAuthorizationService(principal, Db));

        return (harness.Attach(hub), harness);
    }

    private static string[] Joined(HubHarness harness) =>
        [.. harness.Groups.ReceivedCalls()
            .Where(x => x.GetMethodInfo().Name == nameof(Microsoft.AspNetCore.SignalR.IGroupManager.AddToGroupAsync))
            .Select(x => (string)x.GetArguments()[1])];

    private static string[] Left(HubHarness harness) =>
        [.. harness.Groups.ReceivedCalls()
            .Where(x => x.GetMethodInfo().Name == nameof(Microsoft.AspNetCore.SignalR.IGroupManager.RemoveFromGroupAsync))
            .Select(x => (string)x.GetArguments()[1])];

    private async Task<(Gallery.Api.Data.Models.ExhibitEntity Exhibit, Gallery.Api.Data.Models.TeamEntity Old, Gallery.Api.Data.Models.TeamEntity New)> SeedTeams()
    {
        var collection = TestData.Collection();
        var exhibit = TestData.Exhibit(collection.Id);
        var oldTeam = TestData.Team(exhibit.Id, "Old Team");
        var newTeam = TestData.Team(exhibit.Id, "New Team");
        await Seed(collection, exhibit, oldTeam, newTeam);

        return (exhibit, oldTeam, newTeam);
    }
}
