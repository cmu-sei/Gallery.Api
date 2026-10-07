// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

// gallery.api: GalleryDbContext, migrations in Gallery.Api.Migrations.PostgreSQL, and the run-wide
// factory's step 1B (Program.Main runs InitializeDatabase with no switch to skip it), so the database is
// static and the host gets one throwaway database for the run.

using Gallery.Api.Data;

namespace Gallery.Api.Tests.Support;

/// <summary>
/// Owns the PostgreSQL database for the whole test run: starts it on first use and hands out an isolated
/// session per test.
/// </summary>
/// <remarks>
/// PostgreSQL exercises production's actual database, including the <c>if (Database.IsNpgsql())</c>
/// branch of <c>GalleryDbContext.OnModelCreating</c> and the real migration history. A usable Docker
/// daemon is therefore required by every test that takes a database.
/// </remarks>
public sealed class DatabaseFixture : IAsyncLifetime, ITestDatabaseSessionSource<GalleryDbContext>
{
    private static readonly PostgresTestDatabase<GalleryDbContext> _database = new(new()
    {
        Name = "gallery",
        TestAssembly = "Gallery.Api.Tests",
        // Production computes this as {AssemblyName}.Migrations.{provider} in
        // DatabaseExtensions.UseConfiguredDatabase. Without it EF looks in the context's own assembly and
        // finds none.
        MigrationsAssembly = "Gallery.Api.Migrations.PostgreSQL",
        CreateContext = GalleryContextFactory.CreateContext,
        CreateServices = GalleryContextFactory.CreateServices
    });

    /// <summary>
    /// The host's own database, for <c>Program.Main</c>'s <c>InitializeDatabase</c>. Lazy, and blocking
    /// only inside <c>ConfigureWebHost</c>, which runs when the first test uses the host, so tests that
    /// need no database still run without Docker. Never dropped: the container goes at the end.
    /// </summary>
    private static readonly Lazy<Task<ITestDatabaseSession<GalleryDbContext>>> _host =
        new(() => _database.BeginSessionAsync());

    public static ITestDatabaseSession<GalleryDbContext> HostDatabase() => _host.Value.GetAwaiter().GetResult();

    /// <summary>Nothing to do here: the container starts on the first request for a session.</summary>
    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public Task<ITestDatabaseSession<GalleryDbContext>> BeginSessionAsync() => _database.BeginSessionAsync();

    public ValueTask DisposeAsync() => _database.DisposeAsync();
}
