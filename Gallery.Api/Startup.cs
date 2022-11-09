// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Gallery.Api.Infrastructure.EventHandlers;
using Gallery.Api.Infrastructure.Extensions;
using Gallery.Api.Data;
using Gallery.Api.Infrastructure.JsonConverters;
using Gallery.Api.Infrastructure.Mapping;
using Gallery.Api.Infrastructure.Options;
using Gallery.Api.Services;
using System;
using Gallery.Api.Infrastructure;
using Gallery.Api.Infrastructure.Authorization;
using Gallery.Api.Infrastructure.Filters;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Principal;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
namespace Gallery.Api
{
    public class Startup
    {
        public Infrastructure.Options.AuthorizationOptions _authOptions = new Infrastructure.Options.AuthorizationOptions();
        public Infrastructure.Options.VmTaskProcessingOptions _vmTaskProcessingOptions = new Infrastructure.Options.VmTaskProcessingOptions();
        public IConfiguration Configuration { get; }
        private const string _routePrefix = "api";

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            Configuration.GetSection("Authorization").Bind(_authOptions);
            Configuration.GetSection("VmTaskProcessing").Bind(_vmTaskProcessingOptions);
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add Azure Application Insights, if connection string is supplied
            string appInsights = Configuration["ApplicationInsights:ConnectionString"];
            if (!string.IsNullOrWhiteSpace(appInsights))
            {
                services.AddApplicationInsightsTelemetry();
            }

            var provider = Configuration["Database:Provider"];
            var connectionString = Configuration.GetConnectionString(provider);
            switch (provider)
            {
                case "InMemory":
                    services.AddDbContextPool<GalleryDbContext>((serviceProvider, optionsBuilder) => optionsBuilder
                        .AddInterceptors(serviceProvider.GetRequiredService<EntityTransactionInterceptor>())
                        .UseInMemoryDatabase("api"));
                    break;
                case "Sqlite":
                    services.AddDbContextPool<GalleryDbContext>((serviceProvider, optionsBuilder) => optionsBuilder
                        .AddInterceptors(serviceProvider.GetRequiredService<EntityTransactionInterceptor>())
                        .UseConfiguredDatabase(Configuration))
                        .AddHealthChecks().AddSqlite(connectionString, tags: new[] { "ready", "live"});
                    break;
                case "SqlServer":
                    services.AddDbContextPool<GalleryDbContext>((serviceProvider, optionsBuilder) => optionsBuilder
                        .AddInterceptors(serviceProvider.GetRequiredService<EntityTransactionInterceptor>())
                        .UseConfiguredDatabase(Configuration))
                        .AddHealthChecks().AddSqlServer(connectionString, tags: new[] { "ready", "live"});
                    break;
                case "PostgreSQL":
                    services.AddDbContextPool<GalleryDbContext>((serviceProvider, optionsBuilder) => optionsBuilder
                        .AddInterceptors(serviceProvider.GetRequiredService<EntityTransactionInterceptor>())
                        .UseConfiguredDatabase(Configuration))
                        .AddHealthChecks().AddNpgSql(connectionString, tags: new[] { "ready", "live"});
                    break;
            }

            services.AddOptions()
                .Configure<DatabaseOptions>(Configuration.GetSection("Database"))
                    .AddScoped(config => config.GetService<IOptionsMonitor<DatabaseOptions>>().CurrentValue)

                .Configure<ClaimsTransformationOptions>(Configuration.GetSection("ClaimsTransformation"))
                    .AddScoped(config => config.GetService<IOptionsMonitor<ClaimsTransformationOptions>>().CurrentValue)

                .Configure<SeedDataOptions>(Configuration.GetSection("SeedData"))
                    .AddScoped(config => config.GetService<IOptionsMonitor<SeedDataOptions>>().CurrentValue);
            services
                .Configure<ClientOptions>(Configuration.GetSection("ClientSettings"))
                .AddScoped(config => config.GetService<IOptionsMonitor<ClientOptions>>().CurrentValue);


            services.AddScoped<IClaimsTransformation, AuthorizationClaimsTransformer>();
            services.AddScoped<IUserClaimsService, UserClaimsService>();

            services.AddCors(options => options.UseConfiguredCors(Configuration.GetSection("CorsPolicy")));

            services.AddSignalR()
            .AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

            services.AddMvc(options =>
            {
                options.Filters.Add(typeof(ValidateModelStateFilter));
                options.Filters.Add(typeof(JsonExceptionFilter));

                // Require all scopes in authOptions
                var policyBuilder = new AuthorizationPolicyBuilder().RequireAuthenticatedUser();
                Array.ForEach(_authOptions.AuthorizationScope.Split(' '), x => policyBuilder.RequireScope(x));

                var policy = policyBuilder.Build();
                options.Filters.Add(new AuthorizeFilter(policy));
            })
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonNullableGuidConverter());
                options.JsonSerializerOptions.Converters.Add(new JsonIntegerConverter());
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

            services.AddSwagger(_authOptions);

            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = _authOptions.Authority;
                options.RequireHttpsMetadata = _authOptions.RequireHttpsMetadata;
                options.SaveToken = true;

                string[] validAudiences;
                if (_authOptions.ValidAudiences != null && _authOptions.ValidAudiences.Any())
                {
                    validAudiences = _authOptions.ValidAudiences;
                }
                else
                {
                    validAudiences = _authOptions.AuthorizationScope.Split(' ');
                }

                options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateAudience = _authOptions.ValidateAudience,
                    ValidAudiences = validAudiences
                };
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        // If the request is for our hub...
                        var path = context.HttpContext.Request.Path;
                        var accessToken = context.Request.Query["access_token"];

                        if (!string.IsNullOrEmpty(accessToken) &&
                            (path.StartsWithSegments("/hubs")))
                        {
                            // Read the token out of the query string
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    }
                };
            });

            services.AddRouting(options =>
            {
                options.LowercaseUrls = true;
            });

            services.AddMemoryCache();

            services.AddScoped<IArticleService, ArticleService>();
            services.AddScoped<IArticleCardService, ArticleCardService>();
            services.AddScoped<ICardService, CardService>();
            services.AddScoped<ICollectionService, CollectionService>();
            services.AddScoped<IExhibitService, ExhibitService>();
            services.AddScoped<IExhibitTeamService, ExhibitTeamService>();
            services.AddScoped<IPermissionService, PermissionService>();
            services.AddScoped<ISteamfitterService, SteamfitterService>();
            services.AddScoped<ITeamService, TeamService>();
            services.AddScoped<ITeamCardService, TeamCardService>();
            services.AddScoped<ITeamUserService, TeamUserService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IUserArticleService, UserArticleService>();
            services.AddScoped<IUserPermissionService, UserPermissionService>();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddScoped<IPrincipal>(p => p.GetService<IHttpContextAccessor>().HttpContext.User);
            services.AddHttpClient();

            ApplyPolicies(services);

            services.AddTransient<EntityTransactionInterceptor>();
            services.AddAutoMapper(cfg =>
            {
                cfg.ForAllPropertyMaps(
                    pm => pm.SourceType != null && Nullable.GetUnderlyingType(pm.SourceType) == pm.DestinationType,
                    (pm, c) => c.MapFrom<object, object, object, object>(new IgnoreNullSourceValues(), pm.SourceMember.Name));
            }, typeof(Startup));
            services.AddMediatR(typeof(Startup));
            services.Configure<VmTaskProcessingOptions>(Configuration.GetSection("VmTaskProcessing"));
            services
                .Configure<ResourceOwnerAuthorizationOptions>(Configuration.GetSection("ResourceOwnerAuthorization"))
                .AddScoped(config => config.GetService<IOptionsMonitor<ResourceOwnerAuthorizationOptions>>().CurrentValue);

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseRouting();
            app.UseCors("default");

            //move any querystring jwt to Auth bearer header
            app.Use(async (context, next) =>
            {
                if (string.IsNullOrWhiteSpace(context.Request.Headers["Authorization"])
                    && context.Request.QueryString.HasValue)
                {
                    string token = context.Request.QueryString.Value
                        .Substring(1)
                        .Split('&')
                        .SingleOrDefault(x => x.StartsWith("bearer="))?.Split('=')[1];

                    if (!String.IsNullOrWhiteSpace(token))
                        context.Request.Headers.Add("Authorization", new[] { $"Bearer {token}" });
                }

                await next.Invoke();

            });

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.RoutePrefix = _routePrefix;
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Gallery v1");
                c.OAuthClientId(_authOptions.ClientId);
                c.OAuthClientSecret(_authOptions.ClientSecret);
                c.OAuthAppName(_authOptions.ClientName);
                c.OAuthUsePkce();
            });

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                    endpoints.MapHealthChecks($"/{_routePrefix}/health/ready", new HealthCheckOptions()
                    {
                        Predicate = (check) => check.Tags.Contains("ready"),
                    });

                    endpoints.MapHealthChecks($"/{_routePrefix}/health/live", new HealthCheckOptions()
                    {
                        Predicate = (check) => check.Tags.Contains("live"),
                    });
                    endpoints.MapHub<Hubs.MainHub>("/hubs/main");
                    endpoints.MapHub<Hubs.CiteHub>("/hubs/cite");
                }
            );

            app.UseHttpContext();
        }


        private void ApplyPolicies(IServiceCollection services)
        {
            services.AddAuthorizationPolicy(_authOptions);
        }
    }
}
