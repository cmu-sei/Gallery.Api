// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gallery.Api.Data.Models
{
    public class TeamCardEntity : BaseEntity
    {
        public TeamCardEntity() {
            Move = 0;
            Inject = 0;
            IsShownOnWall = true;
            CanPostArticles = false;
        }

        public TeamCardEntity(Guid teamId, Guid cardId)
        {
            TeamId = teamId;
            CardId = cardId;
            Move = 0;
            Inject = 0;
            IsShownOnWall = true;
            CanPostArticles = false;
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }
        public int Move { get; set; }
        public int Inject { get; set; }
        public bool IsShownOnWall { get; set; }
        public bool CanPostArticles { get; set; }
        public Guid TeamId { get; set; }
        public TeamEntity Team { get; set; }
        public Guid CardId { get; set; }
        public CardEntity Card { get; set; }
    }

    public class TeamCardConfiguration : IEntityTypeConfiguration<TeamCardEntity>
    {
        public void Configure(EntityTypeBuilder<TeamCardEntity> builder)
        {
            builder.HasIndex(x => new { x.TeamId, x.CardId }).IsUnique();
        }
    }
}
