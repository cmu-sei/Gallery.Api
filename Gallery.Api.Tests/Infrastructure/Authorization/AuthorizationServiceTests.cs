// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Gallery.Api.Data.Enumerations;
using Gallery.Api.Tests.Support;
using Gallery.Api.ViewModels;

namespace Gallery.Api.Tests.Infrastructure.Authorization;

/// <summary>
/// <c>AuthorizationService</c> (the app's <c>IGalleryAuthorizationService</c>) over the real handlers: how a
/// system permission, a resource permission and the resource type combine.
/// </summary>
public class AuthorizationServiceTests(DatabaseFixture fixture) : DatabaseTestBase(fixture)
{
    [Fact]
    public async Task A_collection_gate_passes_on_the_collection_permission_of_an_exhibit_s_collection()
    {
        var collection = TestData.Collection();
        var exhibit = TestData.Exhibit(collection.Id);
        await Seed(collection, exhibit);
        var user = new ClaimsPrincipalBuilder().WithCollection(collection.Id, CollectionPermission.ViewCollection).Build();

        var allowed = await AuthorizationHarness.CreateGalleryAuthorizationService(user, Db)
            .AuthorizeAsync<Exhibit>(exhibit.Id, [SystemPermission.ViewCollections], [CollectionPermission.ViewCollection], Ct);

        Assert.True(allowed);
    }

    [Fact]
    public async Task An_exhibit_gate_fails_on_the_permission_for_another_exhibit()
    {
        var user = new ClaimsPrincipalBuilder().WithExhibit(Guid.NewGuid(), ExhibitPermission.ViewExhibit).Build();

        var allowed = await AuthorizationHarness.CreateGalleryAuthorizationService(user, Db)
            .AuthorizeAsync<Exhibit>(Guid.NewGuid(), [SystemPermission.ViewExhibits], [ExhibitPermission.ViewExhibit], Ct);

        Assert.False(allowed);
    }

    [Fact]
    public async Task An_exhibit_gate_passes_on_the_system_permission_without_resolving_the_type()
    {
        var user = new ClaimsPrincipalBuilder().WithSystemPermissions(SystemPermission.ViewExhibits).Build();

        var allowed = await AuthorizationHarness.CreateGalleryAuthorizationService(user, Db)
            .AuthorizeAsync<Collection>(Guid.NewGuid(), [SystemPermission.ViewExhibits], [ExhibitPermission.ViewExhibit], Ct);

        Assert.True(allowed);
    }

    /// <summary>An exhibit gate asked about a collection throws for a caller the system permission does not pass.</summary>
    [Fact]
    public async Task An_exhibit_gate_typed_for_a_collection_throws_without_the_system_permission()
    {
        // Same case as ArticleControllerTests.GetByExhibit_answers_a_member_holding_ViewExhibit_with_a_server_error.
        var user = new ClaimsPrincipalBuilder().WithExhibit(Guid.NewGuid(), ExhibitPermission.ViewExhibit).Build();
        var service = AuthorizationHarness.CreateGalleryAuthorizationService(user, Db);

        await Assert.ThrowsAsync<NotImplementedException>(() => service.AuthorizeAsync<Collection>(
            Guid.NewGuid(), [SystemPermission.ViewExhibits], [ExhibitPermission.ViewExhibit], Ct));
    }
}
