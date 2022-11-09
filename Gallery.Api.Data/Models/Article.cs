// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Gallery.Api.Data.Enumerations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gallery.Api.Data.Models
{
    public class ArticleEntity : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public Guid CollectionId { get; set; }
        public CollectionEntity Collection { get; set; }
        public Guid? CardId { get; set; }
        public CardEntity Card { get; set; }
        public int Move { get; set; }
        public int Inject { get; set; }
        public ItemStatus Status  { get; set; }
        public SourceType SourceType  { get; set; }
        public string SourceName { get; set; }
        public string Url { get; set; }
        public DateTime DatePosted { get; set; }
        public bool OpenInNewTab { get; set; }
        public ICollection<ArticleCardEntity> ArticleCards { get; set; } = new List<ArticleCardEntity>();

    }
}
