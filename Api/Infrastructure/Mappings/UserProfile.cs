// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using AutoMapper;
using Api.Data.Models;
using Api.ViewModels;
using System.Linq;

namespace Api.Infrastructure.Mappings
{
    public class UserProfile : AutoMapper.Profile
    {
        public UserProfile()
        {
            CreateMap<UserEntity, User>()
                .ForMember(m => m.Permissions, opt => opt.MapFrom(x => x.UserPermissions.Select(y => y.Permission)))
                .ForMember(m => m.Permissions, opt => opt.ExplicitExpansion());
            CreateMap<User, UserEntity>()
                .ForMember(m => m.UserPermissions, opt => opt.Ignore())
                .ForMember(m => m.TeamUsers, opt => opt.Ignore());
        }
    }
}


