// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using Gallery.Api.Data.Enumerations;

namespace Gallery.Api.ViewModels
{
    public class TeamArticle : Base
    {
        public Guid Id { get; set; }
        public Guid ExhibitId { get; set; }
        public Guid TeamId { get; set; }
        public Guid ArticleId { get; set; }
   }

    /// <summary>A TeamArticle create request. It names the Exhibit, Team and Article by Id only.</summary>
    public class TeamArticleCreate
    {
        /// <summary>The Id to create the TeamArticle with. A new one is made when it is empty.</summary>
        public Guid Id { get; set; }
        public Guid ExhibitId { get; set; }
        public Guid TeamId { get; set; }
        public Guid ArticleId { get; set; }
    }
}
