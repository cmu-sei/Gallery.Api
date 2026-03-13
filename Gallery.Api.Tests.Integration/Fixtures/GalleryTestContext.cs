// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Crucible.Common.Testing.Auth;
using Crucible.Common.Testing.Extensions;
using Gallery.Api.Data;
using Gallery.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace Gallery.Api.Tests.Integration.Fixtures;

public class GalleryTestContext : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public TestAuthenticationUser Actor { get; set; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Override the Database:Provider to InMemory so Startup doesn't register
        // health checks against a real database connection string.
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Provider"] = "InMemory",
                ["Database:AutoMigrate"] = "false",
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContextFactory and DbContext registrations
            services.RemoveServices<IDbContextFactory<GalleryDbContext>>();
            services.RemoveService<GalleryDbContext>();

            // Remove any DbContextOptions registrations
            var dbContextOptionsDescriptors = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<GalleryDbContext>)
                    || d.ServiceType == typeof(DbContextOptions))
                .ToList();
            foreach (var descriptor in dbContextOptionsDescriptors)
            {
                services.Remove(descriptor);
            }

            // Register a fresh DbContext using Testcontainers PostgreSQL
            services.AddDbContext<GalleryDbContext>(options =>
            {
                options.UseNpgsql(_postgres.GetConnectionString());
            });

            // Replace auth
            services.AddAuthentication(TestAuthenticationHandler.AuthenticationSchemeName)
                .AddScheme<TestAuthenticationHandlerOptions, TestAuthenticationHandler>(
                    TestAuthenticationHandler.AuthenticationSchemeName,
                    options => options.Actor = Actor);

            services.ReplaceService<IAuthorizationService, TestAuthorizationService>();
            services.ReplaceService<IClaimsTransformation, TestClaimsTransformation>();

            // Replace IUserClaimsService with a fake that does nothing
            services.RemoveService<IUserClaimsService>();
            services.AddScoped<IUserClaimsService, TestUserClaimsService>();
        });
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        // Ensure database is created and migrations applied
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GalleryDbContext>();
        await context.Database.EnsureCreatedAsync();
    }

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }
}

/// <summary>
/// A no-op IUserClaimsService for integration testing.
/// </summary>
internal class TestUserClaimsService : IUserClaimsService
{
    public Task<System.Security.Claims.ClaimsPrincipal> AddUserClaims(System.Security.Claims.ClaimsPrincipal principal, bool update)
        => Task.FromResult(principal);

    public Task<System.Security.Claims.ClaimsPrincipal> GetClaimsPrincipal(Guid userId, bool setAsCurrent)
        => Task.FromResult(new System.Security.Claims.ClaimsPrincipal());

    public Task<System.Security.Claims.ClaimsPrincipal> RefreshClaims(Guid userId)
        => Task.FromResult(new System.Security.Claims.ClaimsPrincipal());

    public System.Security.Claims.ClaimsPrincipal GetCurrentClaimsPrincipal()
        => new System.Security.Claims.ClaimsPrincipal();

    public void SetCurrentClaimsPrincipal(System.Security.Claims.ClaimsPrincipal principal) { }
}
