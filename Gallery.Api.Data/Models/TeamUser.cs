// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gallery.Api.Data.Models
{
    public class TeamUserEntity
    {
        public TeamUserEntity() { }

        public TeamUserEntity(Guid userId, Guid teamId)
        {
            UserId = userId;
            TeamId = teamId;
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        public Guid UserId { get; set; }
        public UserEntity User { get; set; }

        public Guid TeamId { get; set; }
        public TeamEntity Team { get; set; }
    }

    public class TeamUserConfiguration : IEntityTypeConfiguration<TeamUserEntity>
    {
        public void Configure(EntityTypeBuilder<TeamUserEntity> builder)
        {
            builder.HasIndex(x => new { x.UserId, x.TeamId }).IsUnique();

            builder
                .HasOne(u => u.User)
                .WithMany(p => p.TeamUsers)
                .HasForeignKey(x => x.UserId);
            builder
                .HasOne(u => u.Team)
                .WithMany(p => p.TeamUsers)
                .HasForeignKey(x => x.TeamId);
        }
    }
}

