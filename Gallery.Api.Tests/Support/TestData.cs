// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

// gallery.api: the seeded ids come from the HasData calls in Gallery.Api.Data/Models (SystemRole.cs,
// CollectionRole.cs, ExhibitRole.cs), which the migrations apply; appsettings.json's SeedData is empty.

using Gallery.Api.Data.Enumerations;
using Gallery.Api.Data.Models;

namespace Gallery.Api.Tests.Support;

/// <summary>Object mothers, and the ids of the rows the migrations seed.</summary>
public static class TestData
{
    /// <summary>Ids of the system roles the migrations seed.</summary>
    public static class Roles
    {
        /// <summary><c>Administrator</c>: immutable, all permissions.</summary>
        public static readonly Guid Administrator = SystemRoleEntityDefaults.AdministratorRoleId;

        /// <summary><c>Content Developer</c>: the View, Create and ManageExhibits permissions.</summary>
        public static readonly Guid ContentDeveloper = SystemRoleEntityDefaults.ContentDeveloperRoleId;

        /// <summary><c>Observer</c>: every View permission.</summary>
        public static readonly Guid Observer = SystemRoleEntityDefaults.ObserverRoleId;
    }

    /// <summary>
    /// Ids of the collection roles the migrations seed. The exhibit roles share the same three ids
    /// (<see cref="ExhibitRoleDefaults"/>) in their own table.
    /// </summary>
    public static class MembershipRoles
    {
        /// <summary><c>Manager</c>: all permissions.</summary>
        public static readonly Guid Manager = CollectionRoleEntityDefaults.CollectionCreatorRoleId;

        /// <summary><c>Observer</c>: View only.</summary>
        public static readonly Guid Observer = CollectionRoleEntityDefaults.CollectionReadOnlyRoleId;

        /// <summary><c>Member</c>: View and Edit, and the default of a membership that names no role.</summary>
        public static readonly Guid Member = CollectionRoleEntityDefaults.CollectionMemberRoleId;
    }

    public static UserEntity User(Guid? id = null, string name = "Test User", Guid? roleId = null) =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            Name = name,
            RoleId = roleId
        };

    /// <summary>A system role of its own, named uniquely unless a name is passed, because role names are uniquely indexed.</summary>
    public static SystemRoleEntity SystemRole(
        string name = null,
        bool allPermissions = false,
        SystemPermission[] permissions = null)
    {
        var id = Guid.NewGuid();

        return new SystemRoleEntity
        {
            Id = id,
            Name = name ?? $"role-{id:N}",
            AllPermissions = allPermissions,
            Permissions = [.. permissions ?? []]
        };
    }

    /// <summary>A collection role granting exactly <paramref name="permissions"/> (none by default).</summary>
    public static CollectionRoleEntity CollectionRole(params CollectionPermission[] permissions)
    {
        var id = Guid.NewGuid();

        return new CollectionRoleEntity { Id = id, Name = $"collection-role-{id:N}", Permissions = [.. permissions] };
    }

    /// <summary>An exhibit role granting exactly <paramref name="permissions"/> (none by default).</summary>
    public static ExhibitRoleEntity ExhibitRole(params ExhibitPermission[] permissions)
    {
        var id = Guid.NewGuid();

        return new ExhibitRoleEntity { Id = id, Name = $"exhibit-role-{id:N}", Permissions = [.. permissions] };
    }

    public static CollectionEntity Collection(string name = "Test Collection") =>
        new() { Id = Guid.NewGuid(), Name = name, Description = "Seeded by TestData.Collection" };

    public static CardEntity Card(Guid collectionId, string name = "Test Card", int move = 0, int inject = 0) =>
        new() { Id = Guid.NewGuid(), CollectionId = collectionId, Name = name, Move = move, Inject = inject };

    /// <summary>
    /// A collection article, or with <paramref name="exhibitId"/> one a participant posted during that
    /// exhibit.
    /// </summary>
    public static ArticleEntity Article(
        Guid collectionId,
        Guid? cardId = null,
        Guid? exhibitId = null,
        string name = "Test Article",
        int move = 0,
        int inject = 0) =>
        new()
        {
            Id = Guid.NewGuid(),
            CollectionId = collectionId,
            CardId = cardId,
            ExhibitId = exhibitId,
            Name = name,
            Summary = "Seeded by TestData.Article",
            Move = move,
            Inject = inject,
            SourceType = SourceType.News,
            Status = ItemStatus.Open,
            DatePosted = DefaultDatePosted
        };

    /// <summary>A fixed posting date, in UTC as the timestamp columns require.</summary>
    public static readonly DateTime DefaultDatePosted = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static ExhibitEntity Exhibit(Guid collectionId, string name = "Test Exhibit", int move = 0, int inject = 0) =>
        new() { Id = Guid.NewGuid(), CollectionId = collectionId, Name = name, CurrentMove = move, CurrentInject = inject };

    public static TeamEntity Team(Guid exhibitId, string name = "Test Team", string email = null) =>
        new() { Id = Guid.NewGuid(), ExhibitId = exhibitId, Name = name, ShortName = name, Email = email };

    public static TeamUserEntity TeamUser(Guid teamId, Guid userId, bool observer = false) =>
        new() { Id = Guid.NewGuid(), TeamId = teamId, UserId = userId, IsObserver = observer };

    public static TeamCardEntity TeamCard(Guid teamId, Guid cardId, bool canPostArticles = false) =>
        new(teamId, cardId) { Id = Guid.NewGuid(), CanPostArticles = canPostArticles };

    public static TeamArticleEntity TeamArticle(Guid exhibitId, Guid teamId, Guid articleId) =>
        new(exhibitId, teamId, articleId) { Id = Guid.NewGuid() };

    public static UserArticleEntity UserArticle(Guid exhibitId, Guid userId, Guid articleId, bool isRead = false) =>
        new()
        {
            Id = Guid.NewGuid(),
            ExhibitId = exhibitId,
            UserId = userId,
            ArticleId = articleId,
            IsRead = isRead,
            ActualDatePosted = DefaultDatePosted
        };

    public static ExhibitTeamEntity ExhibitTeam(Guid exhibitId, Guid teamId, Guid? id = null) =>
        new(exhibitId, teamId) { Id = id ?? Guid.NewGuid() };

    public static CollectionMembershipEntity CollectionMembership(Guid collectionId, Guid userId, Guid? roleId = null) =>
        new(collectionId, userId, null) { Id = Guid.NewGuid(), RoleId = roleId ?? MembershipRoles.Member };

    public static ExhibitMembershipEntity ExhibitMembership(Guid exhibitId, Guid userId, Guid? roleId = null) =>
        new(exhibitId, userId, null) { Id = Guid.NewGuid(), RoleId = roleId ?? MembershipRoles.Member };

    public static GroupEntity Group(string name = "Test Group") =>
        new() { Id = Guid.NewGuid(), Name = name, Description = "Seeded by TestData.Group" };

    public static GroupMembershipEntity GroupMembership(Guid groupId, Guid userId) =>
        new(groupId, userId) { Id = Guid.NewGuid() };
}
