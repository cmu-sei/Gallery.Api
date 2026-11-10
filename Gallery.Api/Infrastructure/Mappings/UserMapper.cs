// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Linq;
using Gallery.Api.Data.Models;
using Gallery.Api.ViewModels;
using Riok.Mapperly.Abstractions;

namespace Gallery.Api.Infrastructure.Mappings;

/// <summary>Users' mappings.</summary>
/// <remarks>
/// Nothing fills <see cref="User.Permissions"/>: it is null on every read, whether or not the caller may
/// see permissions.
/// </remarks>
[Mapper]
public static partial class UserMapper
{
    public static partial IQueryable<User> ProjectToUser(this IQueryable<UserEntity> query);

    [MapperIgnoreTarget(nameof(User.Permissions))]
    public static partial User ToUser(this UserEntity source);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    [MapperIgnoreSource(nameof(User.Permissions))]
    public static partial UserEntity ToEntity(this User source);

    /// <remarks>
    /// Writes every member of the body, the audit fields included, so a value it leaves out (an email, a
    /// role) is cleared. The service has already refused a body id other than the route's;
    /// <c>GalleryDbContext</c> restores the creation fields as it saves.
    /// </remarks>
    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    [MapperIgnoreSource(nameof(User.Permissions))]
    public static partial void ApplyTo(this User source, UserEntity target);
}
