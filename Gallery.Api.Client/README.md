# How to create the nuget package for Gallery.Api.Client

1. cd ../Gallery.Api
2. dotnet tool restore
3. dotnet build
4. dotnet swagger tofile --output ../Gallery.Api.Client/swagger.json bin/Debug/net10.0/Gallery.Api.dll v1
5. cd ../Gallery.Api.Client
6. npm install
7. ./node_modules/.bin/nswag run /runtime:Net100
8. dotnet pack -c Release /p:version=<version>

*** NOTE: `dotnet tool restore` installs the `dotnet swagger` command from the tool manifest in Gallery.Api/.config/dotnet-tools.json (Swashbuckle.AspNetCore.Cli 10.0.1).

The version in the tool manifest must match the Swashbuckle.AspNetCore version in the Gallery.Api.csproj file.

The package is published to NuGet by the "Build Client Package" workflow (.github/workflows/client.yml), run manually with the package version as its input. It runs the steps above in Release, pushes the package to nuget.org as unlisted, and creates a GitHub release tagged Gallery.Api.Client.<version>.
