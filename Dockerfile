#
#multi-stage target: dev
#
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS dev

ENV ASPNETCORE_HTTP_PORTS=4302
ENV ASPNETCORE_ENVIRONMENT=DEVELOPMENT

COPY . /app
WORKDIR /app/Gallery.Api
RUN dotnet publish -c Release -o /app/dist
CMD ["dotnet", "run"]

#
#multi-stage target: prod
#
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS prod
ENV DOTNET_HOSTBUILDER__RELOADCONFIGCHANGE=false
COPY --from=dev /app/dist /app

WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=80
EXPOSE 80

CMD [ "dotnet", "Gallery.Api.dll" ]
