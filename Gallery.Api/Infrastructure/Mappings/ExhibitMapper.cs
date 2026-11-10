// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Gallery.Api.Data.Models;
using Gallery.Api.ViewModels;
using Riok.Mapperly.Abstractions;

namespace Gallery.Api.Infrastructure.Mappings;

/// <summary>Exhibits' mappings.</summary>
/// <remarks><see cref="Exhibit.ExhibitPermissions"/> describes the caller; the controller fills it.</remarks>
[Mapper]
public static partial class ExhibitMapper
{
    [MapperIgnoreTarget(nameof(Exhibit.ExhibitPermissions))]
    public static partial Exhibit ToExhibit(this ExhibitEntity source);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public static partial ExhibitEntity ToEntity(this ExhibitCreate source);

    /// <remarks>Writes every member of the edit, so a value it leaves out (a scenario, say) is cleared.</remarks>
    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public static partial void ApplyTo(this ExhibitEdit source, ExhibitEntity target);
}
