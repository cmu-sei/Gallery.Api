// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Gallery.Api.Data;
using Gallery.Api.Data.Models;
using Gallery.Api.Infrastructure.Authorization;
using Gallery.Api.Services;
using Gallery.Api.ViewModels;
using System.Security.Claims;

namespace Gallery.Api.Infrastructure.Mappings
{
    public class TeamCardProfile : AutoMapper.Profile
    {
        public TeamCardProfile()
        {
            CreateMap<TeamCardEntity, TeamCard>();

            CreateMap<TeamCard, TeamCardEntity>();
        }
    }
}


