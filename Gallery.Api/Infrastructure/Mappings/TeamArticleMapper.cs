// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Gallery.Api.Data.Models;
using Gallery.Api.ViewModels;
using Riok.Mapperly.Abstractions;

namespace Gallery.Api.Infrastructure.Mappings;

/// <summary>Team articles' mappings.</summary>
[Mapper]
public static partial class TeamArticleMapper
{
    public static partial TeamArticle ToTeamArticle(this TeamArticleEntity source);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public static partial TeamArticleEntity ToEntity(this TeamArticleCreate source);
}
