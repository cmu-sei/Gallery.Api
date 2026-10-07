// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Net;
using System.Net.Http.Json;
using Gallery.Api.Data.Enumerations;
using Gallery.Api.Data.Models;
using Gallery.Api.Tests.Support;
using Gallery.Api.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Gallery.Api.Tests.Controllers;

/// <summary>
/// <c>CollectionMembershipsController</c> over HTTP: reads need ViewCollections or ViewCollection, writes
/// ManageCollections or ManageCollection, on the membership's collection.
/// </summary>
public class CollectionMembershipControllerTests(DatabaseFixture fixture, GalleryAppFactory factory) : ApiTestBase(fixture, factory)
{
    [Fact]
    public async Task GetAll_returns_the_memberships_to_a_member_holding_ViewCollection()
    {
        var (collection, membership) = await SeedMembership();
        var actor = await Actor().OnCollection(collection, [CollectionPermission.ViewCollection]).SeedAsync();

        var memberships = await ReadAsync<List<CollectionMembership>>(
            await Client(actor).GetAsync($"api/collections/{collection.Id}/memberships", Ct));

        Assert.Contains(memberships, x => x.Id == membership.Id);
    }

    [Fact]
    public async Task GetAll_is_forbidden_for_a_caller_holding_ViewCollection_only_on_another_collection()
    {
        var (collection, _) = await SeedMembership();
        var actor = await Actor().OnNewCollection(CollectionPermission.ViewCollection).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/collections/{collection.Id}/memberships", Ct));
    }

    /// <summary>A request with no identity is answered with a 401; the anonymous client is the case under test.</summary>
    [Fact]
    public async Task GetAll_without_an_identity_is_unauthorized()
    {
        await AssertStatus(HttpStatusCode.Unauthorized, await Client().GetAsync($"api/collections/{Guid.NewGuid()}/memberships", Ct));
    }

    [Fact]
    public async Task Get_returns_the_membership_to_a_caller_holding_ViewCollections()
    {
        var (_, membership) = await SeedMembership();
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewCollections).SeedAsync();

        var got = await ReadAsync<CollectionMembership>(await Client(actor).GetAsync($"api/collections/memberships/{membership.Id}", Ct));

        Assert.Equal(membership.UserId, got.UserId);
    }

    [Fact]
    public async Task Get_returns_the_membership_to_a_member_holding_ViewCollection()
    {
        var (collection, membership) = await SeedMembership();
        var actor = await Actor().OnCollection(collection, [CollectionPermission.ViewCollection]).SeedAsync();

        var got = await ReadAsync<CollectionMembership>(await Client(actor).GetAsync($"api/collections/memberships/{membership.Id}", Ct));

        Assert.Equal(membership.Id, got.Id);
    }

    [Fact]
    public async Task Get_is_forbidden_for_a_caller_holding_ViewCollection_only_on_another_collection()
    {
        var (_, membership) = await SeedMembership();
        var actor = await Actor().OnNewCollection(CollectionPermission.ViewCollection).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/collections/memberships/{membership.Id}", Ct));
    }

    [Fact]
    public async Task Get_is_forbidden_for_a_caller_holding_only_ViewExhibits()
    {
        var (_, membership) = await SeedMembership();
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewExhibits).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/collections/memberships/{membership.Id}", Ct));
    }

    [Fact]
    public async Task Get_reports_an_unknown_membership_as_not_found()
    {
        await AssertProblem(HttpStatusCode.NotFound, await RootClient.GetAsync($"api/collections/memberships/{Guid.NewGuid()}", Ct));
    }

    [Fact]
    public async Task Create_persists_the_membership_for_a_member_holding_ManageCollection()
    {
        var collection = TestData.Collection();
        var user = TestData.User();
        await Seed(collection, user);
        var actor = await Actor().OnCollection(collection, [CollectionPermission.ManageCollection]).SeedAsync();

        await AssertStatus(HttpStatusCode.Created, await Client(actor).PostAsJsonAsync($"api/collections/{collection.Id}/memberships",
            new { collectionId = collection.Id, userId = user.Id, roleId = TestData.MembershipRoles.Observer }, Ct));

        await using var db = NewContext();
        var stored = await db.CollectionMemberships.SingleAsync(x => x.UserId == user.Id, Ct);
        Assert.Equal((collection.Id, TestData.MembershipRoles.Observer), (stored.CollectionId, stored.RoleId));
    }

    [Fact]
    public async Task Create_is_forbidden_for_a_caller_holding_only_EditCollection()
    {
        var collection = TestData.Collection();
        var user = TestData.User();
        await Seed(collection, user);
        var actor = await Actor().OnCollection(collection, [CollectionPermission.EditCollection]).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).PostAsJsonAsync($"api/collections/{collection.Id}/memberships",
            new { collectionId = collection.Id, userId = user.Id }, Ct));
    }

    [Fact]
    public async Task Create_is_forbidden_for_a_caller_holding_ManageCollection_only_on_another_collection()
    {
        var collection = TestData.Collection();
        var user = TestData.User();
        await Seed(collection, user);
        var actor = await Actor().OnNewCollection(CollectionPermission.ManageCollection).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).PostAsJsonAsync($"api/collections/{collection.Id}/memberships",
            new { collectionId = collection.Id, userId = user.Id }, Ct));
    }

    /// <summary>A body whose collection differs from the route's is answered with a 500.</summary>
    [Fact]
    public async Task Create_answers_a_body_naming_another_collection_with_a_server_error()
    {
        var collection = TestData.Collection();
        var other = TestData.Collection("Other");
        var user = TestData.User();
        await Seed(collection, other, user);

        var problem = await AssertProblem(HttpStatusCode.InternalServerError, await RootClient.PostAsJsonAsync(
            $"api/collections/{collection.Id}/memberships", new { collectionId = other.Id, userId = user.Id }, Ct));

        Assert.Equal("The CollectionId of the membership must match the CollectionId of the URL.", problem.Detail);
    }

    [Fact]
    public async Task Update_changes_the_role_for_a_member_holding_ManageCollection()
    {
        var (collection, membership) = await SeedMembership();
        var actor = await Actor().OnCollection(collection, [CollectionPermission.ManageCollection]).SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).PutAsJsonAsync($"api/collections/memberships/{membership.Id}",
            new { id = membership.Id, collectionId = collection.Id, userId = membership.UserId, roleId = TestData.MembershipRoles.Manager }, Ct));

        await using var db = NewContext();
        Assert.Equal(TestData.MembershipRoles.Manager, (await db.CollectionMemberships.SingleAsync(x => x.Id == membership.Id, Ct)).RoleId);
    }

    [Fact]
    public async Task Update_is_forbidden_for_a_caller_holding_ManageCollection_only_on_another_collection()
    {
        var (collection, membership) = await SeedMembership();
        var actor = await Actor().OnNewCollection(CollectionPermission.ManageCollection).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).PutAsJsonAsync($"api/collections/memberships/{membership.Id}",
            new { id = membership.Id, collectionId = collection.Id, userId = membership.UserId, roleId = TestData.MembershipRoles.Manager }, Ct));

        await using var db = NewContext();
        Assert.Equal(TestData.MembershipRoles.Member, (await db.CollectionMemberships.SingleAsync(x => x.Id == membership.Id, Ct)).RoleId);
    }

    /// <summary>A caller holding only ManageCollection gives a member the Manager role, which holds every collection permission.</summary>
    [Fact]
    public async Task Update_lets_a_caller_holding_only_ManageCollection_grant_the_Manager_role()
    {
        var (collection, membership) = await SeedMembership();
        var actor = await Actor().OnCollection(collection, [CollectionPermission.ManageCollection]).SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).PutAsJsonAsync($"api/collections/memberships/{membership.Id}",
            new { id = membership.Id, collectionId = collection.Id, userId = membership.UserId, roleId = TestData.MembershipRoles.Manager }, Ct));

        await using var db = NewContext();
        Assert.Equal(TestData.MembershipRoles.Manager, (await db.CollectionMemberships.SingleAsync(x => x.Id == membership.Id, Ct)).RoleId);
    }

    [Fact]
    public async Task Delete_removes_the_membership_for_a_caller_holding_ManageCollections()
    {
        var (_, membership) = await SeedMembership();
        var actor = await Actor().WithSystemPermissions(SystemPermission.ManageCollections).SeedAsync();

        await AssertStatus(HttpStatusCode.NoContent, await Client(actor).DeleteAsync($"api/collections/memberships/{membership.Id}", Ct));

        await using var db = NewContext();
        Assert.False(await db.CollectionMemberships.AnyAsync(x => x.Id == membership.Id, Ct));
    }

    [Fact]
    public async Task Delete_is_forbidden_for_a_caller_holding_only_EditCollections()
    {
        var (_, membership) = await SeedMembership();
        var actor = await Actor().WithSystemPermissions(SystemPermission.EditCollections).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).DeleteAsync($"api/collections/memberships/{membership.Id}", Ct));
    }

    [Fact]
    public async Task Delete_reports_an_unknown_membership_as_not_found()
    {
        await AssertProblem(HttpStatusCode.NotFound, await RootClient.DeleteAsync($"api/collections/memberships/{Guid.NewGuid()}", Ct));
    }

    private async Task<(CollectionEntity Collection, CollectionMembershipEntity Membership)> SeedMembership()
    {
        var collection = TestData.Collection();
        var user = TestData.User(name: "Member");
        var membership = TestData.CollectionMembership(collection.Id, user.Id);
        await Seed(collection, user, membership);

        return (collection, membership);
    }
}
