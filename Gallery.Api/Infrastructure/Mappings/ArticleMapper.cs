// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Gallery.Api.Data.Models;
using Gallery.Api.ViewModels;
using Riok.Mapperly.Abstractions;

namespace Gallery.Api.Infrastructure.Mappings;

/// <summary>Articles' mappings.</summary>
[Mapper]
public static partial class ArticleMapper
{
    /// <remarks><see cref="Article.Teams"/> is never filled: an article is always answered with an empty list.</remarks>
    [MapperIgnoreTarget(nameof(Article.Teams))]
    public static partial Article ToArticle(this ArticleEntity source);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public static partial ArticleEntity ToEntity(this ArticleCreate source);

    /// <remarks>Writes every member of the edit, so a value it leaves out is cleared.</remarks>
    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public static partial void ApplyTo(this ArticleEdit source, ArticleEntity target);
}
