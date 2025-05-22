// Copyright 2024 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using AutoMapper;
using Gallery.Api.ViewModels;
using Gallery.Api.Data.Models;

namespace Gallery.Api.Infrastructure.Mapping
{
    public class ExhibitRoleProfile : Profile
    {
        public ExhibitRoleProfile()
        {
            CreateMap<ExhibitRoleEntity, ExhibitRole>();
        }
    }
}
