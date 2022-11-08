// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gallery.Api.Data.Models
{
    public class ArticleCardEntity : BaseEntity
    {
        public ArticleCardEntity() {}

        public ArticleCardEntity(Guid cardId, Guid articleId)
        {
            CardId = cardId;
            ArticleId = articleId;
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }
        public Guid CardId { get; set; }
        public CardEntity Card { get; set; }
        public Guid ArticleId { get; set; }
        public ArticleEntity Article { get; set; }
    }

    public class ArticleCardConfiguration : IEntityTypeConfiguration<ArticleCardEntity>
    {
        public void Configure(EntityTypeBuilder<ArticleCardEntity> builder)
        {
            builder.HasIndex(x => new { x.CardId, x.ArticleId }).IsUnique();
        }
    }
}
