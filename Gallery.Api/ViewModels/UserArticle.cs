// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using Gallery.Api.Data.Enumerations;

namespace Gallery.Api.ViewModels
{
    public class UserArticle : Base
    {
        public Guid Id { get; set; }
        public Guid ExhibitId { get; set; }
        public Guid UserId { get; set; }
        public Guid ArticleId { get; set; }
        public Article Article { get; set; }
        public DateTime ActualDatePosted { get; set; }
        public bool IsRead { get; set; }
   }

    /// <summary>
    /// A UserArticle create request. It names the Article by Id only, so it cannot create or change one.
    /// </summary>
    public class UserArticleCreate
    {
        /// <summary>The Id to create the UserArticle with. A new one is made when it is empty.</summary>
        public Guid Id { get; set; }
        public Guid ExhibitId { get; set; }
        public Guid UserId { get; set; }
        public Guid ArticleId { get; set; }
        public DateTime ActualDatePosted { get; set; }
        public bool IsRead { get; set; }
    }
}
