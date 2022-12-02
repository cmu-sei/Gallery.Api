# How to create the nuget package for Gallery.Api.Client
1. cd ../Gallery.Api
2. swagger tofile --output ../Gallery.Api.Client/swagger.json bin/Debug/net6.0/Gallery.Api.dll v1
3. cd ../Gallery.Api.Client
4. ./node_modules/.bin/nswag run /runtime:Net60
5. dotnet pack -c Release /p:version=0.1.2

