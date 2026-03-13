# Gallery.Api.Tests.Shared

Copyright 2026 Carnegie Mellon University. All Rights Reserved.

## Purpose

Shared test fixtures and entity customizations for Gallery API test projects. Provides AutoFixture configurations that generate valid test data for Gallery domain entities while avoiding circular references and entity tracking conflicts.

## Files

- **`Fixtures/GalleryCustomization.cs`** - AutoFixture customization for Gallery entities:
  - `CollectionEntity` - Exercise collections with memberships
  - `ExhibitEntity` - Exercise exhibits with moves, injects, teams, and memberships
  - `ArticleEntity` - News/social media items with status and source types
  - `CardEntity` - Display cards with move/inject tracking
  - `TeamEntity` - Exercise teams with users and articles
  - `UserEntity` - Users with team memberships, roles, and group associations
  - `TeamUserEntity` - Team-user relationships with observer flags
  - `TeamArticleEntity` - Team-article associations
  - `TeamCardEntity` - Team-card associations with wall display flags
  - `UserArticleEntity` - User-article read tracking
  - `ExhibitTeamEntity` - Exhibit-team associations
  - `PermissionEntity` - System permissions
  - `UserPermissionEntity` - User-permission grants
  - `GroupEntity` - User groups with memberships
  - `GroupMembershipEntity` - Group-user relationships
  - `CollectionMembershipEntity` - Collection-level role memberships
  - `ExhibitMembershipEntity` - Exhibit-level role memberships
  - `SystemRoleEntity` - System-wide roles with permissions
  - `CollectionRoleEntity` - Collection-scoped roles
  - `ExhibitRoleEntity` - Exhibit-scoped roles
  - `XApiQueuedStatementEntity` - xAPI statement queue entries

## Usage

```csharp
var fixture = new Fixture();
fixture.Customize(new GalleryCustomization());

var collection = fixture.Create<CollectionEntity>();
var exhibit = fixture.Create<ExhibitEntity>();
```

The customization automatically:
- Generates valid GUIDs for entity IDs
- Sets appropriate default values (e.g., `CurrentMove = 0`, `IsObserver = false`)
- Omits navigation properties to prevent circular references
- Uses `OmitOnRecursionBehavior` to handle complex object graphs

## Dependencies

- **AutoFixture** - Test data generation
- **Gallery.Api.Data.Models** - Entity definitions
- **Gallery.Api.Data.Enumerations** - Enum types (`ItemStatus`, `SourceType`, `XApiQueueStatus`)

## Integration

Referenced by:
- `Gallery.Api.Tests.Unit` - Unit tests for services and business logic
- `Gallery.Api.Tests.Integration` - Integration tests (indirectly via unit test patterns)

## Running Tests

This is a shared library project with no executable tests. Tests that consume this library are run via:

```bash
cd /mnt/data/crucible/gallery/gallery.api
dotnet test Gallery.Api.Tests.Unit
dotnet test Gallery.Api.Tests.Integration
```
