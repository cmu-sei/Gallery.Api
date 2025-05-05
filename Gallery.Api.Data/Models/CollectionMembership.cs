// Copyright 2024 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gallery.Api.Data.Models;

public class CollectionMembershipEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    public Guid CollectionId { get; set; }
    public virtual CollectionEntity Collection { get; set; }

    public Guid? UserId { get; set; }
    public virtual UserEntity User { get; set; }

    public Guid? GroupId { get; set; }
    public virtual GroupEntity Group { get; set; }

    public Guid RoleId { get; set; } = CollectionRoleEntityDefaults.CollectionMemberRoleId;
    public CollectionRoleEntity Role { get; set; }


    public CollectionMembershipEntity() { }

    public CollectionMembershipEntity(Guid collectionId, Guid? userId, Guid? groupId)
    {
        CollectionId = collectionId;
        UserId = userId;
        GroupId = groupId;
    }

    public class CollectionMembershipConfiguration : IEntityTypeConfiguration<CollectionMembershipEntity>
    {
        public void Configure(EntityTypeBuilder<CollectionMembershipEntity> builder)
        {
            builder.HasIndex(e => new { e.CollectionId, e.UserId, e.GroupId }).IsUnique();

            builder.Property(x => x.RoleId).HasDefaultValue(CollectionRoleEntityDefaults.CollectionMemberRoleId);

            builder
                .HasOne(x => x.Collection)
                .WithMany(x => x.Memberships)
                .HasForeignKey(x => x.CollectionId);

            builder
                .HasOne(x => x.User)
                .WithMany(x => x.CollectionMemberships)
                .HasForeignKey(x => x.UserId)
                .HasPrincipalKey(x => x.Id);

            builder
                .HasOne(x => x.Group)
                .WithMany(x => x.CollectionMemberships)
                .HasForeignKey(x => x.GroupId)
                .HasPrincipalKey(x => x.Id);
        }
    }
}
