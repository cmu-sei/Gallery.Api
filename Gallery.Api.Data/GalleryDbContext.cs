// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using Microsoft.EntityFrameworkCore;
using Gallery.Api.Data.Models;
using Gallery.Api.Data.Extensions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
        public DbSet<SystemRoleEntity> SystemRoles { get; set; }
        public DbSet<ExhibitRoleEntity> ExhibitRoles { get; set; }
        public DbSet<ExhibitMembershipEntity> ExhibitMemberships { get; set; }
        public DbSet<CollectionRoleEntity> CollectionRoles { get; set; }
        public DbSet<CollectionMembershipEntity> CollectionMemberships { get; set; }
        public DbSet<GroupEntity> Groups { get; set; }
        public DbSet<GroupMembershipEntity> GroupMemberships { get; set; }

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
        public override async Task<int> SaveChangesAsync(CancellationToken ct = default(CancellationToken))
        {
            var addedEntries = ChangeTracker.Entries().Where(x => x.State == EntityState.Added);
            var modifiedEntries = ChangeTracker.Entries().Where(x => x.State == EntityState.Modified);
            foreach (var entry in addedEntries)
            {
                // add info to entities that are base entities
                try
                {
                    ((BaseEntity)entry.Entity).DateCreated = DateTime.UtcNow;
                    ((BaseEntity)entry.Entity).DateModified = null;
                    ((BaseEntity)entry.Entity).ModifiedBy = null;
                }
                catch
                { }
            }
            foreach (var entry in modifiedEntries)
            {
                // add info to entities that are base entities
                try
                {
                    ((BaseEntity)entry.Entity).DateModified = DateTime.UtcNow;
                    ((BaseEntity)entry.Entity).CreatedBy = (Guid)entry.OriginalValues["CreatedBy"];
                    ((BaseEntity)entry.Entity).DateCreated = DateTime.SpecifyKind((DateTime)entry.OriginalValues["DateCreated"], DateTimeKind.Utc);
                }
                catch
                { }
            }
            return await base.SaveChangesAsync(ct);
        }
    }
}
