// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Gallery.Api.Data.Enumerations;
using Gallery.Api.Data.Models;
using Gallery.Api.ViewModels;
using Riok.Mapperly.Abstractions;

namespace Gallery.Api.Infrastructure.Mappings;

/// <summary>Exhibit roles' mappings. Missing permissions are an empty list, never null.</summary>
[Mapper]
public static partial class ExhibitRoleMapper
{
    public static partial ExhibitRole ToExhibitRole(this ExhibitRoleEntity source);

    private static ExhibitPermission[] MapPermissions(List<ExhibitPermission> source) =>
        source == null ? [] : source.ToArray();
}
