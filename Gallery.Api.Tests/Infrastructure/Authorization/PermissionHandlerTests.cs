// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Text.Json;
using Gallery.Api.Data.Enumerations;
using Gallery.Api.Infrastructure.Authorization;
using Gallery.Api.Tests.Support;

namespace Gallery.Api.Tests.Infrastructure.Authorization;

/// <summary>
/// The four requirement handlers, run directly against principals in the shapes the claims transformer
/// produces (and some it cannot). Any one of the required permissions satisfies a requirement.
/// </summary>
public class PermissionHandlerTests
{
    [Fact]
    public async Task SystemPermissionHandler_succeeds_on_any_one_of_the_required_permissions()
    {
        var user = new ClaimsPrincipalBuilder().WithSystemPermissions(SystemPermission.ViewExhibits).Build();

        var context = await AuthorizationHarness.HandleAsync(new SystemPermissionHandler(),
            new SystemPermissionRequirement([SystemPermission.ViewCollections, SystemPermission.ViewExhibits]), user);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task SystemPermissionHandler_does_not_succeed_on_a_neighbouring_permission()
    {
        var user = new ClaimsPrincipalBuilder().WithSystemPermissions(SystemPermission.ViewRoles).Build();

        var context = await AuthorizationHarness.HandleAsync(new SystemPermissionHandler(),
            new SystemPermissionRequirement([SystemPermission.ManageRoles]), user);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task SystemPermissionHandler_ignores_a_value_that_is_not_a_permission_name()
    {
        var user = new ClaimsPrincipalBuilder().WithRawSystemPermission("manageroles").Build();

        var context = await AuthorizationHarness.HandleAsync(new SystemPermissionHandler(),
            new SystemPermissionRequirement([SystemPermission.ManageRoles]), user);

        Assert.False(context.HasSucceeded);
    }

    /// <summary>An empty or missing list grants nothing, even to a principal holding every permission.</summary>
    [Fact]
    public async Task SystemPermissionHandler_does_not_succeed_on_an_empty_or_missing_requirement()
    {
        var user = new ClaimsPrincipalBuilder().WithSystemPermissions(Enum.GetValues<SystemPermission>()).Build();

        var empty = await AuthorizationHarness.HandleAsync(new SystemPermissionHandler(), new SystemPermissionRequirement([]), user);
        var missing = await AuthorizationHarness.HandleAsync(new SystemPermissionHandler(), new SystemPermissionRequirement(null), user);

        Assert.False(empty.HasSucceeded);
        Assert.False(missing.HasSucceeded);
    }

    /// <summary>
    /// Every participant has a claim for their team, exhibit and collection, so an empty list must not pass on the
    /// claim alone.
    /// </summary>
    [Fact]
    public async Task The_scoped_handlers_do_not_succeed_on_an_empty_requirement_for_a_caller_with_a_claim_for_the_scope()
    {
        var (collectionId, exhibitId, teamId) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var user = new ClaimsPrincipalBuilder()
            .WithCollection(collectionId, CollectionPermission.ViewCollection)
            .WithExhibit(exhibitId, ExhibitPermission.ViewExhibit)
            .WithTeam(teamId, TeamPermission.ViewTeam)
            .Build();

        var collection = await AuthorizationHarness.HandleAsync(new CollectionPermissionHandler(), new CollectionPermissionRequirement([], collectionId), user);
        var exhibit = await AuthorizationHarness.HandleAsync(new ExhibitPermissionHandler(), new ExhibitPermissionRequirement([], exhibitId), user);
        var team = await AuthorizationHarness.HandleAsync(new TeamPermissionHandler(), new TeamPermissionRequirement([], teamId), user);

        Assert.Equal((false, false, false), (collection.HasSucceeded, exhibit.HasSucceeded, team.HasSucceeded));
    }

    [Fact]
    public async Task CollectionPermissionHandler_succeeds_on_the_permission_for_that_collection()
    {
        var collectionId = Guid.NewGuid();
        var user = new ClaimsPrincipalBuilder().WithCollection(collectionId, CollectionPermission.EditCollection).Build();

        var context = await AuthorizationHarness.HandleAsync(new CollectionPermissionHandler(),
            new CollectionPermissionRequirement([CollectionPermission.EditCollection], collectionId), user);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task CollectionPermissionHandler_fails_on_the_permission_for_another_collection()
    {
        var user = new ClaimsPrincipalBuilder().WithCollection(Guid.NewGuid(), CollectionPermission.EditCollection).Build();

        var context = await AuthorizationHarness.HandleAsync(new CollectionPermissionHandler(),
            new CollectionPermissionRequirement([CollectionPermission.EditCollection], Guid.NewGuid()), user);

        Assert.True(context.HasFailed);
    }

    /// <summary>A claim for the collection with the wrong permission neither succeeds nor fails, so another handler may still succeed.</summary>
    [Fact]
    public async Task CollectionPermissionHandler_neither_succeeds_nor_fails_on_a_neighbouring_permission()
    {
        var collectionId = Guid.NewGuid();
        var user = new ClaimsPrincipalBuilder().WithCollection(collectionId, CollectionPermission.ViewCollection).Build();

        var context = await AuthorizationHarness.HandleAsync(new CollectionPermissionHandler(),
            new CollectionPermissionRequirement([CollectionPermission.ManageCollection], collectionId), user);

        Assert.Equal((false, false), (context.HasSucceeded, context.HasFailed));
    }

    [Fact]
    public async Task ExhibitPermissionHandler_succeeds_on_the_permission_for_that_exhibit()
    {
        var exhibitId = Guid.NewGuid();
        var user = new ClaimsPrincipalBuilder().WithExhibit(exhibitId, ExhibitPermission.ManageExhibit).Build();

        var context = await AuthorizationHarness.HandleAsync(new ExhibitPermissionHandler(),
            new ExhibitPermissionRequirement([ExhibitPermission.ManageExhibit], exhibitId), user);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task ExhibitPermissionHandler_fails_on_the_permission_for_another_exhibit()
    {
        var user = new ClaimsPrincipalBuilder().WithExhibit(Guid.NewGuid(), ExhibitPermission.ManageExhibit).Build();

        var context = await AuthorizationHarness.HandleAsync(new ExhibitPermissionHandler(),
            new ExhibitPermissionRequirement([ExhibitPermission.ManageExhibit], Guid.NewGuid()), user);

        Assert.True(context.HasFailed);
    }

    [Fact]
    public async Task ExhibitPermissionHandler_throws_on_a_claim_that_is_not_json()
    {
        var user = new ClaimsPrincipalBuilder().WithClaim(AuthorizationConstants.ExhibitPermissionClaimType, "not json").Build();

        await Assert.ThrowsAsync<JsonException>(() => AuthorizationHarness.HandleAsync(new ExhibitPermissionHandler(),
            new ExhibitPermissionRequirement([ExhibitPermission.ViewExhibit], Guid.NewGuid()), user));
    }

    [Fact]
    public async Task TeamPermissionHandler_succeeds_on_ViewTeam_for_that_team()
    {
        var teamId = Guid.NewGuid();
        var user = new ClaimsPrincipalBuilder().WithTeam(teamId, TeamPermission.ViewTeam).Build();

        var context = await AuthorizationHarness.HandleAsync(new TeamPermissionHandler(),
            new TeamPermissionRequirement([TeamPermission.ViewTeam], teamId), user);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task TeamPermissionHandler_fails_on_ViewTeam_for_another_team()
    {
        var user = new ClaimsPrincipalBuilder().WithTeam(Guid.NewGuid(), TeamPermission.ViewTeam).Build();

        var context = await AuthorizationHarness.HandleAsync(new TeamPermissionHandler(),
            new TeamPermissionRequirement([TeamPermission.ViewTeam], Guid.NewGuid()), user);

        Assert.True(context.HasFailed);
    }

    /// <summary>The team gate has no system permission path: every system permission still fails it.</summary>
    [Fact]
    public async Task TeamPermissionHandler_fails_a_principal_holding_every_system_permission()
    {
        // Same case as TeamControllerTests.Get_is_forbidden_for_a_caller_holding_every_system_permission.
        var user = new ClaimsPrincipalBuilder().WithSystemPermissions(Enum.GetValues<SystemPermission>()).Build();

        var context = await AuthorizationHarness.HandleAsync(new TeamPermissionHandler(),
            new TeamPermissionRequirement([TeamPermission.ViewTeam], Guid.NewGuid()), user);

        Assert.True(context.HasFailed);
    }
}
