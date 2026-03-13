// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using AutoFixture;
using Gallery.Api.Data.Enumerations;
using Gallery.Api.Data.Models;

namespace Gallery.Api.Tests.Shared.Fixtures;

public class GalleryCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        fixture.Customize<CollectionEntity>(c => c
            .With(x => x.Id, Guid.NewGuid())
            .Without(x => x.Memberships));

        fixture.Customize<ExhibitEntity>(c => c
            .With(x => x.Id, Guid.NewGuid())
            .With(x => x.CurrentMove, 0)
            .With(x => x.CurrentInject, 0)
            .Without(x => x.Collection)
            .Without(x => x.Teams)
            .Without(x => x.Memberships));

        fixture.Customize<CardEntity>(c => c
            .With(x => x.Id, Guid.NewGuid())
            .With(x => x.Move, 0)
            .With(x => x.Inject, 0)
            .Without(x => x.Collection));

        fixture.Customize<ArticleEntity>(c => c
            .With(x => x.Id, Guid.NewGuid())
            .With(x => x.Move, 0)
            .With(x => x.Inject, 0)
            .With(x => x.Status, ItemStatus.Open)
            .With(x => x.SourceType, SourceType.News)
            .With(x => x.DatePosted, DateTime.UtcNow)
            .Without(x => x.Collection)
            .Without(x => x.Exhibit)
            .Without(x => x.Card)
            .Without(x => x.TeamArticles));

        fixture.Customize<UserEntity>(c => c
            .With(x => x.Id, Guid.NewGuid())
            .Without(x => x.TeamUsers)
            .Without(x => x.Role)
            .Without(x => x.ExhibitMemberships)
            .Without(x => x.CollectionMemberships)
            .Without(x => x.GroupMemberships));

        fixture.Customize<TeamEntity>(c => c
            .With(x => x.Id, Guid.NewGuid())
            .Without(x => x.Exhibit)
            .Without(x => x.TeamUsers)
            .Without(x => x.TeamArticles));

        fixture.Customize<TeamUserEntity>(c => c
            .With(x => x.Id, Guid.NewGuid())
            .With(x => x.IsObserver, false)
            .Without(x => x.User)
            .Without(x => x.Team));

        fixture.Customize<TeamArticleEntity>(c => c
            .With(x => x.Id, Guid.NewGuid())
            .Without(x => x.Exhibit)
            .Without(x => x.Team)
            .Without(x => x.Article));

        fixture.Customize<TeamCardEntity>(c => c
            .With(x => x.Id, Guid.NewGuid())
            .With(x => x.Move, 0)
            .With(x => x.Inject, 0)
            .With(x => x.IsShownOnWall, true)
            .With(x => x.CanPostArticles, false)
            .Without(x => x.Team)
            .Without(x => x.Card));

        fixture.Customize<UserArticleEntity>(c => c
            .With(x => x.Id, Guid.NewGuid())
            .With(x => x.IsRead, false)
            .With(x => x.ActualDatePosted, DateTime.UtcNow)
            .Without(x => x.Exhibit)
            .Without(x => x.User)
            .Without(x => x.Article));

        fixture.Customize<ExhibitTeamEntity>(c => c
            .With(x => x.Id, Guid.NewGuid())
            .Without(x => x.Exhibit)
            .Without(x => x.Team));

        fixture.Customize<PermissionEntity>(c => c
            .With(x => x.Id, Guid.NewGuid())
            .With(x => x.ReadOnly, false)
            .Without(x => x.UserPermissions));

        fixture.Customize<UserPermissionEntity>(c => c
            .With(x => x.Id, Guid.NewGuid())
            .Without(x => x.User)
            .Without(x => x.Permission));

        fixture.Customize<GroupEntity>(c => c
            .With(x => x.Id, Guid.NewGuid())
            .Without(x => x.Memberships)
            .Without(x => x.CollectionMemberships)
            .Without(x => x.ExhibitMemberships));

        fixture.Customize<GroupMembershipEntity>(c => c
            .With(x => x.Id, Guid.NewGuid())
            .Without(x => x.Group)
            .Without(x => x.User));

        fixture.Customize<CollectionMembershipEntity>(c => c
            .With(x => x.Id, Guid.NewGuid())
            .Without(x => x.Collection)
            .Without(x => x.User)
            .Without(x => x.Group)
            .Without(x => x.Role));

        fixture.Customize<ExhibitMembershipEntity>(c => c
            .With(x => x.Id, Guid.NewGuid())
            .Without(x => x.Exhibit)
            .Without(x => x.User)
            .Without(x => x.Group)
            .Without(x => x.Role));

        fixture.Customize<SystemRoleEntity>(c => c
            .With(x => x.Id, Guid.NewGuid())
            .With(x => x.AllPermissions, false)
            .With(x => x.Immutable, false)
            .With(x => x.Permissions, new List<SystemPermission>()));

        fixture.Customize<CollectionRoleEntity>(c => c
            .With(x => x.Id, Guid.NewGuid())
            .With(x => x.AllPermissions, false)
            .With(x => x.Permissions, new List<CollectionPermission>()));

        fixture.Customize<ExhibitRoleEntity>(c => c
            .With(x => x.Id, Guid.NewGuid())
            .With(x => x.AllPermissions, false)
            .With(x => x.Permissions, new List<ExhibitPermission>()));

        fixture.Customize<XApiQueuedStatementEntity>(c => c
            .With(x => x.Id, Guid.NewGuid())
            .With(x => x.Status, XApiQueueStatus.Pending)
            .With(x => x.QueuedAt, DateTime.UtcNow)
            .With(x => x.RetryCount, 0));
    }
}
