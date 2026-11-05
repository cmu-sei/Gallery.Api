// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Gallery.Api.Data.Models;
using Gallery.Api.ViewModels;
using Riok.Mapperly.Abstractions;

namespace Gallery.Api.Infrastructure.Mappings;

/// <summary>Collections' mappings.</summary>
/// <remarks><see cref="Collection.CollectionPermissions"/> describes the caller; the controller fills it.</remarks>
[Mapper]
public static partial class CollectionMapper
{
    [MapperIgnoreTarget(nameof(Collection.CollectionPermissions))]
    public static partial Collection ToCollection(this CollectionEntity source);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    [MapperIgnoreSource(nameof(Collection.CollectionPermissions))]
    public static partial CollectionEntity ToEntity(this Collection source);

    /// <remarks>
    /// Writes every member of the body, the id and audit fields included, so a value it leaves out is
    /// cleared. EF Core refuses to change the key, so a body id other than the route's (or none) fails
    /// the save; <c>GalleryDbContext</c> restores the creation fields as it saves.
    /// </remarks>
    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    [MapperIgnoreSource(nameof(Collection.CollectionPermissions))]
    public static partial void ApplyTo(this Collection source, CollectionEntity target);
}
