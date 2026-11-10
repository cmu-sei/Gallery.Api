// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Gallery.Api.Data.Models;
using Gallery.Api.ViewModels;
using Riok.Mapperly.Abstractions;

namespace Gallery.Api.Infrastructure.Mappings;

/// <summary>Groups' mappings. The view model has no description, so an update keeps the stored one.</summary>
[Mapper]
public static partial class GroupMapper
{
    public static partial Group ToGroup(this GroupEntity source);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public static partial GroupEntity ToEntity(this Group source);

    /// <remarks>
    /// Writes the body's id onto the row as well. EF Core refuses to change the key, so a body id other
    /// than the route's (or none) fails the save.
    /// </remarks>
    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public static partial void ApplyTo(this Group source, GroupEntity target);
}
