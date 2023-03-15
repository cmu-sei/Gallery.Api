// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gallery.Api.Data.Models
{
    public class TeamEntity : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string ShortName { get; set; }
        public Guid? ExhibitId { get; set; }
        public virtual ExhibitEntity Exhibit { get; set; }
        public ICollection<TeamUserEntity> TeamUsers { get; set; } = new List<TeamUserEntity>();
        public ICollection<TeamArticleEntity> TeamArticles { get; set; } = new List<TeamArticleEntity>();
    }

    public class TeamConfiguration : IEntityTypeConfiguration<TeamEntity>
    {
        public void Configure(EntityTypeBuilder<TeamEntity> builder)
        {
            builder
                .HasIndex(e => e.Id).IsUnique();
            builder
                .HasOne(d => d.Exhibit)
                .WithMany(d => d.Teams)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

}
