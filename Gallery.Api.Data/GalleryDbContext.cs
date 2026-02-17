// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using Microsoft.EntityFrameworkCore;
using Gallery.Api.Data.Models;
using Gallery.Api.Data.Extensions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Crucible.Common.EntityEvents;
using Crucible.Common.EntityEvents.Abstractions;

namespace Gallery.Api.Data
{
    [GenerateEntityEventInterfaces(typeof(INotification))]
    public class GalleryDbContext : EventPublishingDbContext
    {

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
        public DbSet<SystemRoleEntity> SystemRoles { get; set; }
        public DbSet<ExhibitRoleEntity> ExhibitRoles { get; set; }
        public DbSet<ExhibitMembershipEntity> ExhibitMemberships { get; set; }
        public DbSet<CollectionRoleEntity> CollectionRoles { get; set; }
        public DbSet<CollectionMembershipEntity> CollectionMemberships { get; set; }
        public DbSet<GroupEntity> Groups { get; set; }
        public DbSet<GroupMembershipEntity> GroupMemberships { get; set; }
        public DbSet<XApiQueuedStatementEntity> XApiQueuedStatements { get; set; }

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

        public override int SaveChanges()
        {
            UpdateBaseEntityFields();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken ct = default(CancellationToken))
        {
            UpdateBaseEntityFields();
            return await base.SaveChangesAsync(ct);
        }

        private void UpdateBaseEntityFields()
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
        }

        protected override async Task PublishEventsAsync(CancellationToken cancellationToken)
        {
            if (EntityEvents.Count > 0 && ServiceProvider is not null)
            {
                var mediator = ServiceProvider.GetRequiredService<IMediator>();
                foreach (var evt in EntityEvents.Cast<INotification>())
                {
                    await mediator.Publish(evt, cancellationToken);
                }
            }
        }
    }
}
