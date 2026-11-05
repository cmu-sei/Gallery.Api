// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Gallery.Api.Data.Models;
using Gallery.Api.ViewModels;
using Riok.Mapperly.Abstractions;

namespace Gallery.Api.Infrastructure.Mappings;

/// <summary>Exhibit memberships' mappings.</summary>
[Mapper]
public static partial class ExhibitMembershipMapper
{
    public static partial ExhibitMembership ToExhibitMembership(this ExhibitMembershipEntity source);

    /// <remarks>A body without a role writes the empty id, which the column's default replaces with the Member role.</remarks>
    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public static partial ExhibitMembershipEntity ToEntity(this ExhibitMembership source);
}
