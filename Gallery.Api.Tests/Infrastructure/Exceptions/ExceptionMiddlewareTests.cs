// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Net;
using System.Net.Http.Json;
using System.Text;
using Gallery.Api.Data.Enumerations;
using Gallery.Api.Tests.Support;
using Gallery.Api.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Gallery.Api.Tests.Infrastructure.Exceptions;

/// <summary>
/// The shapes <c>ExceptionMiddleware</c> and the MVC filters answer with, which clients parse: an
/// <c>application/problem+json</c> body whose title names the failure. The host runs in Production, so a
/// 500's title is generic and its detail the exception message.
/// </summary>
public class ExceptionMiddlewareTests(DatabaseFixture fixture, GalleryAppFactory factory) : ApiTestBase(fixture, factory)
{
    [Fact]
    public async Task A_forbidden_exception_is_a_403_titled_insufficient_permissions()
    {
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewGroups).SeedAsync();

        var problem = await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).GetAsync("api/system-roles", Ct));

        Assert.Equal(("Insufficient Permissions", 403), (problem.Title, problem.Status.Value));
    }

    [Fact]
    public async Task A_not_found_exception_is_a_404_titled_with_the_spaced_type_name()
    {
        var problem = await AssertProblem(HttpStatusCode.NotFound, await RootClient.GetAsync($"api/collection-roles/{Guid.NewGuid()}", Ct));

        Assert.Equal("Collection Role not found", problem.Title);
    }

    [Fact]
    public async Task A_foreign_key_violation_is_a_400_with_a_generic_title()
    {
        var problem = await AssertProblem(HttpStatusCode.BadRequest,
            await RootClient.PostAsJsonAsync("api/cards", new { name = "Orphan Card", collectionId = Guid.NewGuid() }, Ct));

        Assert.Equal("Referenced entity does not exist. Please verify all referenced entities exist.", problem.Title);
    }

    [Fact]
    public async Task An_unhandled_exception_is_a_500_with_a_generic_title_and_the_message_as_detail()
    {
        // Same case as UnknownIdTests.An_unknown_id_is_answered_with_a_server_error.
        var problem = await AssertProblem(HttpStatusCode.InternalServerError, await RootClient.DeleteAsync($"api/teams/{Guid.NewGuid()}", Ct));

        Assert.Equal(("A server error occurred.", "Object reference not set to an instance of an object."), (problem.Title, problem.Detail));
    }

    [Fact]
    public async Task A_body_that_is_not_json_is_a_400()
    {
        var response = await RootClient.PostAsync("api/collections", new StringContent("{ not json", Encoding.UTF8, "application/json"), Ct);

        await AssertProblem(HttpStatusCode.BadRequest, response);
    }

    [Fact]
    public async Task A_missing_body_is_a_400()
    {
        var response = await RootClient.PostAsync("api/collections", new StringContent("", Encoding.UTF8, "application/json"), Ct);

        await AssertProblem(HttpStatusCode.BadRequest, response);
    }

    [Fact]
    public async Task A_route_id_that_is_not_a_guid_is_a_400()
    {
        await AssertProblem(HttpStatusCode.BadRequest, await RootClient.GetAsync("api/collections/not-a-guid", Ct));
    }

    /// <summary>An enum value outside the defined ones is accepted and stored.</summary>
    [Fact]
    public async Task An_out_of_range_source_type_is_stored()
    {
        var collection = TestData.Collection();
        await Seed(collection);

        var created = await ReadAsync<Article>(await RootClient.PostAsJsonAsync("api/articles",
            new { name = "Odd", collectionId = collection.Id, sourceType = 999, status = 50, datePosted = TestData.DefaultDatePosted }, Ct));

        await using var db = NewContext();
        Assert.Equal(999, (int)(await db.Articles.SingleAsync(x => x.Id == created.Id, Ct)).SourceType);
    }
}
