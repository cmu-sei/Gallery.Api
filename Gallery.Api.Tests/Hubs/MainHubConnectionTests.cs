// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Net;
using Gallery.Api.Data.Enumerations;
using Gallery.Api.Hubs;
using Gallery.Api.Tests.Support;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;

namespace Gallery.Api.Tests.Hubs;

/// <summary>
/// <see cref="MainHub"/>'s own <c>[Authorize(AuthenticationSchemes = "Bearer")]</c>, over a real SignalR
/// connection to the in-process server at <c>/hubs/main</c>, where <c>Startup.Configure</c> maps it.
/// </summary>
public class MainHubConnectionTests(DatabaseFixture fixture, GalleryAppFactory factory) : ApiTestBase(fixture, factory)
{
    private const string HubPath = "/hubs/main";

    /// <summary>A token scoped for another API only, the scope the default policy does not accept.</summary>
    private const string AnotherApisScope = "steamfitter";

    [Fact]
    public async Task An_actor_connects_over_WebSockets_and_joins()
    {
        var actor = await Actor().SeedAsync();
        await using var connection = Connection(actor);

        await connection.StartAsync(Ct);
        await connection.InvokeAsync(nameof(MainHub.Join), Ct);

        Assert.Equal(HubConnectionState.Connected, connection.State);
    }

    /// <summary>The default policy refuses an actor whose token lacks the gallery scope, whatever permissions it holds; An_actor_connects_over_WebSockets_and_joins is its control.</summary>
    [Fact]
    public async Task Negotiate_for_an_actor_whose_token_lacks_the_gallery_scope_is_forbidden()
    {
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewExhibits).SeedAsync();
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{HubPath}/negotiate?negotiateVersion=1");
        request.Headers.Add(TestAuthHandler.ScopeHeader, AnotherApisScope);

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).SendAsync(request, Ct));
    }

    /// <summary>A request with no identity is answered with a 401; the anonymous client is the case under test.</summary>
    [Fact]
    public async Task Negotiate_without_an_identity_is_unauthorized()
    {
        await AssertStatus(HttpStatusCode.Unauthorized, await Client().PostAsync($"{HubPath}/negotiate?negotiateVersion=1", null, Ct));
    }

    /// <summary>A WebSocket to the TestServer, carrying the headers every ApiTestBase client sends.</summary>
    /// <remarks>
    /// Under long polling a hub invocation runs outside any request, where no X-Test-Session header names the
    /// test's database; a WebSocket keeps the connection's own request, headers included, for every invocation.
    /// </remarks>
    private HubConnection Connection(TestActor actor)
    {
        var session = Client().DefaultRequestHeaders.GetValues(TestDatabaseScope.HeaderName).Single();

        return new HubConnectionBuilder()
            .WithUrl($"http://localhost{HubPath}", options =>
            {
                options.Transports = HttpTransportType.WebSockets;
                options.SkipNegotiation = true;
                options.WebSocketFactory = async (context, ct) =>
                {
                    var client = Factory.Server.CreateWebSocketClient();
                    client.ConfigureRequest = request =>
                    {
                        request.Headers[TestAuthHandler.UserHeader] = actor.Id.ToString();
                        request.Headers[TestAuthHandler.NameHeader] = actor.Name;
                        request.Headers[TestDatabaseScope.HeaderName] = session;
                    };

                    return await client.ConnectAsync(context.Uri, ct);
                };
            })
            .Build();
    }
}
