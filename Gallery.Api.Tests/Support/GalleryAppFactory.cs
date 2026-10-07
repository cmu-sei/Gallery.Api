// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

// gallery.api: the run-wide factory with step 1B (a throwaway host database for InitializeDatabase), the
// two hubs MainHub and CiteHub as recorders, and the stub HTTP handler behind IHttpClientFactory, which is
// how SteamfitterService reaches Steamfitter and the identity provider.

using System.Collections.Concurrent;
using Gallery.Api.Data;
using Gallery.Api.Hubs;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Gallery.Api.Tests.Support;

/// <summary>
/// Hosts <c>Gallery.Api</c> in process over <c>TestServer</c>, so that tests drive the real application:
/// the real <c>Startup</c>, the real middleware chain, the real MVC filters, the real authorization stack
/// and the real claims transformer.
/// </summary>
/// <remarks>
/// <para>
/// One instance serves the whole run, declared in <c>AssemblyFixtures.cs</c>. Everything the application
/// registers as a singleton is therefore shared by every test, which is what
/// <see cref="TestConfiguration"/>'s claims-caching entry and <see cref="TestDatabaseScope"/> exist to
/// deal with. Only three things are not the application's own: token validation
/// (<see cref="TestAuthHandler"/>), the context registration (<see cref="TestDatabaseScope"/>), and the
/// collaborators that leave the process.
/// </para>
/// <para>
/// <c>Program.CreateWebHostBuilder</c> matches neither convention <c>HostFactoryResolver</c> looks for, so
/// <c>WebApplicationFactory</c> invokes <c>Program.Main</c>, which runs <c>InitializeDatabase</c> with no
/// production switch to skip it. The host is therefore pointed at a throwaway database cloned from the
/// migrated template, where migrating is a no-op and the (empty) shipped seed data changes nothing.
/// </para>
/// </remarks>
public sealed class GalleryAppFactory : WebApplicationFactory<Program>, ITestHttpHost
{
    /// <summary>Answers every request the application makes over HTTP. Arrange a url of your own on it.</summary>
    public StubHttpMessageHandler OutboundHttp { get; } = new();

    private readonly ConcurrentDictionary<Type, object> _hubs = new();

    /// <summary>What the application broadcast through a hub, per audience.</summary>
    public HubRecorder<THub> Hub<THub>() where THub : Hub =>
        (HubRecorder<THub>)_hubs.GetOrAdd(typeof(THub), _ => new HubRecorder<THub>());

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Production, so the developer exception page stays off and ExceptionMiddleware answers as it
        // does in a deployment: a 500's Title is "A server error occurred." and its Detail the message.
        builder.UseEnvironment("Production");

        // Step 1B: Main's InitializeDatabase needs a real database to migrate and seed.
        builder.UseSetting("Database:Provider", "PostgreSQL");
        builder.UseSetting("ConnectionStrings:PostgreSQL", DatabaseFixture.HostDatabase().ConnectionString);

        builder.ConfigureAppConfiguration(configuration =>
            configuration.AddInMemoryCollection(TestConfiguration.Values));

        builder.ConfigureTestServices(services =>
        {
            // XApiBackgroundService starts in the background and reaches for a database no test owns.
            services.RemoveAll<IHostedService>();

            services
                .AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, null);

            // InitializeDatabase resolves the context from a scope of its own, outside any request, where
            // there is no X-Test-Session header to route by. Until the host has started, such a resolution
            // gets the host's own database, over that session's own services, so the seed's entity events
            // never reach the real handlers or a recorder; afterwards it throws, so a stray resolution
            // outside a request still fails loudly.
            TestDatabaseScope.ReplaceRegistration<GalleryDbContext>(
                services, () => _started ? null : DatabaseFixture.HostDatabase());

            // SignalR registers hub contexts as an open generic, which RemoveAll of a closed type cannot
            // match; a later closed registration wins on resolution.
            services.AddSingleton<IHubContext<MainHub>>(Hub<MainHub>());
            services.AddSingleton<IHubContext<CiteHub>>(Hub<CiteHub>());

            services.RemoveAll<IHttpClientFactory>();
            services.AddSingleton<IHttpClientFactory>(new StubHttpClientFactory(OutboundHttp));
        });
    }

    private readonly Lock _creating = new();
    private IHost _host;

    /// <summary>Set once the host has started, which is after <c>Program.Main</c>'s <c>InitializeDatabase</c>.</summary>
    private volatile bool _started;

    /// <summary>Builds the one host, under a lock, and hands it to every caller.</summary>
    /// <remarks>
    /// <c>WebApplicationFactory.StartServer</c> takes no lock, so two tests asking for their first client at
    /// once would each build a host (each running <c>Program.Main</c>), and the run would continue on two
    /// of them. Every way into the host (<c>CreateClient</c> in any overload, <c>Services</c>,
    /// <c>Server</c>) goes through <c>StartServer</c> to here, so this lock covers them all.
    /// <c>base.CreateHost</c> returns once the host has started, and the deferred host starts only after
    /// <c>Main</c> has run <c>InitializeDatabase</c>.
    /// </remarks>
    protected override IHost CreateHost(IHostBuilder builder)
    {
        lock (_creating)
        {
            if (_host is null)
            {
                _host = base.CreateHost(builder);
                _started = true;
            }

            return _host;
        }
    }
}
