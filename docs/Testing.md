Gallery.Api has an automated test suite in the `Gallery.Api.Tests` project. This document covers how to run it, how it is built, and what is specific to Gallery. The suite follows the Crucible API test standard in `agent-docs/api-testing/` of the workspace, which every Crucible API shares. The files under `Gallery.Api.Tests/Support/Shared/` are copied from there and are not edited here. The standard's `README.md` describes the shared harness and `CONVENTIONS.md` says how tests are written. Neither is repeated below.

# Testing

The suite has 523 tests.

The suite uses xUnit v3 and NSubstitute, and runs against a real PostgreSQL instance in a container. Most tests are not isolated unit tests. A typical test sends an HTTP request to the application hosted in process, through the real routes, MVC filters, `ExceptionMiddleware`, claims transformer, authorization handlers, services, AutoMapper profiles and entity event handlers, over a real database. It then asserts on the response, on the database and on what the hubs were sent. Only collaborators that leave the process are replaced.

# Running the tests

```bash
dotnet test Gallery.Api.Tests
```

Docker must be running. The suite starts and disposes its own `postgres:16-alpine` container through Testcontainers, so there is no local database to install or keep in sync. The container starts when the first test asks for a database, so tests that need none still run without Docker.

Filters:

```bash
dotnet test Gallery.Api.Tests --filter "FullyQualifiedName~.ExhibitControllerTests"
dotnet test Gallery.Api.Tests -- xUnit.DiagnosticMessages=true    # prints the provider banner
```

Never set `diagnosticMessages` in `xunit.runner.json`. If you do, the run hangs after the last test (see the standard's README, "Known pitfalls").

# Coverage

```bash
dotnet test Gallery.Api.Tests --collect:"XPlat Code Coverage"
```

`Gallery.Api.Tests.csproj` names `coverlet.runsettings` in `RunSettingsFilePath`, so the exclusions always apply. They cover the `Gallery.Api.Migrations.PostgreSQL` assembly, the `Crucible.Common.EntityEvents` content files and generated code. The collector is disabled unless you ask for it. coverlet's cobertura output lists every class twice, so read `lines-covered` and `lines-valid` on the root element rather than summing the classes. CI does not collect coverage.

# Build settings

- `Gallery.Api.Tests/Directory.Build.props` is the standard's file. It sets `TreatWarningsAsErrors` for the test project only, which makes the xUnit analyzers fail the build (xUnit1051 for a missing `Ct`, xUnit2000, xUnit2029, and so on). NuGet audit and restore warnings (NU1901-NU1904, NU1510, NU1701 for TinCan) stay warnings. The repository has no root `Directory.Build.props`, so the application projects are not built with warnings as errors. That is how the standard intends it, and they report warnings today: CS1573 in the controllers, CS8981 in two generated migrations and CS8073 in `CollectionService`. The standard's `verify.sh` counts only the test project's own warnings and lists the application projects' warnings as a note (`note: application projects warn (not counted): CS1573 CS8073 CS8981`), so they never fail the check. The test project itself builds with no warning.
- The root `.editorconfig` raises xUnit1004, so `[Fact(Skip = ...)]` fails the build. A test with nothing to assert under some condition uses `Assert.SkipWhen`.
- The repository has no central package management, so the pinned test packages (the standard's `test-packages.props`) are `Version=` attributes in `Gallery.Api.Tests.csproj`. `sync.sh` checks them. That includes the standard's optional `Microsoft.AspNetCore.SignalR.Client` pin (10.0.1), which the hubs' real-connection tests use.
- The runner is VSTest (`Microsoft.NET.Test.Sdk`, `xunit.runner.visualstudio`, `coverlet.collector`), as in every Crucible API. The standard's README explains why it does not use Microsoft.Testing.Platform yet.

# How the harness works

The shared mechanics (per-test databases cloned from a migrated template, `X-Test-Session` routing, `TestAuthHandler`, the recorders) are in the standard's README, "How the harness works". Gallery's specifics:

- **Base classes.** `DatabaseTestBase` provides `Db`, `NewContext()`, `Mediator`, `Ct` and `Seed`. `ApiTestBase` adds `Root` (the seeded Administrator role), `RootClient`, `Actor()`, `Client(actor)`, `Client()` (no identity, so 401), `ReadAsync`, `AssertStatus`, `AssertProblem` and `Factory`. Gallery has no `ServiceTestBase`. A service that needs options a test chose is constructed directly, as in `UserClaimsServiceTests`.
- **Fixtures.** `AssemblyFixtures.cs` declares `DatabaseFixture` and `GalleryAppFactory` as assembly fixtures. One host serves the whole run.
- **`GalleryAppFactory`** uses the run-wide variant with step 1B and the template's Bearer recipe: `MainHub` and `CiteHub` carry `[Authorize(AuthenticationSchemes = "Bearer")]`, so the factory removes every `IConfigureOptions<AuthenticationOptions>` (Startup's JWT bearer registration among them) and registers `TestAuthHandler` under its own name (the default scheme) and under `Bearer`. `Program.Main` runs `InitializeDatabase` with no switch to skip it, so the host is pointed at a throwaway database cloned from the template (`DatabaseFixture.HostDatabase()`), where migrating is a no-op. `InitializeDatabase` resolves `GalleryDbContext` outside a request. The factory registers the context with the standard's two-argument `TestDatabaseScope.ReplaceRegistration(services, () => _started ? null : DatabaseFixture.HostDatabase())`. Until the host has started, a resolution with no request gets the host database over that session's own services, so the seed's entity events reach a substituted mediator, not the real handlers or the recorders. After startup, such a resolution throws. `CreateHost` builds the one host under a lock, as the template does, because `WebApplicationFactory.StartServer` takes none and two tests asking for their first client at once would otherwise each run `Program.Main`. `HttpHarnessTests.A_request_never_writes_to_the_host_database` checks that no request reaches the host database.
- **What is replaced.** Token validation (`TestAuthHandler`, under the `Test` and `Bearer` names), the `GalleryDbContext` registration, `IHubContext<MainHub>` and `IHubContext<CiteHub>` (`Factory.Hub<MainHub>()` and `Factory.Hub<CiteHub>()`, both `HubRecorder`s), `IHttpClientFactory` (`Factory.OutboundHttp`, which `SteamfitterService` and the token request would use), and the hosted `XApiBackgroundService`, which is removed. Nothing in the host is a substitute.
- **Scopes.** Every identity carries the shipped `Authorization:AuthorizationScope` ("gallery") from `TestAuthHandler`. The scope requirement itself, in the default policy (`AuthorizationPolicyExtensions`) and again in the global MVC `AuthorizeFilter` (`Startup.ConfigureServices`), is tested with `X-Test-Scope` naming another API's scope: `Infrastructure/Extensions/AuthorizationPolicyExtensionTests` for the controllers, and one negotiate test in each hub's connection tests.
- **`TestConfiguration`** turns off `ClaimsTransformation:EnableCaching` only. The IdP role and group lookups need no override: `TestAuthHandler` mints no role or group claims, so they have nothing to read. `UserClaimsServiceTests` drives the token path directly. The shipped provider is already PostgreSQL. xAPI stays unconfigured as shipped (empty `XApiOptions:Username`), so the xAPI routes skip their statements.
- **`TestActor`** mirrors `UserClaimsService.GetPermissionClaims`:
  - `WithSystemPermissions(...)` or `WithRole(id)` sets `User.RoleId`, and `WithAllSystemPermissions()` uses the seeded Administrator role.
  - `OnCollection(collection, permissions | roleId)` and `OnExhibit(exhibit, permissions | roleId)` create a membership whose role is minted for exactly those permissions. With neither, the role is the seeded `Member` (View and Edit).
  - `OnTeam(team, observer)` creates a `TeamUser`. That grants `ParticipateTeam` and `ViewTeam` on the team, `ParticipateExhibit` on its exhibit and `ParticipateCollection` on its collection. An observer also gets `ViewTeam` on every other team of the exhibit.
  - Near misses use `OnNewCollection(permissions)`, `OnNewExhibit(collectionId, permissions)` and `OnNewTeam(exhibitId)`. The minted ids are on `TestActor.NewCollectionIds`, `NewExhibitIds` and `NewTeamIds`.
- **Team gates have no system-permission path.** `Root` is refused on team-gated routes such as `GET teams/{id}`, so their allowed case is a participant (`OnTeam`) and their near miss a participant on a sibling team (`OnNewTeam`). The defect document lists this under "Needs a decision".
- **Self-tests.**
  - `DatabaseHarnessTests` probes isolation on the unique `system_roles.name`.
  - `HttpHarnessTests` checks the 401, the 403 for an empty actor, routing, the swagger document and that requests never reach the host database. Its concurrency probes `POST api/system-roles` with one shared name. Role names are uniquely indexed, so two tests sharing a database would collide with a 409.
  - `TestActorTests` checks each actor shape against the real `UserClaimsService`.
- **Hubs and handlers.** Each hub's own `[Authorize]` is tested over a real SignalR connection (`MainHubConnectionTests`, `CiteHubConnectionTests`): an actor connects over WebSockets (`SkipNegotiation`, a `WebSocketFactory` over `Factory.Server.CreateWebSocketClient()` carrying the actor's `X-Test-User`/`X-Test-Name` and the test's `X-Test-Session` headers) and invokes `Join`, and a negotiate with no identity is a 401. WebSockets because under long polling an invocation has no request carrying `X-Test-Session`. Hub methods are driven with `HubHarness` (`Hubs/`). Entity event handlers are reached over HTTP and read on the recorders by a group a test-owned id names (`Infrastructure/EventHandlers/`). The membership handlers send to `Clients.Groups(name)`. `MembershipHandlerTests` reads those sends with the recorder's `ToGroups(name)`, keyed on a membership id the test created.

# Adding a test

Follow the standard's CONVENTIONS.md section 2. In short:

1. Put the file where the code lives (`Controllers/`, `Services/`, `Hubs/`, `Infrastructure/...`).
2. Derive from `ApiTestBase` for a request or `DatabaseTestBase` for a database only, or use no base class.
3. Seed with `TestData` mothers and `Actor()`.
4. Name the test as a sentence, pass `Ct`, and re-read through `NewContext()`.

Denied cases are near misses (`OnNew*`, or a neighbouring permission), named `<Operation>_is_forbidden_for_a_caller_holding_only_<Permission>` or `..._only_on_another_<resource>`. A defect gets a passing test of the current behaviour and an entry in `agent-docs/api-test-bugs/gallery.api.md`, never a note in the test. The document lists 38 entries and 4 "Needs a decision" items. Recipes A to E cover the shared fixes:

- A: the gate reads the parent the body names, not the stored row's.
- B: a bad request thrown as a non-API exception.
- C: the route's parent id ignored.
- D: a body naming another id answered as forbidden.
- E: a membership update granting a role above the caller's.

A pattern that repeats across routes is one `[Theory]` over a route table (`UnknownIdTests`) with one entry.

Every authorization call site needs an allowed case that is not `Root` and a near-miss denial; the standard's `check-repo.js gates` (run by `verify.sh`) fails any that lacks one. Two gates cannot meet that because a characterized defect stands in the way (`ArticleController.GetByExhibit` answers every near miss with a 500; `ExhibitTeamController.Get` without an id refuses exactly the callers holding the `ViewExhibits` it names). They are listed, with their reasons, in the workspace's `agent-docs/api-test-gaps/gallery.api.txt`, outside the repo; when one of those defects is fixed, its entry's **Test to flip:** says to drop the line.

Where a file needs the same graph in several tests, it has a private seed helper, such as `SeedPostedArticle(canPost)` in `ArticleControllerTests`. It seeds an exhibit, a team whose card allows posting or not, and an article posted on that card, for the exhibit-article gates of create, update and delete.

# Layout

```
Gallery.Api.Tests/
  AssemblyFixtures.cs
  Controllers/          every controller over HTTP; UnknownIdTests is the route table of unknown-id answers
  Hubs/                 MainHub, CiteHub (HubHarness), MainHubConnectionTests, CiteHubConnectionTests (real connections)
  Infrastructure/
    Authorization/      the requirement handlers and AuthorizationService
    EventHandlers/      entity event handlers and their broadcasts, one file per handler file (ArticleHandlerTests,
                        CardHandlerTests, CollectionHandlerTests, ExhibitHandlerTests, TeamHandlerTests,
                        TeamCardHandlerTests, UserArticleHandlerTests, UserHandlerTests, MembershipHandlerTests)
    Exceptions/         ExceptionMiddleware shapes, malformed bodies and routes, enums
    Extensions/         ClaimsPrincipalExtensions, StringExtensions
    Mappings/           AutoMapper profiles through TestMapper
  Services/             UserClaimsService options the host turns off
  Support/              DatabaseFixture, GalleryContextFactory, GalleryAppFactory, TestConfiguration,
                        DatabaseTestBase, ApiTestBase, TestActor, TestData, ClaimsPrincipalBuilder,
                        AuthorizationHarness, TestMapper, and the three self-test classes
    Shared/             the standard's shared files (sync.sh), never edited here
```

# Continuous integration

`.github/workflows/build-and-test.yml` is the standard's workflow with `Gallery.Api.Tests` filled in, and `sync.sh` keeps it identical. It runs on pull requests and on pushes to `main`. It restores, builds and runs the suite, then greps the `[Gallery.Api.Tests] database provider: PostgreSQL` banner out of the log, and uploads the TRX as `test-results`. Before delivering a change, run `agent-docs/api-testing/verify.sh --app gallery.api <repo>` from the workspace.
