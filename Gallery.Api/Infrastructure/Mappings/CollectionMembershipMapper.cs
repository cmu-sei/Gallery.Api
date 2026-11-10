// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Gallery.Api.Data.Models;
using Gallery.Api.ViewModels;
using Riok.Mapperly.Abstractions;

namespace Gallery.Api.Infrastructure.Mappings;

/// <summary>Collection memberships' mappings.</summary>
[Mapper]
public static partial class CollectionMembershipMapper
{
    public static partial CollectionMembership ToCollectionMembership(this CollectionMembershipEntity source);

    /// <remarks>A body without a role writes the empty id, which the column's default replaces with the Member role.</remarks>
    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public static partial CollectionMembershipEntity ToEntity(this CollectionMembership source);
}
