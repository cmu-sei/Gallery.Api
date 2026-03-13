# Gallery.Api.Tests.Unit

Copyright 2026 Carnegie Mellon University. All Rights Reserved.

## Purpose

Unit tests for Gallery API services and infrastructure. Tests business logic in isolation using in-memory databases, mocked dependencies, and AutoFixture test data generation.

## Files

### Infrastructure Tests

- **`MappingConfigurationTests.cs`** - AutoMapper profile validation
  - Validates mapper configuration can be created without errors
  - Uses `IgnoreNullSourceValues` for nullable-to-non-nullable mappings
  - Verifies all profiles in `Gallery.Api.Startup` assembly

### Service Tests

- **`Services/CollectionServiceTests.cs`** - CollectionService CRUD operations
  - `GetAsync` - Retrieves all collections when user can view all
  - `GetAsync` by ID - Retrieves single collection by ID
  - `DeleteAsync` - Deletes existing collection, throws `EntityNotFoundException` when missing
  - `UpdateAsync` - Throws `EntityNotFoundException` when collection does not exist
  - **Pattern:** Per-test factory method `CreateTestContext()` creates fresh `GalleryDbContext` per test to avoid EF Core entity tracking conflicts

- **`Services/ExhibitServiceTests.cs`** - ExhibitService CRUD operations
  - `GetAsync` - Retrieves all exhibits when user can view all
  - `GetAsync` by ID - Retrieves single exhibit with collection relationship
  - `CreateAsync` - Validates `CollectionId` is required, throws `EntityNotFoundException` for non-existent collections
  - `DeleteAsync` - Deletes existing exhibit, throws `EntityNotFoundException` when missing
  - `UpdateAsync` - Throws `EntityNotFoundException` when exhibit does not exist
  - **Pattern:** Constructor-based test fixture (shared context across tests within class)

- **`Services/UserServiceTests.cs`** - UserService CRUD operations
  - `DeleteAsync` - Deletes user, prevents self-deletion via `ForbiddenException`, throws `EntityNotFoundException` when missing
  - `UpdateAsync` - Validates ID consistency, throws `EntityNotFoundException` when user does not exist
  - `CreateAsync` - Creates user, uses real `MapperConfiguration` with `UserProfile` because service internally calls `GetAsync` which uses `ProjectTo<User>()` requiring real `IConfigurationProvider`
  - **Pattern:** Constructor-based test fixture with selective real mapper usage

## Key Patterns

### Per-Test Context Factory (CollectionServiceTests)

```csharp
private static (GalleryDbContext context, CollectionService sut, IMapper mapper, IFixture fixture) CreateTestContext()
{
    var fixture = new Fixture();
    fixture.Customize(new GalleryCustomization());

    var context = TestDbContextFactory.Create<GalleryDbContext>();
    var userClaimsService = A.Fake<IUserClaimsService>();
    var mapper = A.Fake<IMapper>();
    var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("sub", userId.ToString()) }, "TestAuth"));

    var sut = new CollectionService(context, userClaimsService, user, mapper);
    return (context, sut, mapper, fixture);
}
```

Creates fresh in-memory database per test, preventing entity tracking conflicts across tests.

### Constructor-Based Fixture (ExhibitServiceTests, UserServiceTests)

```csharp
private readonly GalleryDbContext _context;
private readonly ExhibitService _sut;

public ExhibitServiceTests()
{
    _context = TestDbContextFactory.Create<GalleryDbContext>();
    // ... initialize mocks and SUT
}
```

Shares context across tests within a class (suitable when tests don't modify state or use disjoint data).

### Real Mapper for ProjectTo Scenarios (UserServiceTests.CreateAsync)

```csharp
var config = new MapperConfiguration(cfg =>
    cfg.AddProfile<Gallery.Api.Infrastructure.Mappings.UserProfile>());
var realMapper = config.CreateMapper();
var sut = new UserService(context, _user, _authorizationService, _logger, realMapper);
```

Uses real AutoMapper when service internally calls `ProjectTo<T>()` which requires `IConfigurationProvider`.

## Dependencies

- **xUnit** - Test framework
- **FakeItEasy** - Mocking framework
- **AutoFixture** - Test data generation
- **Shouldly** - Fluent assertions (implicitly via patterns)
- **Crucible.Common.Testing** - `TestDbContextFactory.Create<GalleryDbContext>()`
- **Gallery.Api.Tests.Shared** - `GalleryCustomization` fixture
- **EF Core InMemory** - In-memory database provider

## Running Tests

```bash
# From gallery.api directory
cd /mnt/data/crucible/gallery/gallery.api

# Run all unit tests
dotnet test Gallery.Api.Tests.Unit

# Run specific test class
dotnet test Gallery.Api.Tests.Unit --filter FullyQualifiedName~CollectionServiceTests

# Run specific test method
dotnet test Gallery.Api.Tests.Unit --filter FullyQualifiedName~CollectionServiceTests.GetAsync_WhenCanViewAll_ReturnsAllCollections

# Run with verbose output
dotnet test Gallery.Api.Tests.Unit --verbosity detailed

# Generate coverage report (requires coverlet.collector)
dotnet test Gallery.Api.Tests.Unit --collect:"XPlat Code Coverage"
```

## Architecture Notes

- **DbContext:** Tests use `GalleryDbContext` (not `GalleryContext`) as per Gallery naming conventions
- **Mocking:** FakeItEasy used for all dependencies except when real implementations required (AutoMapper `ProjectTo<T>()`)
- **Assertions:** Standard xUnit assertions (`Assert.Equal`, `Assert.NotNull`, `Assert.ThrowsAsync`)
- **Test Isolation:** Per-test factory pattern or constructor-based fixtures prevent test interdependencies
- **Entity Tracking:** Fresh contexts per test avoid EF Core tracking conflicts when multiple tests modify same entity IDs
