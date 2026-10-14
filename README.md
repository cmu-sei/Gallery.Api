# api

This project provides a restful api for the Gallery Wall and Archive.

It targets .NET 10 (SDK 10.0.100, see `global.json`). Run `dotnet run` from the Gallery.Api directory and the api is available at localhost:4722, with the swagger page at localhost:4722/api/index.html. The Docker image listens on port 8080.

# Database Migrations

When the data model is changed, a new database migration must be created. The commands below need the `dotnet-ef` tool (`dotnet tool install --global dotnet-ef`) and `Database:Provider` set to `PostgreSQL`, as it is in appsettings.json. From the Gallery.Api directory, run this command to create the new migration:
    dotnet ef migrations add new_migration_name --project ../Gallery.Api.Migrations.PostgreSQL/Gallery.Api.Migrations.PostgreSQL.csproj
To Roll back a migration, first update the database to the previous migration
    dotnet ef database update <previous_migration_name> --project ../Gallery.Api.Migrations.PostgreSQL/Gallery.Api.Migrations.PostgreSQL.csproj
Then remove the migration
    dotnet ef migrations remove --project ../Gallery.Api.Migrations.PostgreSQL/Gallery.Api.Migrations.PostgreSQL.csproj


# Permissions

Access is governed by roles and permissions. See [Gallery.Api/docs/Permissions.md](Gallery.Api/docs/Permissions.md) for the model, seed data and Identity Provider integration.

System Roles grant system permissions across Gallery. The default System Roles are:
    * Administrator: all permissions
    * Content Developer: CreateCollections, CreateExhibits, ManageExhibits, ViewCollections, ViewExhibits, ViewRoles and ViewGroups
    * Observer: every View permission

Collection and Exhibit memberships grant permissions on one Collection or Exhibit through a Collection or Exhibit Role (Manager, Member or Observer by default).

Team membership grants permissions on the team, and participation in its Exhibit and Collection.

The Administration area is available to any user holding at least one system permission.

# Testing

The tests are in the Gallery.Api.Tests project. Docker must be running: the tests start their own PostgreSQL container through Testcontainers.
    dotnet test Gallery.Api.Tests
See [docs/Testing.md](docs/Testing.md) for how the tests are built and how to add one.
