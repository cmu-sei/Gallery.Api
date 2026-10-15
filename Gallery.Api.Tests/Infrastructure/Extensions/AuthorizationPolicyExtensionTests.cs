// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Net;
using Gallery.Api.Data.Enumerations;
using Gallery.Api.Tests.Support;

namespace Gallery.Api.Tests.Infrastructure.Extensions;

/// <summary>
/// The default policy <c>AuthorizationPolicyExtensions.AddAuthorizationPolicy</c> builds: an authenticated
/// user whose token carries every scope in <c>Authorization:AuthorizationScope</c> ("gallery" as shipped).
/// Every controller takes it through <c>BaseController</c>'s <c>[Authorize]</c>, and the same scope again
/// through the global <c>AuthorizeFilter</c> <c>Startup.ConfigureServices</c> adds to MVC; the hubs take the
/// default policy only (<c>MainHubConnectionTests</c>, <c>CiteHubConnectionTests</c>).
/// </summary>
public class AuthorizationPolicyExtensionTests(DatabaseFixture fixture, GalleryAppFactory factory) : ApiTestBase(fixture, factory)
{
    /// <summary>A token scoped for another API only, the scope the default policy does not accept.</summary>
    private const string AnotherApisScope = "steamfitter";

    /// <summary>The default policy refuses a token without the gallery scope, whatever permissions its user holds.</summary>
    [Fact]
    public async Task A_request_whose_token_lacks_the_gallery_scope_is_forbidden_but_allowed_with_it()
    {
        var other = TestData.User(name: "Scoped");
        await Seed(other);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewUsers).SeedAsync();
        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/users/{other.Id}");
        request.Headers.Add(TestAuthHandler.ScopeHeader, AnotherApisScope);

        var refused = await Client(actor).SendAsync(request, Ct);

        await AssertStatus(HttpStatusCode.Forbidden, refused);
        await AssertStatus(HttpStatusCode.OK, await Client(actor).GetAsync($"api/users/{other.Id}", Ct));
    }
}
