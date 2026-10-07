// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Net;
using System.Net.Http.Json;
using Gallery.Api.Hubs;
using Gallery.Api.Tests.Support;
using Gallery.Api.ViewModels;

namespace Gallery.Api.Tests.Infrastructure.EventHandlers;

/// <summary>The collection handlers, reached through a request.</summary>
public class CollectionHandlerTests(DatabaseFixture fixture, GalleryAppFactory factory) : ApiTestBase(fixture, factory)
{
    [Fact]
    public async Task Creating_a_collection_sends_CollectionCreated_to_its_group_and_the_collection_admin_group()
    {
        var created = await ReadAsync<Collection>(await RootClient.PostAsJsonAsync("api/collections", new { name = "Announced" }, Ct));

        var hub = Factory.Hub<MainHub>();
        var broadcast = Assert.Single(hub.ToGroup(created.Id));
        Assert.Equal(MainHubMethods.CollectionCreated, broadcast.Method);
        Assert.Null(broadcast.Arguments[1]);
        Assert.Contains(hub.ToGroup(MainHub.COLLECTION_GROUP), x => x.Arguments[0] is Collection c && c.Id == created.Id);
    }

    [Fact]
    public async Task Updating_a_collection_sends_CollectionUpdated_with_the_modified_properties()
    {
        var collection = TestData.Collection();
        await Seed(collection);

        await AssertStatus(HttpStatusCode.OK, await RootClient.PutAsJsonAsync($"api/collections/{collection.Id}", new { id = collection.Id, name = "Renamed" }, Ct));

        var broadcast = Assert.Single(Factory.Hub<MainHub>().ToGroup(collection.Id));
        Assert.Equal(MainHubMethods.CollectionUpdated, broadcast.Method);
        Assert.Equal("Renamed", Assert.IsType<Collection>(broadcast.Arguments[0]).Name);
        Assert.Contains("name", Assert.IsType<string[]>(broadcast.Arguments[1]));
    }

    [Fact]
    public async Task Deleting_a_collection_sends_CollectionDeleted_with_the_bare_id()
    {
        var collection = TestData.Collection();
        await Seed(collection);

        await AssertStatus(HttpStatusCode.NoContent, await RootClient.DeleteAsync($"api/collections/{collection.Id}", Ct));

        var broadcast = Assert.Single(Factory.Hub<MainHub>().ToGroup(collection.Id));
        Assert.Equal((MainHubMethods.CollectionDeleted, (object)collection.Id), (broadcast.Method, broadcast.Argument));
    }
}
