// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

// gallery.api: the claims service is UserClaimsService(context, IMemoryCache, ClaimsTransformationOptions),
// with caching and the IdP role and group lookups off, as TestConfiguration sets them for the host.

using System.Security.Claims;
using Gallery.Api.Data.Enumerations;
using Gallery.Api.Infrastructure.Authorization;
using Gallery.Api.Infrastructure.Options;
using Gallery.Api.Services;
using Microsoft.Extensions.Caching.Memory;

namespace Gallery.Api.Tests.Support;

/// <summary>Tests for <see cref="TestActorBuilder"/>: an actor that holds more than asked turns an authorization test into a formality.</summary>
public class TestActorTests(DatabaseFixture fixture) : DatabaseTestBase(fixture)
{
    [Fact]
    public async Task WithAllSystemPermissions_grants_every_system_permission()
    {
        var actor = await Actor().WithAllSystemPermissions().SeedAsync();

        Assert.Equal(Enum.GetNames<SystemPermission>().Order(), Permissions(await ClaimsOf(actor)));
    }

    [Fact]
    public async Task WithSystemPermissions_grants_exactly_what_it_names()
    {
        var first = Enum.GetValues<SystemPermission>()[0];
        var actor = await Actor().WithSystemPermissions(first).SeedAsync();

        Assert.Equal([first.ToString()], Permissions(await ClaimsOf(actor)));
    }

    [Fact]
    public async Task An_actor_with_no_role_holds_nothing()
    {
        var actor = await Actor().SeedAsync();

        Assert.DoesNotContain((await ClaimsOf(actor)).Claims, x => x.Type.EndsWith("Permission"));
    }

    [Fact]
    public void WithRole_after_WithSystemPermissions_throws()
    {
        var builder = Actor().WithSystemPermissions(Enum.GetValues<SystemPermission>()[0]);

        Assert.Throws<InvalidOperationException>(() => builder.WithRole(TestData.Roles.Administrator));
    }

    [Fact]
    public void OnCollection_with_both_a_role_and_permissions_throws()
    {
        var collection = TestData.Collection();

        Assert.Throws<InvalidOperationException>(() => Actor().OnCollection(
            collection, [CollectionPermission.ViewCollection], TestData.MembershipRoles.Manager));
    }

    [Fact]
    public void OnTeam_with_a_team_outside_an_exhibit_throws()
    {
        var team = new Gallery.Api.Data.Models.TeamEntity { Id = Guid.NewGuid(), Name = "Loose" };

        Assert.Throws<InvalidOperationException>(() => Actor().OnTeam(team));
    }

    [Fact]
    public async Task OnCollection_with_permissions_grants_exactly_those_on_that_collection()
    {
        var collection = TestData.Collection();
        await Seed(collection);

        var actor = await Actor().OnCollection(collection, [CollectionPermission.EditCollection]).SeedAsync();

        var claim = Assert.Single(CollectionClaims(await ClaimsOf(actor)));
        Assert.Equal(collection.Id, claim.CollectionId);
        Assert.Equal([CollectionPermission.EditCollection], claim.Permissions);
    }

    [Fact]
    public async Task OnCollection_with_no_role_grants_the_seeded_member_role()
    {
        var collection = TestData.Collection();
        await Seed(collection);

        var actor = await Actor().OnCollection(collection).SeedAsync();

        var claim = Assert.Single(CollectionClaims(await ClaimsOf(actor)));
        Assert.Equal([CollectionPermission.ViewCollection, CollectionPermission.EditCollection], claim.Permissions.Order());
    }

    [Fact]
    public async Task OnCollection_with_the_manager_role_grants_every_collection_permission()
    {
        var collection = TestData.Collection();
        await Seed(collection);

        var actor = await Actor().OnCollection(collection, roleId: TestData.MembershipRoles.Manager).SeedAsync();

        var claim = Assert.Single(CollectionClaims(await ClaimsOf(actor)));
        Assert.Equal(Enum.GetValues<CollectionPermission>(), claim.Permissions.Order());
    }

    [Fact]
    public async Task OnNewCollection_grants_exactly_what_it_names()
    {
        var actor = await Actor().OnNewCollection(CollectionPermission.ViewCollection).SeedAsync();

        var claim = Assert.Single(CollectionClaims(await ClaimsOf(actor)));
        Assert.Equal(Assert.Single(actor.NewCollectionIds), claim.CollectionId);
        Assert.Equal([CollectionPermission.ViewCollection], claim.Permissions);
    }

    [Fact]
    public async Task OnExhibit_with_permissions_grants_exactly_those_on_that_exhibit()
    {
        var collection = TestData.Collection();
        var exhibit = TestData.Exhibit(collection.Id);
        await Seed(collection, exhibit);

        var actor = await Actor().OnExhibit(exhibit, [ExhibitPermission.ManageExhibit]).SeedAsync();

        var claim = Assert.Single(ExhibitClaims(await ClaimsOf(actor)));
        Assert.Equal(exhibit.Id, claim.ExhibitId);
        Assert.Equal([ExhibitPermission.ManageExhibit], claim.Permissions);
    }

    [Fact]
    public async Task OnNewExhibit_grants_exactly_what_it_names()
    {
        var collection = TestData.Collection();
        await Seed(collection);

        var actor = await Actor().OnNewExhibit(collection.Id, ExhibitPermission.ViewExhibit).SeedAsync();

        var principal = await ClaimsOf(actor);
        var claim = Assert.Single(ExhibitClaims(principal));
        Assert.Equal(Assert.Single(actor.NewExhibitIds), claim.ExhibitId);
        Assert.Equal([ExhibitPermission.ViewExhibit], claim.Permissions);
        Assert.Empty(CollectionClaims(principal));
    }

    /// <summary>
    /// Being on a team is what grants team permissions, and it also makes the actor a participant of the
    /// team's exhibit and collection; nothing more.
    /// </summary>
    [Fact]
    public async Task OnTeam_grants_participation_in_the_team_its_exhibit_and_its_collection()
    {
        var collection = TestData.Collection();
        var exhibit = TestData.Exhibit(collection.Id);
        var team = TestData.Team(exhibit.Id);
        await Seed(collection, exhibit, team);

        var actor = await Actor().OnTeam(team).SeedAsync();

        var principal = await ClaimsOf(actor);
        var teamClaim = Assert.Single(TeamClaims(principal));
        Assert.Equal(team.Id, teamClaim.TeamId);
        Assert.Equal([TeamPermission.ViewTeam, TeamPermission.ParticipateTeam], teamClaim.Permissions.Order());
        Assert.Equal([ExhibitPermission.ParticipateExhibit], Assert.Single(ExhibitClaims(principal)).Permissions);
        Assert.Equal([CollectionPermission.ParticipateCollection], Assert.Single(CollectionClaims(principal)).Permissions);
        Assert.Empty(Permissions(principal));
    }

    [Fact]
    public async Task OnTeam_as_an_observer_also_grants_ViewTeam_on_the_other_teams_of_the_exhibit()
    {
        var collection = TestData.Collection();
        var exhibit = TestData.Exhibit(collection.Id);
        var team = TestData.Team(exhibit.Id);
        var other = TestData.Team(exhibit.Id, "Other Team");
        await Seed(collection, exhibit, team, other);

        var actor = await Actor().OnTeam(team, observer: true).SeedAsync();

        var otherClaim = Assert.Single(TeamClaims(await ClaimsOf(actor)), x => x.TeamId == other.Id);
        Assert.Equal([TeamPermission.ViewTeam], otherClaim.Permissions);
    }

    [Fact]
    public async Task OnNewTeam_puts_the_actor_on_a_sibling_team_only()
    {
        var collection = TestData.Collection();
        var exhibit = TestData.Exhibit(collection.Id);
        var team = TestData.Team(exhibit.Id);
        await Seed(collection, exhibit, team);

        var actor = await Actor().OnNewTeam(exhibit.Id).SeedAsync();

        var claim = Assert.Single(TeamClaims(await ClaimsOf(actor)));
        Assert.Equal(Assert.Single(actor.NewTeamIds), claim.TeamId);
        Assert.NotEqual(team.Id, claim.TeamId);
    }

    private TestActorBuilder Actor() => new(Db, Ct);

    private async Task<ClaimsPrincipal> ClaimsOf(TestActor actor)
    {
        await using var context = NewContext();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new UserClaimsService(context, cache, new ClaimsTransformationOptions
        {
            EnableCaching = false,
            UseRolesFromIdP = false,
            UseGroupsFromIdP = false
        });

        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("sub", actor.Id.ToString()), new Claim("name", actor.Name)], "Test"));

        return await service.AddUserClaims(principal, update: false);
    }

    private static string[] Permissions(ClaimsPrincipal principal) =>
        [.. principal.Claims
            .Where(x => x.Type == AuthorizationConstants.PermissionClaimType)
            .Select(x => x.Value)
            .Order()];

    private static CollectionPermissionClaim[] CollectionClaims(ClaimsPrincipal principal) =>
        [.. principal.Claims
            .Where(x => x.Type == AuthorizationConstants.CollectionPermissionClaimType)
            .Select(x => CollectionPermissionClaim.FromString(x.Value))];

    private static ExhibitPermissionClaim[] ExhibitClaims(ClaimsPrincipal principal) =>
        [.. principal.Claims
            .Where(x => x.Type == AuthorizationConstants.ExhibitPermissionClaimType)
            .Select(x => ExhibitPermissionClaim.FromString(x.Value))];

    private static TeamPermissionClaim[] TeamClaims(ClaimsPrincipal principal) =>
        [.. principal.Claims
            .Where(x => x.Type == AuthorizationConstants.TeamPermissionClaimType)
            .Select(x => TeamPermissionClaim.FromString(x.Value))];
}
