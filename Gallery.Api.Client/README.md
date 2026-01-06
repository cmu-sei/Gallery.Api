# How to create the nuget package for Gallery.Api.Client

1. cd ../Gallery.Api
2. swagger tofile --output ../Gallery.Api.Client/swagger.json bin/Debug/net10.0/Gallery.Api.dll v1
3. cd ../Gallery.Api.Client
4. ./node_modules/.bin/nswag run /runtime:Net100
5. dotnet pack -c Release /p:version=0.1.2

*** NOTE: If dotnet sawgger is not recognized, in the Gallery.Api folder run the following:
    dotnet new tool-manifest
    dotnet tool install --version 10.1.0 Swashbuckle.AspNetCore.Cli

The version installed must match the version in Gallery.Api.csproj file.

Also, if nswag is not found, run npm install from Gallery.Api.Client folder
