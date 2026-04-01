# Adapted from https://github.com/dotnet/dotnet-docker/blob/main/samples/aspnetapp/Dockerfile.chiseled

# Build stage
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG TARGETARCH
ARG VERSION
WORKDIR /source

# Copy project files and restore as distinct layers
COPY --link Gallery.Api/*.csproj ./Gallery.Api/
COPY --link Gallery.Api.Data/*.csproj ./Gallery.Api.Data/
COPY --link Gallery.Api.Migrations.PostgreSQL/*.csproj ./Gallery.Api.Migrations.PostgreSQL/
WORKDIR /source/Gallery.Api
RUN dotnet restore -a $TARGETARCH

# Copy source code and publish app
WORKDIR /source
COPY --link . .
WORKDIR /source/Gallery.Api
RUN dotnet publish -a $TARGETARCH --no-restore -o /app /p:Version=${VERSION:-1.0.0} /p:AssemblyVersion=${VERSION:-1.0.0}

# Debug Stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS debug
ENV DOTNET_HOSTBUILDER__RELOADCONFIGCHANGE=false
EXPOSE 8080
WORKDIR /app
COPY --link --from=build /app .
USER $APP_UID
ENTRYPOINT ["./Gallery.Api"]

# Production stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS prod
ARG commit
ENV COMMIT=$commit
ENV DOTNET_HOSTBUILDER__RELOADCONFIGCHANGE=false
EXPOSE 8080
WORKDIR /app
COPY --link --from=build /app .
ENTRYPOINT ["./Gallery.Api"]
