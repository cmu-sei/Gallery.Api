// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Gallery.Api.Data.Models;
using Gallery.Api.ViewModels;
using Riok.Mapperly.Abstractions;

namespace Gallery.Api.Infrastructure.Mappings;

/// <summary>Teams' mappings.</summary>
/// <remarks>
/// <see cref="Team.Users"/> is optional in projections: <see cref="ProjectToTeam"/> leaves it empty, and
/// <see cref="TeamWithUsersMapper.ProjectToTeamWithUsers"/> fills it. An in-memory map always fills it,
/// from whatever team users the context has loaded. <see cref="Team.Articles"/> is never filled.
/// </remarks>
[Mapper]
public static partial class TeamMapper
{
    /// <remarks>Without users: the list is empty.</remarks>
    public static partial IQueryable<Team> ProjectToTeam(this IQueryable<TeamEntity> query);

    // The projection's element mapping.
    [UserMapping(Default = true)]
    [MapperIgnoreTarget(nameof(Team.Users))]
    [MapperIgnoreTarget(nameof(Team.Articles))]
    private static partial Team MapToTeam(TeamEntity source);

    [UserMapping(Default = false)]
    [MapProperty(nameof(TeamEntity.TeamUsers), nameof(Team.Users), Use = nameof(MapUsers))]
    [MapperIgnoreTarget(nameof(Team.Articles))]
    public static partial Team ToTeam(this TeamEntity source);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public static partial TeamEntity ToEntity(this TeamCreate source);

    /// <remarks>Writes every member of the edit, so a short name or email it leaves out is cleared.</remarks>
    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public static partial void ApplyTo(this TeamEdit source, TeamEntity target);

    [IncludeMappingConfiguration(nameof(@ExhibitMapper.ToExhibit))]
    private static partial Exhibit MapToExhibit(ExhibitEntity source);

    [IncludeMappingConfiguration(nameof(@UserMapper.ToUser))]
    private static partial User MapToUser(UserEntity source);

    // AutoMapper turned a missing list into an empty one.
    [UserMapping(Default = false)]
    private static ICollection<User> MapUsers(ICollection<TeamUserEntity> teamUsers) =>
        teamUsers == null ? [] : teamUsers.Select(x => MapToUser(x.User)).ToList();
}

/// <summary>The projection of a team with its users.</summary>
/// <remarks>
/// A class of its own because a projection takes the default mapping of its element types, and a
/// mapper has one default per type pair.
/// </remarks>
[Mapper]
public static partial class TeamWithUsersMapper
{
    /// <remarks>With users, read through the team's team users.</remarks>
    public static partial IQueryable<Team> ProjectToTeamWithUsers(this IQueryable<TeamEntity> query);

    [MapProperty(nameof(TeamEntity.TeamUsers), nameof(Team.Users), Use = nameof(MapUsers))]
    [MapperIgnoreTarget(nameof(Team.Articles))]
    private static partial Team MapToTeam(TeamEntity source);

    [IncludeMappingConfiguration(nameof(@ExhibitMapper.ToExhibit))]
    private static partial Exhibit MapToExhibit(ExhibitEntity source);

    [IncludeMappingConfiguration(nameof(@UserMapper.ToUser))]
    private static partial User MapToUser(UserEntity source);

    [UserMapping(Default = false)]
    private static ICollection<User> MapUsers(ICollection<TeamUserEntity> teamUsers) =>
        teamUsers.Select(x => MapToUser(x.User)).ToList();
}
