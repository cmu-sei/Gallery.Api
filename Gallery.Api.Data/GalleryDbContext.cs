// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure.Internal;
using Gallery.Api.Data.Models;
using Gallery.Api.Data.Extensions;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure.Internal;

namespace Gallery.Api.Data
{
    public class GalleryDbContext : DbContext
    {
        private DbContextOptions<GalleryDbContext> _options;

        public GalleryDbContext(DbContextOptions<GalleryDbContext> options) : base(options) {
            _options = options;
        }

        public DbSet<UserEntity> Users { get; set; }
        public DbSet<PermissionEntity> Permissions { get; set; }
        public DbSet<UserPermissionEntity> UserPermissions { get; set; }
        public DbSet<ArticleEntity> Articles { get; set; }
        public DbSet<ArticleCardEntity> ArticleCards { get; set; }
        public DbSet<ArticleTagEntity> ArticleTags { get; set; }
        public DbSet<CardEntity> Cards { get; set; }
        public DbSet<CollectionEntity> Collections { get; set; }
        public DbSet<ExhibitEntity> Exhibits { get; set; }
        public DbSet<TagEntity> Tags { get; set; }
        public DbSet<TeamEntity> Teams { get; set; }
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

