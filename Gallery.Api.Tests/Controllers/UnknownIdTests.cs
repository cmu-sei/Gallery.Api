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

    public static TheoryData<string, string, string> NotFoundRoutes => new()
    {
        { "GET", $"api/teamusers/{Unknown}", null },
        { "PUT", $"api/teamusers/{Unknown}/observer/set", null },
        { "PUT", $"api/teamusers/{Unknown}/observer/clear", null },
        { "DELETE", $"api/teamusers/{Unknown}", null },
        { "DELETE", $"api/exhibitteams/{Unknown}", null },
        { "PUT", $"api/userarticles/{Unknown}/isread", "true" },
        { "PUT", $"api/userarticles/{Unknown}/share", "{}" },
        { "DELETE", $"api/userarticles/{Unknown}", null },
        { "DELETE", $"api/teamcards/{Unknown}", null },
        { "DELETE", $"api/articles/{Unknown}", null },
        { "DELETE", $"api/teams/{Unknown}", null },
        { "POST", "api/teamusers", $$"""{ "teamId": "{{Unknown}}", "userId": "{{Unknown}}" }""" },
        { "DELETE", $"api/teams/{Unknown}/users/{Unknown}", null },
        { "POST", "api/teamcards", $$"""{ "teamId": "{{Unknown}}", "cardId": "{{Unknown}}" }""" },
        { "PUT", $"api/teamcards/{Unknown}", $$"""{ "id": "{{Unknown}}", "teamId": "{{Unknown}}", "cardId": "{{Unknown}}" }""" },
        { "DELETE", $"api/teams/{Unknown}/cards/{Unknown}", null },
        { "DELETE", $"api/teams/{Unknown}/articles/{Unknown}", null },
    };

    public static TheoryData<string> NoContentRoutes => new()
    {
        $"api/system-roles/{Unknown}",
        $"api/groups/{Unknown}",
        $"api/groups/memberships/{Unknown}",
    };

    /// <summary>An id that names no row is answered with a 404 on these routes.</summary>
    [Theory]
    [MemberData(nameof(NotFoundRoutes))]
    public async Task An_unknown_id_is_answered_with_not_found(string method, string route, string body)
    {
        var response = await RootClient.SendAsync(Request(method, route, body), Ct);

        await AssertProblem(HttpStatusCode.NotFound, response);
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
