// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

namespace Gallery.Api.Infrastructure.Mappings
{
    using Gallery.Api.Data.Models;
    using Gallery.Api.ViewModels;

    public class GroupProfile : AutoMapper.Profile
    {
        public GroupProfile()
        {
            CreateMap<GroupEntity, Group>();
            CreateMap<Group, GroupEntity>();
        }
    }
}
