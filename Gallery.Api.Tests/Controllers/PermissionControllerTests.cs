// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Net;
using Gallery.Api.Data.Enumerations;
using Gallery.Api.Infrastructure.Authorization;
using Gallery.Api.Tests.Support;

namespace Gallery.Api.Tests.Controllers;

/// <summary>
/// The three "my permissions" endpoints (<c>SystemPermissionsController</c>,
/// <c>CollectionPermissionsController</c>, <c>ExhibitPermissionsController</c>): any authenticated caller
/// reads back what the claims transformer derived for them.
/// </summary>
public class PermissionControllerTests(DatabaseFixture fixture, GalleryAppFactory factory) : ApiTestBase(fixture, factory)
{
    [Fact]
    public async Task GetMySystemPermissions_returns_exactly_the_caller_s_system_permissions()
    {
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewGroups, SystemPermission.ViewRoles).SeedAsync();

        var permissions = await ReadAsync<SystemPermission[]>(await Client(actor).GetAsync("api/me/systemPermissions", Ct));

        Assert.Equal([SystemPermission.ViewRoles, SystemPermission.ViewGroups], permissions.Order());
    }

    /// <summary>A request with no identity is answered with a 401; the anonymous client is the case under test.</summary>
    [Fact]
    public async Task GetMySystemPermissions_without_an_identity_is_unauthorized()
    {
        await AssertStatus(HttpStatusCode.Unauthorized, await Client().GetAsync("api/me/systemPermissions", Ct));
    }

    [Fact]
    public async Task GetMyCollectionPermissions_returns_the_caller_s_collection_claims()
    {
        var actor = await Actor().OnNewCollection(CollectionPermission.EditCollection).SeedAsync();

        var claims = await ReadAsync<List<CollectionPermissionClaim>>(await Client(actor).GetAsync("api/collection-permissions", Ct));

        var claim = Assert.Single(claims);
        Assert.Equal(Assert.Single(actor.NewCollectionIds), claim.CollectionId);
        Assert.Equal([CollectionPermission.EditCollection], claim.Permissions);
    }

    [Fact]
    public async Task GetMyExhibitPermissions_returns_the_caller_s_exhibit_claims()
    {
        var collection = TestData.Collection();
        await Seed(collection);
        var actor = await Actor().OnNewExhibit(collection.Id, ExhibitPermission.ManageExhibit).SeedAsync();

        var claims = await ReadAsync<List<ExhibitPermissionClaim>>(await Client(actor).GetAsync("api/exhibit-permissions", Ct));

        var claim = Assert.Single(claims);
        Assert.Equal(Assert.Single(actor.NewExhibitIds), claim.ExhibitId);
        Assert.Equal([ExhibitPermission.ManageExhibit], claim.Permissions);
    }
}
