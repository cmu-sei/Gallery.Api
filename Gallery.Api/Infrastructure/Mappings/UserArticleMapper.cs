// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Gallery.Api.Data.Models;
using Gallery.Api.ViewModels;
using Riok.Mapperly.Abstractions;

namespace Gallery.Api.Infrastructure.Mappings;

/// <summary>User articles' mappings. The article is mapped when the context has loaded it.</summary>
[Mapper]
[UseStaticMapper(typeof(ArticleMapper))]
public static partial class UserArticleMapper
{
    public static partial UserArticle ToUserArticle(this UserArticleEntity source);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public static partial UserArticleEntity ToEntity(this UserArticleCreate source);
}
