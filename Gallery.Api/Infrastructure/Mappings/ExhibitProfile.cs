// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Gallery.Api.Data.Models;
using Gallery.Api.ViewModels;

namespace Gallery.Api.Infrastructure.Mappings
{
    public class ExhibitProfile : AutoMapper.Profile
    {
        public ExhibitProfile()
        {
            CreateMap<ExhibitEntity, Exhibit>();

            CreateMap<Exhibit, ExhibitEntity>();

            CreateMap<ExhibitEntity, ExhibitEntity>()
                .ForMember(e => e.Id, opt => opt.Ignore());

        }
    }
}


