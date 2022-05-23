// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Api.Data.Models;
using Api.ViewModels;

namespace Api.Infrastructure.Mappings
{
    public class ExhibitTeamProfile : AutoMapper.Profile
    {
        public ExhibitTeamProfile()
        {
            CreateMap<ExhibitTeamEntity, ExhibitTeam>();

            CreateMap<ExhibitTeam, ExhibitTeamEntity>();

            CreateMap<ExhibitTeamEntity, ExhibitTeamEntity>()
                .ForMember(e => e.Id, opt => opt.Ignore());

        }
    }
}


