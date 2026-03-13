# Gallery.Api.Tests.Integration

Copyright 2026 Carnegie Mellon University. All Rights Reserved.

## Purpose

Integration tests for Gallery API endpoints using real HTTP clients, Testcontainers PostgreSQL database, and in-process ASP.NET Core hosting. Tests full request/response pipeline including authentication, authorization, database interactions, and API contracts.

## Files

### Test Infrastructure

- **`Fixtures/GalleryTestContext.cs`** - WebApplicationFactory-based test context
  - Extends `WebApplicationFactory<Program>` for in-process hosting
  - Uses **Testcontainers PostgreSQL 16-alpine** for real database integration
  - Implements `IAsyncInitializer + IAsyncDisposable` for container lifecycle management
  - Provides `TestUserClaimsService` with `SetCurrentClaimsPrincipal` for actor impersonation
  - Replaces production services with test implementations:
    - `TestAuthenticationHandler` - Test authentication scheme
    - `TestAuthorizationService` - Always-authorized test authorization
    - `TestClaimsTransformation` - No-op claims transformation
    - `TestUserClaimsService` - Mock user claims service
  - Configures `Database:Provider = "InMemory"` to prevent Startup from registering health checks against real connection strings
  - Registers fresh `GalleryDbContext` with Testcontainers PostgreSQL connection string
  - Applies EF Core migrations via `EnsureCreatedAsync` during initialization

### Controller Tests

- **`Tests/Controllers/HealthCheckTests.cs`** - Health endpoint verification
  - `LiveHealthCheck_ReturnsOk` - Verifies `/api/health/live` returns HTTP 200
  - `ReadyHealthCheck_ReturnsOk` - Verifies `/api/health/ready` returns HTTP 200

- **`Tests/Controllers/UserControllerTests.cs`** - User API endpoint tests
  - `GetUsers_ReturnsSuccessStatusCode` - Verifies `/api/users` returns HTTP 200
  - `GetUser_WhenUserExists_ReturnsUser` - Seeds user via `GalleryDbContext`, verifies GET `/api/users/{id}` returns user with correct ID
  - `GetUser_WhenUserDoesNotExist_ReturnsNotFound` - Verifies GET with non-existent ID returns 404 or 200 (implementation-dependent)
  - **Pattern:** Injects dependencies via `[ClassDataSource<GalleryTestContext>(Shared = SharedType.PerTestSession)]`, seeds test data via scoped `GalleryDbContext`

## Key Patterns

### Test Context Setup

```csharp
public class GalleryTestContext : WebApplicationFactory<Program>, IAsyncInitializer, IAsyncDisposable
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public TestAuthenticationUser Actor { get; set; } = new();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        // Apply migrations via EnsureCreatedAsync
    }
}
```

### Test Fixture Usage

```csharp
[ClassDataSource<GalleryTestContext>(Shared = SharedType.PerTestSession)]
public class UserControllerTests(GalleryTestContext factory)
{
    private readonly GalleryTestContext _factory = factory;
    private readonly HttpClient _client = factory.CreateClient();
}
```

### Database Seeding

```csharp
using (var scope = _factory.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<GalleryDbContext>();
    context.Users.Add(new UserEntity { Id = userId, Name = "Test User" });
    await context.SaveChangesAsync();
}
```

### HTTP Assertions

```csharp
var response = await _client.GetAsync("/api/users");
await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

var user = await response.Content.ReadFromJsonAsync<Gallery.Api.ViewModels.User>();
await Assert.That(user).IsNotNull();
await Assert.That(user.Id).IsEqualTo(expectedId);
```

## Dependencies

- **TUnit** 1.19.22 - Test framework
- **Testcontainers.PostgreSql** - Ephemeral PostgreSQL containers for isolated test databases
- **Microsoft.AspNetCore.Mvc.Testing** - `WebApplicationFactory<T>` for in-process hosting
- **Crucible.Common.Testing** - `TestAuthenticationHandler`, `TestAuthorizationService`, `TestClaimsTransformation`
- **EF Core PostgreSQL** - Real database provider
- **Gallery.Api** - API under test

## Running Tests

```bash
# From gallery.api directory
cd /mnt/data/crucible/gallery/gallery.api

# Run all integration tests
dotnet test Gallery.Api.Tests.Integration

# Run specific test class
dotnet test Gallery.Api.Tests.Integration --filter FullyQualifiedName~UserControllerTests

# Run specific test method
dotnet test Gallery.Api.Tests.Integration --filter FullyQualifiedName~UserControllerTests.GetUser_WhenUserExists_ReturnsUser

# Run with verbose output
dotnet test Gallery.Api.Tests.Integration --verbosity detailed

# Run with detailed logging (shows Testcontainers startup)
dotnet test Gallery.Api.Tests.Integration --logger "console;verbosity=detailed"
```

## Architecture Notes

- **Database:** Each test class shares a Testcontainers PostgreSQL instance via `[ClassDataSource<GalleryTestContext>(Shared = SharedType.PerTestSession)]`
- **Isolation:** Testcontainers creates ephemeral databases that are destroyed after test execution
- **Authentication:** `TestAuthenticationHandler` uses `GalleryTestContext.Actor` to impersonate users (default: `TestAuthenticationUser.DefaultUserId`)
- **Authorization:** `TestAuthorizationService` bypasses policy checks (always authorized) for testing business logic without permission constraints
- **DbContext:** Tests use `GalleryDbContext` (not `GalleryContext`) as per Gallery naming conventions
- **Port Binding:** Testcontainers handles dynamic port allocation; no port conflicts
- **Container Lifecycle:** PostgreSQL container starts once per test session (`InitializeAsync`) and stops after all tests complete (`DisposeAsync`)
- **Migration Strategy:** Uses `EnsureCreatedAsync` to apply migrations without requiring migration files in test project

## TestUserClaimsService

The test implementation provides:
- `AddUserClaims(principal, update)` - Returns principal unchanged
- `GetClaimsPrincipal(userId, setAsCurrent)` - Returns empty principal
- `RefreshClaims(userId)` - Returns empty principal
- `GetCurrentClaimsPrincipal()` - Returns empty principal
- `SetCurrentClaimsPrincipal(principal)` - No-op (used for actor impersonation in tests)

This allows tests to bypass user claims hydration while maintaining service contract compatibility.
