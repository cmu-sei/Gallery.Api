// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Linq;
using Gallery.Api.Data.Models;
using Gallery.Api.ViewModels;
using Riok.Mapperly.Abstractions;

namespace Gallery.Api.Infrastructure.Mappings;

/// <summary>Cards' mappings.</summary>
[Mapper]
public static partial class CardMapper
{
    public static partial IQueryable<Card> ProjectToCard(this IQueryable<CardEntity> query);

    public static partial Card ToCard(this CardEntity source);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public static partial CardEntity ToEntity(this CardCreate source);

    /// <remarks>Writes every member of the edit, so a value it leaves out is cleared.</remarks>
    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public static partial void ApplyTo(this CardEdit source, CardEntity target);
}
