// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gallery.Api.Data.Models
{
    public class TeamArticleEntity : BaseEntity
    {
        public TeamArticleEntity() {}

        public TeamArticleEntity(Guid exhibitId, Guid teamId, Guid articleId)
        {
            ExhibitId = exhibitId;
            TeamId = teamId;
            ArticleId = articleId;
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }
        public Guid ExhibitId { get; set; }
        public ExhibitEntity Exhibit { get; set; }
        public Guid TeamId { get; set; }
        public TeamEntity Team { get; set; }
        public Guid ArticleId { get; set; }
        public ArticleEntity Article { get; set; }
    }

}
