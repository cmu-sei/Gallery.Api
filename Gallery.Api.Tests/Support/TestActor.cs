// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

// gallery.api: mirrors UserClaimsService.GetPermissionClaims. System permissions come from User.RoleId ->
// SystemRole { AllPermissions, Permissions }; collection and exhibit permissions from a membership's role
// (CollectionMembership / ExhibitMembership -> CollectionRole / ExhibitRole); team permissions from a
// TeamUser row, which also makes the user a participant of the team's exhibit and collection.

using Gallery.Api.Data;
using Gallery.Api.Data.Enumerations;
using Gallery.Api.Data.Models;

namespace Gallery.Api.Tests.Support;

/// <summary>A seeded user, and the ids of the rows seeded with them.</summary>
/// <remarks>
/// An HTTP test acts as an actor rather than as a hand-built principal: <c>ApiTestBase.Client(actor)</c>
/// puts the id on the request and the real <c>AuthorizationClaimsTransformer</c> derives the permission
/// claims from these rows.
/// </remarks>
public sealed class TestActor
{
    public required Guid Id { get; init; }

    /// <summary>Sent as the <c>name</c> claim, which <c>UserClaimsService.ValidateUser</c> writes back to the user row.</summary>
    public required string Name { get; init; }

    /// <summary>The collections <see cref="TestActorBuilder.OnNewCollection"/> minted, in call order.</summary>
    public required IReadOnlyList<Guid> NewCollectionIds { get; init; }

    /// <summary>The exhibits <see cref="TestActorBuilder.OnNewExhibit"/> minted, in call order.</summary>
    public required IReadOnlyList<Guid> NewExhibitIds { get; init; }

    /// <summary>The teams <see cref="TestActorBuilder.OnNewTeam"/> minted, in call order.</summary>
    public required IReadOnlyList<Guid> NewTeamIds { get; init; }
}

/// <summary>
/// Seeds a user, the role that grants their system permissions, and their collection, exhibit and team
/// memberships, so that the real claims transformer derives the permissions a test needs.
/// </summary>
/// <remarks>
/// <para>
/// The mapping is <c>UserClaimsService.GetPermissionClaims</c>. A collection or exhibit membership grants
/// its role's permissions, all of them when the role has <c>AllPermissions</c>; a membership that names no
/// role gets the seeded <c>Member</c> role (View and Edit) from the column default. A <c>TeamUser</c> row
/// grants <c>ParticipateTeam</c> and <c>ViewTeam</c> on its team, <c>ParticipateExhibit</c> on the team's
/// exhibit and <c>ParticipateCollection</c> on that exhibit's collection, and an observer also gets
/// <c>ViewTeam</c> on every other team of the exhibit.
/// </para>
/// <para>
/// Roles are minted per actor rather than shared, because system role names are uniquely indexed. Where a
/// seeded role says what a test means (<c>TestData.Roles.Administrator</c>,
/// <c>TestData.MembershipRoles.Manager</c>), pass its id instead.
/// </para>
/// <para>
/// Near misses: <see cref="OnNewCollection"/> and <see cref="OnNewExhibit"/> mint a resource and a
/// membership whose role grants exactly the permissions named. <see cref="OnNewTeam"/> mints a sibling team
/// in an existing exhibit; a team grants what being on it grants and nothing else, so it is the actor on
/// another team of the same exhibit.
/// </para>
/// </remarks>
public sealed class TestActorBuilder(GalleryDbContext db, CancellationToken ct)
{
    private readonly List<PendingCollection> _collections = [];
    private readonly List<PendingExhibit> _exhibits = [];
    private readonly List<PendingTeam> _teams = [];
    private Guid _id = Guid.NewGuid();
    private string _name = "Test Actor";
    private Guid? _roleId;
    private SystemPermission[] _systemPermissions;

    /// <summary>Fixes the actor's id, for a test that needs to know it before seeding.</summary>
    public TestActorBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public TestActorBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    /// <summary>Gives the actor an existing system role, such as <c>TestData.Roles.Administrator</c>.</summary>
    public TestActorBuilder WithRole(Guid roleId)
    {
        if (_systemPermissions is not null)
        {
            throw new InvalidOperationException(
                "WithRole and WithSystemPermissions both decide the actor's system role. Drop one.");
        }

        _roleId = roleId;
        return this;
    }

    /// <summary>Every system permission, by way of the seeded <c>Administrator</c> role (<c>AllPermissions</c>).</summary>
    public TestActorBuilder WithAllSystemPermissions() => WithRole(TestData.Roles.Administrator);

    /// <summary>Exactly these system permissions, by way of a role minted for this actor.</summary>
    public TestActorBuilder WithSystemPermissions(params SystemPermission[] permissions)
    {
        if (_roleId is not null)
        {
            throw new InvalidOperationException(
                "WithSystemPermissions and WithRole both decide the actor's system role. Drop one.");
        }

        _systemPermissions = permissions;
        return this;
    }

    /// <summary>
    /// Puts the actor on <paramref name="collection"/>, which must already be saved. Passing
    /// <paramref name="permissions"/> mints a collection role for this membership alone;
    /// <paramref name="roleId"/> names an existing one instead; with neither the membership gets the
    /// seeded <c>Member</c> role.
    /// </summary>
    public TestActorBuilder OnCollection(
        CollectionEntity collection,
        CollectionPermission[] permissions = null,
        Guid? roleId = null)
    {
        ArgumentNullException.ThrowIfNull(collection);
        RequireOne(permissions, roleId, $"collection {collection.Id}");

        _collections.Add(new PendingCollection(collection.Id, permissions, roleId));
        return this;
    }

    /// <summary>
    /// Puts the actor on a new collection with a role of exactly <paramref name="permissions"/>: the near
    /// miss of a denied test. The minted id is on <see cref="TestActor.NewCollectionIds"/>.
    /// </summary>
    public TestActorBuilder OnNewCollection(params CollectionPermission[] permissions)
    {
        _collections.Add(new PendingCollection(null, permissions, null));
        return this;
    }

    /// <summary>
    /// Puts the actor on <paramref name="exhibit"/>, which must already be saved. Permissions and role as
    /// for <see cref="OnCollection"/>.
    /// </summary>
    public TestActorBuilder OnExhibit(
        ExhibitEntity exhibit,
        ExhibitPermission[] permissions = null,
        Guid? roleId = null)
    {
        ArgumentNullException.ThrowIfNull(exhibit);
        RequireOne(permissions, roleId, $"exhibit {exhibit.Id}");

        _exhibits.Add(new PendingExhibit(exhibit.Id, null, permissions, roleId));
        return this;
    }

    /// <summary>
    /// Puts the actor on a new exhibit of <paramref name="collectionId"/> (already saved) with a role of
    /// exactly <paramref name="permissions"/>: the near miss of a denied test. The minted id is on
    /// <see cref="TestActor.NewExhibitIds"/>.
    /// </summary>
    public TestActorBuilder OnNewExhibit(Guid collectionId, params ExhibitPermission[] permissions)
    {
        _exhibits.Add(new PendingExhibit(null, collectionId, permissions, null));
        return this;
    }

    /// <summary>
    /// Puts the actor on <paramref name="team"/> as a participant, or an observer of its exhibit. The team
    /// and its exhibit must already be saved.
    /// </summary>
    public TestActorBuilder OnTeam(TeamEntity team, bool observer = false)
    {
        ArgumentNullException.ThrowIfNull(team);

        if (team.ExhibitId is null)
        {
            throw new InvalidOperationException(
                $"Team {team.Id} has no ExhibitId. UserClaimsService ignores a team outside an exhibit, so " +
                "the membership would grant nothing.");
        }

        _teams.Add(new PendingTeam(team.Id, null, observer));
        return this;
    }

    /// <summary>
    /// Puts the actor on a new team of <paramref name="exhibitId"/> (already saved): the near miss of a
    /// team-gated test, a participant on a sibling team. The minted id is on
    /// <see cref="TestActor.NewTeamIds"/>.
    /// </summary>
    public TestActorBuilder OnNewTeam(Guid exhibitId, bool observer = false)
    {
        _teams.Add(new PendingTeam(null, exhibitId, observer));
        return this;
    }

    /// <summary>Writes the actor and everything above to the database.</summary>
    public async Task<TestActor> SeedAsync()
    {
        var roleId = _roleId;

        if (_systemPermissions is not null)
        {
            var role = TestData.SystemRole(permissions: _systemPermissions);
            db.SystemRoles.Add(role);
            roleId = role.Id;
        }

        db.Users.Add(TestData.User(_id, _name, roleId));

        List<Guid> newCollections = [];
        List<Guid> newExhibits = [];
        List<Guid> newTeams = [];

        foreach (var pending in _collections)
        {
            var collectionId = pending.CollectionId ?? Mint(TestData.Collection("Near Miss Collection"), newCollections);
            var membershipRole = pending.Permissions is null ? pending.RoleId : MintCollectionRole(pending.Permissions);

            db.CollectionMemberships.Add(TestData.CollectionMembership(collectionId, _id, membershipRole));
        }

        foreach (var pending in _exhibits)
        {
            var exhibitId = pending.ExhibitId ?? MintExhibit(pending.CollectionId.Value, newExhibits);
            var membershipRole = pending.Permissions is null ? pending.RoleId : MintExhibitRole(pending.Permissions);

            db.ExhibitMemberships.Add(TestData.ExhibitMembership(exhibitId, _id, membershipRole));
        }

        foreach (var pending in _teams)
        {
            var teamId = pending.TeamId ?? MintTeam(pending.ExhibitId.Value, newTeams);

            db.TeamUsers.Add(TestData.TeamUser(teamId, _id, pending.Observer));
        }

        await db.SaveChangesAsync(ct);

        return new TestActor
        {
            Id = _id,
            Name = _name,
            NewCollectionIds = newCollections,
            NewExhibitIds = newExhibits,
            NewTeamIds = newTeams
        };
    }

    private Guid Mint(CollectionEntity collection, List<Guid> minted)
    {
        db.Collections.Add(collection);
        minted.Add(collection.Id);
        return collection.Id;
    }

    private Guid MintExhibit(Guid collectionId, List<Guid> minted)
    {
        var exhibit = TestData.Exhibit(collectionId, "Near Miss Exhibit");
        db.Exhibits.Add(exhibit);
        minted.Add(exhibit.Id);
        return exhibit.Id;
    }

    private Guid MintTeam(Guid exhibitId, List<Guid> minted)
    {
        var team = TestData.Team(exhibitId, "Near Miss Team");
        db.Teams.Add(team);
        minted.Add(team.Id);
        return team.Id;
    }

    private Guid MintCollectionRole(CollectionPermission[] permissions)
    {
        var role = TestData.CollectionRole(permissions);
        db.CollectionRoles.Add(role);
        return role.Id;
    }

    private Guid MintExhibitRole(ExhibitPermission[] permissions)
    {
        var role = TestData.ExhibitRole(permissions);
        db.ExhibitRoles.Add(role);
        return role.Id;
    }

    private static void RequireOne<T>(T[] permissions, Guid? roleId, string what)
    {
        if (permissions is not null && roleId is not null)
        {
            throw new InvalidOperationException(
                $"The membership on {what} names a role and also asks for permissions, which would mint a " +
                "second role. Pass one or the other.");
        }
    }

    private sealed record PendingCollection(Guid? CollectionId, CollectionPermission[] Permissions, Guid? RoleId);

    private sealed record PendingExhibit(Guid? ExhibitId, Guid? CollectionId, ExhibitPermission[] Permissions, Guid? RoleId);

    private sealed record PendingTeam(Guid? TeamId, Guid? ExhibitId, bool Observer);
}
