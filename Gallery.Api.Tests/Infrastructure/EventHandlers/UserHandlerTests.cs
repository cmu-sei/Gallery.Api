// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Net;
using System.Net.Http.Json;
using Gallery.Api.Hubs;
using Gallery.Api.Tests.Support;
using Gallery.Api.ViewModels;

namespace Gallery.Api.Tests.Infrastructure.EventHandlers;

/// <summary>The user handlers, reached through a request.</summary>
public class UserHandlerTests(DatabaseFixture fixture, GalleryAppFactory factory) : ApiTestBase(fixture, factory)
{
    /// <summary>A user created through the API is announced to the group named by its CreatedBy, the empty id, only.</summary>
    [Fact]
    public async Task Creating_a_user_sends_UserCreated_to_the_empty_id_group_only()
    {
        var id = Guid.NewGuid();

        await AssertStatus(HttpStatusCode.Created, await RootClient.PostAsJsonAsync("api/users", new { id, name = "Announced" }, Ct));

        var hub = Factory.Hub<MainHub>();
        var broadcast = Assert.Single(hub.ToGroup(Guid.Empty), x => x.Arguments[0] is User u && u.Id == id);
        Assert.Equal(MainHubMethods.UserCreated, broadcast.Method);
        Assert.DoesNotContain(hub.ToGroup(MainHub.USER_GROUP), x => x.Arguments[0] is User u && u.Id == id);
    }
}
