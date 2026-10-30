// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Linq.Expressions;
using Gallery.Api.Data.Models;

namespace Gallery.Api.Services
{
    /// <summary>Which articles and cards belong to an exhibit, for every rule that links one to the other.</summary>
    public static class ArticleScope
    {
        /// <summary>
        /// An article of the exhibit's collection that is a template (no exhibit) or the exhibit's own.
        /// Another exhibit's article on the same collection is not in the exhibit.
        /// </summary>
        public static Expression<Func<ArticleEntity, bool>> InExhibit(Guid exhibitId, Guid collectionId) =>
            a => a.CollectionId == collectionId && (a.ExhibitId == null || a.ExhibitId == exhibitId);

        public static Expression<Func<ArticleEntity, bool>> InExhibit(ExhibitEntity exhibit) =>
            InExhibit(exhibit.Id, exhibit.CollectionId);
    }
}
