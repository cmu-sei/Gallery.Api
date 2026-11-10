// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Gallery.Api.Data.Enumerations;
using Gallery.Api.Data.Models;
using Gallery.Api.ViewModels;
using Riok.Mapperly.Abstractions;

namespace Gallery.Api.Infrastructure.Mappings;

/// <summary>
/// System roles' mappings. Missing permissions are an empty list in both directions, never null. The
/// view model has no description, so an update keeps the stored one.
/// </summary>
[Mapper]
public static partial class SystemRoleMapper
{
    public static partial SystemRole ToSystemRole(this SystemRoleEntity source);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public static partial SystemRoleEntity ToEntity(this SystemRole source);

    /// <remarks>
    /// Writes the body's id onto the row as well. EF Core refuses to change the key, so a body id other
    /// than the route's (or none) fails the save.
    /// </remarks>
    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public static partial void ApplyTo(this SystemRole source, SystemRoleEntity target);

    private static SystemPermission[] MapPermissions(List<SystemPermission> source) =>
        source == null ? [] : source.ToArray();

    private static List<SystemPermission> MapPermissions(SystemPermission[] source) =>
        source == null ? [] : source.ToList();
}
