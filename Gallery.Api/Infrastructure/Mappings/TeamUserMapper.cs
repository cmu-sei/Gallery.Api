// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Gallery.Api.Data.Models;
using Gallery.Api.ViewModels;
using Riok.Mapperly.Abstractions;

namespace Gallery.Api.Infrastructure.Mappings;

/// <summary>Team users' mappings.</summary>
/// <remarks>
/// The user and team are mapped when the context has loaded them, and the team with its users. A team
/// user has no audit fields of its own, so the view model's are left at their defaults.
/// </remarks>
[Mapper]
[UseStaticMapper(typeof(UserMapper))]
[UseStaticMapper(typeof(TeamMapper))]
public static partial class TeamUserMapper
{
    [MapperIgnoreTarget(nameof(TeamUser.DateCreated))]
    [MapperIgnoreTarget(nameof(TeamUser.DateModified))]
    [MapperIgnoreTarget(nameof(TeamUser.CreatedBy))]
    [MapperIgnoreTarget(nameof(TeamUser.ModifiedBy))]
    [MapProperty(nameof(TeamUserEntity.Team), nameof(TeamUser.Team), Use = nameof(@TeamMapper.ToTeam))]
    public static partial TeamUser ToTeamUser(this TeamUserEntity source);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public static partial TeamUserEntity ToEntity(this TeamUserCreate source);
}
