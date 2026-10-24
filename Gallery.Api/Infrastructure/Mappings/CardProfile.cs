// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Gallery.Api.Data.Models;
using Gallery.Api.ViewModels;

namespace Gallery.Api.Infrastructure.Mappings
{
    public class CardProfile : AutoMapper.Profile
    {
        public CardProfile()
        {
            CreateMap<CardEntity, Card>();

            CreateMap<CardCreate, CardEntity>(AutoMapper.MemberList.Source);
            CreateMap<CardEdit, CardEntity>(AutoMapper.MemberList.Source);

            CreateMap<CardEntity, CardEntity>()
                .ForMember(e => e.Id, opt => opt.Ignore());

        }
    }
}


