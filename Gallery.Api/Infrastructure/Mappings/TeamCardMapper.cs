// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Gallery.Api.Data.Models;
using Gallery.Api.ViewModels;
using Riok.Mapperly.Abstractions;

namespace Gallery.Api.Infrastructure.Mappings;

/// <summary>Team cards' mappings.</summary>
[Mapper]
public static partial class TeamCardMapper
{
    public static partial TeamCard ToTeamCard(this TeamCardEntity source);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public static partial TeamCardEntity ToEntity(this TeamCardCreate source);

    /// <remarks>
    /// Writes every member of the edit, its team and card included; the service checks those before it
    /// maps. A flag or move the edit leaves out takes the edit's default.
    /// </remarks>
    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public static partial void ApplyTo(this TeamCardEdit source, TeamCardEntity target);
}
