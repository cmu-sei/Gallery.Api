// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Api.Data;
using Api.Data.Models;
using Api.Infrastructure.Authorization;
using Api.Services;
using Api.ViewModels;
using System.Security.Claims;

namespace Api.Infrastructure.Mappings
{
    public class TeamUserProfile : AutoMapper.Profile
    {
        public TeamUserProfile()
        {
            CreateMap<TeamUserEntity, TeamUser>();

            CreateMap<TeamUser, TeamUserEntity>();
        }
    }
}


