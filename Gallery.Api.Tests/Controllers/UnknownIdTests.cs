// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Net;
using System.Text;
using Gallery.Api.Tests.Support;

namespace Gallery.Api.Tests.Controllers;

/// <summary>
/// What the routes answer when the id in the route (or the team a body names) names no row, for a caller
/// holding every system permission, so that no authorization check answers first.
/// </summary>
public class UnknownIdTests(DatabaseFixture fixture, GalleryAppFactory factory) : ApiTestBase(fixture, factory)
{
    /// <summary>An id no test seeds: every test database starts from the migrated template, which holds no such row.</summary>
    private const string Unknown = "6f0c9a64-1d2e-4c3b-9a8f-00000000dead";

    /// <summary>The detail of a 500 from a null dereference.</summary>
    private const string NullReference = "Object reference not set to an instance of an object.";

    /// <summary>The detail of a 500 from <c>FirstAsync</c> over no rows.</summary>
    private const string NoElements = "Sequence contains no elements.";

    public static TheoryData<string, string, string, string> ServerErrorRoutes => new()
    {
        { "DELETE", $"api/articles/{Unknown}", null, NullReference },
        { "DELETE", $"api/teams/{Unknown}", null, NullReference },
        { "GET", $"api/teamusers/{Unknown}", null, NullReference },
        { "POST", "api/teamusers", $$"""{ "teamId": "{{Unknown}}", "userId": "{{Unknown}}" }""", NullReference },
        { "PUT", $"api/teamusers/{Unknown}/observer/set", null, NullReference },
        { "PUT", $"api/teamusers/{Unknown}/observer/clear", null, NullReference },
        { "DELETE", $"api/teamusers/{Unknown}", null, NullReference },
        { "DELETE", $"api/teams/{Unknown}/users/{Unknown}", null, NullReference },
        { "POST", "api/teamcards", $$"""{ "teamId": "{{Unknown}}", "cardId": "{{Unknown}}" }""", NullReference },
        { "PUT", $"api/teamcards/{Unknown}", $$"""{ "id": "{{Unknown}}", "teamId": "{{Unknown}}", "cardId": "{{Unknown}}" }""", NullReference },
        { "DELETE", $"api/teamcards/{Unknown}", null, NullReference },
        { "DELETE", $"api/teams/{Unknown}/cards/{Unknown}", null, NullReference },
        { "DELETE", $"api/teams/{Unknown}/articles/{Unknown}", null, NullReference },
        { "DELETE", $"api/exhibitteams/{Unknown}", null, NullReference },
        { "PUT", $"api/userarticles/{Unknown}/isread", "true", NoElements },
        { "PUT", $"api/userarticles/{Unknown}/share", "{}", NoElements },
        { "DELETE", $"api/userarticles/{Unknown}", null, NoElements },
    };

    public static TheoryData<string> NoContentRoutes => new()
    {
        $"api/system-roles/{Unknown}",
        $"api/groups/{Unknown}",
        $"api/groups/memberships/{Unknown}",
    };

    /// <summary>An id that names no row is answered with a 500 on these routes.</summary>
    [Theory]
    [MemberData(nameof(ServerErrorRoutes))]
    public async Task An_unknown_id_is_answered_with_a_server_error(string method, string route, string body, string detail)
    {
        var response = await RootClient.SendAsync(Request(method, route, body), Ct);

        Assert.Equal(detail, (await AssertProblem(HttpStatusCode.InternalServerError, response)).Detail);
    }

    /// <summary>An id that names no row is answered with 204 and no body on these routes.</summary>
    [Theory]
    [MemberData(nameof(NoContentRoutes))]
    public async Task An_unknown_id_is_answered_with_no_content(string route)
    {
        var response = await RootClient.GetAsync(route, Ct);

        await AssertStatus(HttpStatusCode.NoContent, response);
    }

    private static HttpRequestMessage Request(string method, string route, string body) => new(new HttpMethod(method), route)
    {
        Content = body is null ? null : new StringContent(body, Encoding.UTF8, "application/json")
    };
}
