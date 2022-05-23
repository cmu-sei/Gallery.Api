// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gallery.Api.Data.Models
{
    public class ExhibitTeamEntity : BaseEntity
    {
        public ExhibitTeamEntity() {}

        public ExhibitTeamEntity(Guid exhibitId, Guid teamId)
        {
            ExhibitId = exhibitId;
            TeamId = teamId;
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }
        public Guid ExhibitId { get; set; }
        public ExhibitEntity Exhibit { get; set; }
        public Guid TeamId { get; set; }
        public TeamEntity Team { get; set; }
    }

    public class ExhibitTeamConfiguration : IEntityTypeConfiguration<ExhibitTeamEntity>
    {
        public void Configure(EntityTypeBuilder<ExhibitTeamEntity> builder)
        {
            builder.HasIndex(x => new { x.ExhibitId, x.TeamId }).IsUnique();
        }
    }
}
