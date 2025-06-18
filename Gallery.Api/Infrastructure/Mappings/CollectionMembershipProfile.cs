// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using AutoMapper;
using Gallery.Api.ViewModels;
using Gallery.Api.Data.Models;

namespace Gallery.Api.Infrastructure.Mapping
{
    public class CollectionMembershipProfile : Profile
    {
        public CollectionMembershipProfile()
        {
            CreateMap<CollectionMembershipEntity, CollectionMembership>();
            CreateMap<CollectionMembership, CollectionMembershipEntity>();
        }
    }
}
