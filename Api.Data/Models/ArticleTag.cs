// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Data.Models
{
    public class ArticleTagEntity : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }
        public int Move { get; set; }
        public int Inject { get; set; }
        public Guid ArticleId { get; set; }
        public ArticleEntity Article { get; set; }
        public Guid TagId { get; set; }
        public TagEntity Tag { get; set; }
    }
}
