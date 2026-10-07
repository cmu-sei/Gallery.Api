// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Net;
using System.Net.Http.Json;
using Gallery.Api.Hubs;
using Gallery.Api.Tests.Support;
using Gallery.Api.ViewModels;

namespace Gallery.Api.Tests.Infrastructure.EventHandlers;

/// <summary>
/// The membership handlers, reached through a request. They address their admin group with
/// <c>Clients.Groups(name)</c> and send the membership as a single argument; each test reads the admin
/// group's sends for a membership id it created.
/// </summary>
public class MembershipHandlerTests(DatabaseFixture fixture, GalleryAppFactory factory) : ApiTestBase(fixture, factory)
{
    [Fact]
    public async Task A_new_collection_membership_is_sent_alone_to_the_collection_admin_group()
    {
        var collection = TestData.Collection();
        var user = TestData.User();
        await Seed(collection, user);

        var created = await ReadAsync<CollectionMembership>(await RootClient.PostAsJsonAsync(
            $"api/collections/{collection.Id}/memberships", new { collectionId = collection.Id, userId = user.Id }, Ct));

        var broadcast = Assert.Single(Factory.Hub<MainHub>().ToGroups(MainHub.COLLECTION_GROUP),
            x => x.Arguments[0] is CollectionMembership m && m.Id == created.Id);
        Assert.Equal((MainHubMethods.CollectionMembershipCreated, 1), (broadcast.Method, broadcast.Arguments.Length));
    }

    [Fact]
    public async Task A_deleted_exhibit_membership_is_sent_as_its_bare_id_to_the_exhibit_admin_group()
    {
        var collection = TestData.Collection();
        var exhibit = TestData.Exhibit(collection.Id);
        var user = TestData.User();
        var membership = TestData.ExhibitMembership(exhibit.Id, user.Id);
        await Seed(collection, exhibit, user, membership);

        await AssertStatus(HttpStatusCode.NoContent, await RootClient.DeleteAsync($"api/exhibits/memberships/{membership.Id}", Ct));

        var broadcast = Assert.Single(Factory.Hub<MainHub>().ToGroups(MainHub.EXHIBIT_GROUP),
            x => x.Arguments.Length == 1 && Equals(x.Argument, membership.Id));
        Assert.Equal(MainHubMethods.ExhibitMembershipDeleted, broadcast.Method);
    }

    [Fact]
    public async Task A_new_group_membership_is_sent_alone_to_the_group_admin_group()
    {
        var group = TestData.Group();
        var user = TestData.User();
        await Seed(group, user);

        var created = await ReadAsync<GroupMembership>(await RootClient.PostAsJsonAsync(
            $"api/groups/{group.Id}/memberships", new { groupId = group.Id, userId = user.Id }, Ct));

        var broadcast = Assert.Single(Factory.Hub<MainHub>().ToGroups(MainHub.GROUP_GROUP),
            x => x.Arguments[0] is GroupMembership m && m.Id == created.Id);
        Assert.Equal((MainHubMethods.GroupMembershipCreated, 1), (broadcast.Method, broadcast.Arguments.Length));
    }
}
