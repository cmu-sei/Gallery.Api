// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using Microsoft.EntityFrameworkCore;
using Gallery.Api.Data.Models;
using Gallery.Api.Data.Extensions;

namespace Gallery.Api.Data
{
    public class GalleryDbContext : DbContext
    {
        // Needed for EventInterceptor
        public IServiceProvider ServiceProvider;

        public GalleryDbContext(DbContextOptions<GalleryDbContext> options)
            : base(options) { }

        public DbSet<UserEntity> Users { get; set; }
        public DbSet<PermissionEntity> Permissions { get; set; }
        public DbSet<UserPermissionEntity> UserPermissions { get; set; }
        public DbSet<ArticleEntity> Articles { get; set; }
        public DbSet<CardEntity> Cards { get; set; }
        public DbSet<CollectionEntity> Collections { get; set; }
        public DbSet<ExhibitEntity> Exhibits { get; set; }
        public DbSet<TeamEntity> Teams { get; set; }
        public DbSet<TeamArticleEntity> TeamArticles { get; set; }
        public DbSet<TeamCardEntity> TeamCards { get; set; }
        public DbSet<TeamUserEntity> TeamUsers { get; set; }
        public DbSet<ExhibitTeamEntity> ExhibitTeams { get; set; }
        public DbSet<UserArticleEntity> UserArticles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurations();

            // Apply PostgreSQL specific options
            if (Database.IsNpgsql())
            {
                modelBuilder.AddPostgresUUIDGeneration();
                modelBuilder.UsePostgresCasing();
            }

        }
    }
}
