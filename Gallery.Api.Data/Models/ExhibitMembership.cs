// Copyright 2024 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gallery.Api.Data.Models;

public class ExhibitMembershipEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    public Guid ExhibitId { get; set; }
    public virtual ExhibitEntity Exhibit { get; set; }

    public Guid? UserId { get; set; }
    public virtual UserEntity User { get; set; }

    public Guid? GroupId { get; set; }
    public virtual GroupEntity Group { get; set; }

    public Guid RoleId { get; set; } = ExhibitRoleDefaults.ExhibitMemberRoleId;
    public ExhibitRoleEntity Role { get; set; }


    public ExhibitMembershipEntity() { }

    public ExhibitMembershipEntity(Guid exhibitId, Guid? userId, Guid? groupId)
    {
        ExhibitId = exhibitId;
        UserId = userId;
        GroupId = groupId;
    }

    public class ExhibitMembershipEntityConfiguration : IEntityTypeConfiguration<ExhibitMembershipEntity>
    {
        public void Configure(EntityTypeBuilder<ExhibitMembershipEntity> builder)
        {
            builder.HasIndex(e => new { e.ExhibitId, e.UserId, e.GroupId }).IsUnique();

            builder.Property(x => x.RoleId).HasDefaultValue(ExhibitRoleDefaults.ExhibitMemberRoleId);

            builder
                .HasOne(x => x.Exhibit)
                .WithMany(x => x.Memberships)
                .HasForeignKey(x => x.ExhibitId);

            builder
                .HasOne(x => x.User)
                .WithMany(x => x.ExhibitMemberships)
                .HasForeignKey(x => x.UserId)
                .HasPrincipalKey(x => x.Id);

            builder
                .HasOne(x => x.Group)
                .WithMany(x => x.ExhibitMemberships)
                .HasForeignKey(x => x.GroupId)
                .HasPrincipalKey(x => x.Id);
        }
    }
}
