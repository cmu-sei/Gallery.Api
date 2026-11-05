// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Gallery.Api.Data.Models;
using Gallery.Api.ViewModels;
using Riok.Mapperly.Abstractions;

namespace Gallery.Api.Infrastructure.Mappings;

/// <summary>Exhibit teams' mappings. The team and exhibit are mapped when the context has loaded them.</summary>
[Mapper]
[UseStaticMapper(typeof(TeamMapper))]
[UseStaticMapper(typeof(ExhibitMapper))]
public static partial class ExhibitTeamMapper
{
    [MapProperty(nameof(ExhibitTeamEntity.Team), nameof(ExhibitTeam.Team), Use = nameof(@TeamMapper.ToTeam))]
    public static partial ExhibitTeam ToExhibitTeam(this ExhibitTeamEntity source);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public static partial ExhibitTeamEntity ToEntity(this ExhibitTeamCreate source);
}
