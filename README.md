# api

This project provides a restful api for the Gallery Wall and Archive.

By default, api is available at localhost:4722, with the swagger page at localhost:4722/api/index.html.

# Database Migrations

When the data model is changed, a new database migration must be created.  From the Gallery.Api directory, run this command to create the new migration:
    dotnet ef migrations add new_migration_name --project ../Gallery.Api.Migrations.PostgreSQL/Gallery.Api.Migrations.PostgreSQL.csproj


# Permissions

SystemAdmin permission required for:
    * User admin
    * Evaluation create/delete

Content Developer permission required for:
    * ScoringModel create/update/delete
    * Evaluation update

CanIncrementIncident required for:
    * Evaluation update

CanSubmit permission required for:
    * Submission update for a team

CanModify permission required for:
    * Setting SubmissionOption value for a team

Authenticated user has permission to:
    * Create a Submission

