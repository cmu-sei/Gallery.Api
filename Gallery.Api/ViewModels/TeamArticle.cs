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
}

