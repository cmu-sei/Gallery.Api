// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using Gallery.Api.Data.Enumerations;

namespace Gallery.Api.ViewModels
{
    public class Article : Base
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Summary { get; set; }
        public string Description { get; set; }
        public Guid CollectionId { get; set; }
        public Guid? ExhibitId { get; set; }
        public Guid? CardId { get; set; }
        public int Move { get; set; }
        public int Inject { get; set; }
        public ItemStatus Status  { get; set; }
        public SourceType SourceType  { get; set; }
        public string SourceName { get; set; }
        public string Url { get; set; }
        public DateTime DatePosted { get; set; }
        public bool OpenInNewTab { get; set; }
        public ICollection<Team> Teams { get; set; } = new List<Team>();
   }

    /// <summary>
    /// The fields an Article's create and edit requests share. The Collection and Exhibit are not among
    /// them: only <see cref="ArticleCreate"/> names them, so an edit cannot move an Article.
    /// </summary>
    public abstract class ArticleFields
    {
        public string Name { get; set; }
        public string Summary { get; set; }
        public string Description { get; set; }
        /// <summary>A card of the Article's Collection, or null.</summary>
        public Guid? CardId { get; set; }
        public int Move { get; set; }
        public int Inject { get; set; }
        public ItemStatus Status { get; set; }
        public SourceType SourceType { get; set; }
        public string SourceName { get; set; }
        public string Url { get; set; }
        public DateTime DatePosted { get; set; }
        public bool OpenInNewTab { get; set; }
    }

    public class ArticleCreate : ArticleFields
    {
        /// <summary>The Id to create the Article with. A new one is made when it is empty.</summary>
        public Guid Id { get; set; }
        public Guid CollectionId { get; set; }
        /// <summary>Null for a template Article of the Collection.</summary>
        public Guid? ExhibitId { get; set; }
    }

    public class ArticleEdit : ArticleFields
    {
    }
}
