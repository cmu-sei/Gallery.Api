// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Gallery.Api.Data.Models;
using Gallery.Api.ViewModels;

namespace Gallery.Api.Infrastructure.Mappings
{
    public class ArticleProfile : AutoMapper.Profile
    {
        public ArticleProfile()
        {
            CreateMap<ArticleEntity, Article>();

            CreateMap<Article, ArticleEntity>();

            CreateMap<ArticleEntity, ArticleEntity>()
                .ForMember(e => e.Id, opt => opt.Ignore());

        }
    }
}


