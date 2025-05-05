// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Gallery.Api.Data.Models;
using System.Collections.Generic;

namespace Gallery.Api.Infrastructure.Options
{
    public class SeedDataOptions
    {
        public List<PermissionEntity> Permissions { get; set; }
        public List<TeamEntity> Teams { get; set; }
        public List<TeamUserEntity> TeamUsers { get; set; }
        public List<UserEntity> Users { get; set; }
        public List<CollectionEntity> Collections { get; set; }
        public List<CardEntity> Cards { get; set; }
        public List<TeamCardEntity> TeamCards { get; set; }
        public List<ArticleEntity> Articles { get; set; }
        public List<TeamArticleEntity> TeamArticles { get; set; }
        public List<ExhibitEntity> Exhibits { get; set; }

    }
}
