// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Net;
using System.Net.Http.Json;
using Gallery.Api.Data.Enumerations;
using Gallery.Api.Hubs;
using Gallery.Api.Tests.Support;
using Gallery.Api.ViewModels;

namespace Gallery.Api.Tests.Infrastructure.EventHandlers;

/// <summary>The user handlers, reached through a request.</summary>
public class UserHandlerTests(DatabaseFixture fixture, GalleryAppFactory factory) : ApiTestBase(fixture, factory)
{
    /// <summary>A user created through the API is announced to the group named by its CreatedBy, the caller, only.</summary>
    [Fact]
    public async Task Creating_a_user_sends_UserCreated_to_the_creator_s_group_only()
    {
        var id = Guid.NewGuid();
        var actor = await Actor().WithSystemPermissions(SystemPermission.ManageUsers).SeedAsync();

        await AssertStatus(HttpStatusCode.Created, await Client(actor).PostAsJsonAsync("api/users", new { id, name = "Announced" }, Ct));

        var hub = Factory.Hub<MainHub>();
        Assert.DoesNotContain(hub.ToGroup(Guid.Empty), x => x.Arguments[0] is User u && u.Id == id);
        var broadcast = Assert.Single(hub.ToGroup(actor.Id), x => x.Arguments[0] is User u && u.Id == id);
        Assert.Equal(MainHubMethods.UserCreated, broadcast.Method);
        Assert.DoesNotContain(hub.ToGroup(MainHub.USER_GROUP), x => x.Arguments[0] is User u && u.Id == id);
    }
}
