# api

This project provides a restful api for the Gallery Wall and Archive.

By default, api is available at localhost:4722, with the swagger page at localhost:4722/api/index.html.

# Database Migrations

When the data model is changed, a new database migration must be created.  From the Gallery.Api directory, run this command to create the new migration:
    dotnet ef migrations add new_migration_name --project ../Gallery.Api.Migrations.PostgreSQL/Gallery.Api.Migrations.PostgreSQL.csproj
To Roll back a migration, first update the database to the previous migration
    dotnet ef database update <previous_migration_name> --project ../Gallery.Api.Migrations.PostgreSQL/Gallery.Api.Migrations.PostgreSQL.csproj
Then remove the migration
    dotnet ef migrations remove --project ../Gallery.Api.Migrations.PostgreSQL/Gallery.Api.Migrations.PostgreSQL.csproj


# Permissions

SystemAdmin permission required for:
    * User admin
    * Collection and Exhibit create/delete

Content Developer permission required for:
    * Collection and Exhibit create/delete

Authenticated User
    * No access to Admin pages

