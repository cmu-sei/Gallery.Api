// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Data.Models
{
    public class UserArticleEntity : BaseEntity
    {
        public UserArticleEntity() {}

        public UserArticleEntity(Guid exhibitId, Guid userId, Guid articleId, bool isRead, DateTime actualPostTime)
        {
            UserId = userId;
            ArticleId = articleId;
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }
        public Guid ExhibitId { get; set; }
        public ExhibitEntity Exhibit { get; set; }
        public Guid UserId { get; set; }
        public UserEntity User { get; set; }
        public Guid ArticleId { get; set; }
        public ArticleEntity Article { get; set; }
        public DateTime ActualDatePosted { get; set; }
        public bool IsRead { get; set; }
    }

    public class UserArticleConfiguration : IEntityTypeConfiguration<UserArticleEntity>
    {
        public void Configure(EntityTypeBuilder<UserArticleEntity> builder)
        {
            builder.HasIndex(x => new { x.ExhibitId, x.UserId, x.ArticleId }).IsUnique();
        }
    }
}
