// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Gallery.Api.Data.Models;
using Gallery.Api.ViewModels;

namespace Gallery.Api.Infrastructure.Mappings
{
    public class UserArticleProfile : AutoMapper.Profile
    {
        public UserArticleProfile()
        {
            CreateMap<UserArticleEntity, UserArticle>();

            CreateMap<UserArticle, UserArticleEntity>();

            CreateMap<UserArticleEntity, UserArticleEntity>()
                .ForMember(e => e.Id, opt => opt.Ignore());

        }
    }
}


