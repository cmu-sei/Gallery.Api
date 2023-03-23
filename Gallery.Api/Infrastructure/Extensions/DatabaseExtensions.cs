// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Gallery.Api.Infrastructure.Options;
using Gallery.Api.Data;
using Gallery.Api.Data.Models;

namespace Gallery.Api.Infrastructure.Extensions
{
    public static class DatabaseExtensions
    {
        public static IWebHost InitializeDatabase(this IWebHost webHost)
        {
            using (var scope = webHost.Services.CreateScope())
            {
                var services = scope.ServiceProvider;

                try
                {
                    var databaseOptions = services.GetService<DatabaseOptions>();
                    var ctx = services.GetRequiredService<GalleryDbContext>();

                    if (ctx != null)
                    {
                        if (databaseOptions.DevModeRecreate)
                            ctx.Database.EnsureDeleted();

                        // Do not run migrations on Sqlite, only devModeRecreate allowed
                        if (!ctx.Database.IsSqlite())
                        {
                            ctx.Database.Migrate();
                        }

                        if (databaseOptions.DevModeRecreate)
                        {
                            ctx.Database.EnsureCreated();
                        }

                        IHostEnvironment env = services.GetService<IHostEnvironment>();
                        string seedFile = Path.Combine(
                            env.ContentRootPath,
                            databaseOptions.SeedFile
                        );
                        if (File.Exists(seedFile)) {
                            SeedDataOptions seedDataOptions = JsonSerializer.Deserialize<SeedDataOptions>(File.ReadAllText(seedFile));
                            ProcessSeedDataOptions(seedDataOptions, ctx);
                            MoveExhibitTeamsToIndividualTeams(ctx);
                        }
                    }

                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred while initializing the database.");

                    // exit on database connection error on startup so app can be restarted to try again
                    throw;
                }
            }

            return webHost;
        }

        private static void ProcessSeedDataOptions(SeedDataOptions options, GalleryDbContext context)
        {
            if (options.Permissions != null && options.Permissions.Any())
            {
                var dbPermissions = context.Permissions.ToList();

                foreach (PermissionEntity permission in options.Permissions)
                {
                    if (!dbPermissions.Where(x => x.Key == permission.Key && x.Value == permission.Value).Any())
                    {
                        context.Permissions.Add(permission);
                    }
                }
                context.SaveChanges();
            }
            if (options.Users != null && options.Users.Any())
            {
                var dbUsers = context.Users.ToList();

                foreach (UserEntity user in options.Users)
                {
                    if (!dbUsers.Where(x => x.Id == user.Id).Any())
                    {
                        context.Users.Add(user);
                    }
                }
                context.SaveChanges();
            }
            if (options.Teams != null && options.Teams.Any())
            {
                var dbTeams = context.Teams.ToList();

                foreach (TeamEntity team in options.Teams)
                {
                    if (!dbTeams.Where(x => x.Id == team.Id).Any())
                    {
                        context.Teams.Add(team);
                    }
                }
                context.SaveChanges();
            }
            if (options.TeamUsers != null && options.TeamUsers.Any())
            {
                var dbTeamUsers = context.TeamUsers.ToList();

                foreach (TeamUserEntity teamUser in options.TeamUsers)
                {
                    if (!dbTeamUsers.Where(x => x.UserId == teamUser.UserId && x.TeamId == teamUser.TeamId).Any())
                    {
                        context.TeamUsers.Add(teamUser);
                    }
                }
                context.SaveChanges();
            }
            if (options.UserPermissions != null && options.UserPermissions.Any())
            {
                var dbUserPermissions = context.UserPermissions.ToList();

                foreach (UserPermissionEntity userPermission in options.UserPermissions)
                {
                    if (!dbUserPermissions.Where(x => x.UserId == userPermission.UserId && x.PermissionId == userPermission.PermissionId).Any())
                    {
                        context.UserPermissions.Add(userPermission);
                    }
                }
                context.SaveChanges();
            }
            if (options.Collections != null && options.Collections.Any())
            {

                var dbCollections = context.Collections.ToList();

                foreach (CollectionEntity collections in options.Collections)
                {
                    if (!dbCollections.Where(x => x.Id == collections.Id).Any())
                    {
                        context.Collections.Add(collections);
                    }
                }
                context.SaveChanges();
            }
            if (options.Cards != null && options.Cards.Any())
            {
                var dbCards = context.Cards.ToList();

                foreach (CardEntity cards in options.Cards)
                {
                    if (!dbCards.Where(x => x.Id == cards.Id).Any())
                    {
                        context.Cards.Add(cards);
                    }
                }
                context.SaveChanges();
            }
            if (options.TeamCards != null && options.TeamCards.Any())
            {
                var dbTeamCards = context.TeamCards.ToList();

                foreach (TeamCardEntity teamCards in options.TeamCards)
                {
                    if (!dbTeamCards.Where(x => x.Id == teamCards.Id).Any())
                    {
                        context.TeamCards.Add(teamCards);
                    }
                }
                context.SaveChanges();
            }
            if (options.Articles != null && options.Articles.Any())
            {
                var dbArticles = context.Articles.ToList();

                foreach (ArticleEntity articles in options.Articles)
                {
                    if (!dbArticles.Where(x => x.Id == articles.Id).Any())
                    {
                        context.Articles.Add(articles);
                    }
                }
                context.SaveChanges();
            }
            if (options.Exhibits != null && options.Exhibits.Any())
            {
                var dbExhibits = context.Exhibits.ToList();

                foreach (ExhibitEntity exhibits in options.Exhibits)
                {
                    if (!dbExhibits.Where(x => x.Id == exhibits.Id).Any())
                    {
                        context.Exhibits.Add(exhibits);
                    }
                }
                context.SaveChanges();
            }

        }

        private static void MoveExhibitTeamsToIndividualTeams(GalleryDbContext context)
        {
            // this ONLY gets run ONCE!
            // check to see if this has already been done
            var isAlreadyDone = context.Teams.Any(t => t.ExhibitId != null);
            if (isAlreadyDone) return;

            var exhibitTeams = context.ExhibitTeams.ToList();
            // create unique teams for each exhibit team record
            foreach (var et in exhibitTeams)
            {
                var newTeamId = Guid.NewGuid();
                var newTeam = new TeamEntity()
                {
                    Id = newTeamId,
                    Name = et.Team.Name,
                    ShortName = et.Team.ShortName,
                    ExhibitId = et.ExhibitId,
                    DateCreated = et.DateCreated,
                    DateModified = et.DateModified,
                    CreatedBy = et.CreatedBy,
                    ModifiedBy = et.ModifiedBy
                };
                context.Teams.Add(newTeam);
                context.SaveChanges();
                // create team users for the new team
                foreach (var tu in et.Team.TeamUsers)
                {
                    var newTeamUser = new TeamUserEntity()
                    {
                        Id = Guid.NewGuid(),
                        TeamId = newTeamId,
                        UserId = tu.UserId
                    };
                    context.TeamUsers.Add(newTeamUser);
                }
                // substitute the new team ID in place of the old team ID
                // replace in TeamArticles, because TeamArticles are already by Exhibit
                var teamArticles = context.TeamArticles.Where(a => a.ExhibitId == et.ExhibitId && a.TeamId == et.TeamId);
                foreach (var action in teamArticles)
                {
                    action.TeamId = newTeamId;
                }
                // add new TeamCards for the newly created teams, because TeamCards were not already by Exhibit
                var collectionId = et.Exhibit.CollectionId;
                var teamCards = context.TeamCards.Where(a => a.Card.CollectionId == collectionId && a.TeamId == et.TeamId);
                foreach (var teamCard in teamCards)
                {
                    var newTeamCard = new TeamCardEntity()
                    {
                        Id = Guid.NewGuid(),
                        Move = teamCard.Move,
                        Inject = teamCard.Inject,
                        IsShownOnWall = teamCard.IsShownOnWall,
                        TeamId = newTeamId,
                        CardId = teamCard.CardId
                    };
                    context.TeamCards.Add(newTeamCard);
                }
                context.SaveChanges();
            }
        }

        private static string DbProvider(IConfiguration config)
        {
            return config.GetValue<string>("Database:Provider", "Sqlite").Trim();
        }

        public static DbContextOptionsBuilder UseConfiguredDatabase(
            this DbContextOptionsBuilder builder,
            IConfiguration config
        )
        {
            string dbProvider = DbProvider(config);
            var migrationsAssembly = String.Format("{0}.Migrations.{1}", typeof(Startup).GetTypeInfo().Assembly.GetName().Name, dbProvider);
            var connectionString = config.GetConnectionString(dbProvider);

            switch (dbProvider)
            {
                case "Sqlite":
                    builder.UseSqlite(connectionString, options => options.MigrationsAssembly(migrationsAssembly));
                    break;

                case "PostgreSQL":
                    builder.UseNpgsql(connectionString, options => options.MigrationsAssembly(migrationsAssembly));
                    break;

            }
            return builder;
        }
    }
}
