// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Gallery.Api.Data.Enumerations;
using Gallery.Api.Data.Models;
using Gallery.Api.Infrastructure.Exceptions;

namespace Gallery.Api.Services
{
    // The fields a collection or exhibit file can set. The property names match the files that
    // DownloadJsonAsync writes, so earlier downloads still upload. Anything else in a file, such as
    // users, roles, memberships or TeamUsers, is not read. Every Id is file-local: it only links the
    // records of one file, and each record gets a new Id when it is saved.

    public class CollectionImport
    {
        public CollectionImportFields Collection { get; set; }
        public List<CardImport> Cards { get; set; } = [];
        public List<ArticleImport> Articles { get; set; } = [];
    }

    public class ExhibitImport
    {
        public CollectionImportFields Collection { get; set; }
        public List<CardImport> Cards { get; set; } = [];
        public List<ArticleImport> Articles { get; set; } = [];
        public ExhibitImportFields Exhibit { get; set; }
        public List<TeamImport> Teams { get; set; } = [];
        public List<TeamCardImport> TeamCards { get; set; } = [];
        public List<TeamArticleImport> TeamArticles { get; set; } = [];
    }

    public class CollectionImportFields
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class CardImport
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int Move { get; set; }
        public int Inject { get; set; }
    }

    public class ArticleImport
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Summary { get; set; }
        public string Description { get; set; }
        /// <summary>Null for a template article. Read only to keep the file's own exhibit articles.</summary>
        public Guid? ExhibitId { get; set; }
        public Guid? CardId { get; set; }
        public int Move { get; set; }
        public int Inject { get; set; }
        public ItemStatus Status { get; set; }
        public SourceType SourceType { get; set; }
        public string SourceName { get; set; }
        public string Url { get; set; }
        public DateTime DatePosted { get; set; }
        public bool OpenInNewTab { get; set; }
    }

    public class ExhibitImportFields
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool ShowAdvanceButton { get; set; }
    }

    public class TeamImport
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string ShortName { get; set; }
        public string Email { get; set; }
    }

    public class TeamCardImport
    {
        public Guid TeamId { get; set; }
        public Guid CardId { get; set; }
        public int Move { get; set; }
        public int Inject { get; set; }
        public bool IsShownOnWall { get; set; }
        public bool CanPostArticles { get; set; }
    }

    public class TeamArticleImport
    {
        public Guid TeamId { get; set; }
        public Guid ArticleId { get; set; }
    }

    /// <summary>
    /// Builds new entities from an import, field by field. It checks every reference before it returns,
    /// so nothing is tracked for a file that does not hold together.
    /// </summary>
    public static class ImportBuilder
    {
        /// <summary>
        /// New cards and articles for collection <paramref name="collectionId"/>. Template articles are kept.
        /// An article of exhibit <paramref name="fromExhibitId"/> is kept for <paramref name="toExhibitId"/>;
        /// an article of any other exhibit is left out. <paramref name="cardIdMap"/> starts with the cards that
        /// already belong to the collection, if any, and gets the new ones.
        /// </summary>
        public static (List<CardEntity> Cards, List<ArticleEntity> Articles, Dictionary<Guid, Guid> ArticleIdMap) BuildCollectionContent(
            Guid collectionId,
            IEnumerable<CardImport> cards,
            IEnumerable<ArticleImport> articles,
            Guid? fromExhibitId,
            Guid? toExhibitId,
            Dictionary<Guid, Guid> cardIdMap)
        {
            var cardEntities = new List<CardEntity>();
            foreach (var card in cards ?? [])
            {
                if (card == null || cardIdMap.ContainsKey(card.Id))
                    throw new BadRequestException("The file lists a card more than once.");

                var cardEntity = new CardEntity
                {
                    Id = Guid.NewGuid(),
                    CollectionId = collectionId,
                    Name = card.Name,
                    Description = card.Description,
                    Move = card.Move,
                    Inject = card.Inject
                };
                cardIdMap[card.Id] = cardEntity.Id;
                cardEntities.Add(cardEntity);
            }

            var articleEntities = new List<ArticleEntity>();
            var articleIdMap = new Dictionary<Guid, Guid>();
            foreach (var article in articles ?? [])
            {
                if (article == null || articleIdMap.ContainsKey(article.Id))
                    throw new BadRequestException("The file lists an article more than once.");

                Guid? exhibitId;
                if (article.ExhibitId == null)
                    exhibitId = null;
                else if (fromExhibitId.HasValue && article.ExhibitId == fromExhibitId)
                    exhibitId = toExhibitId;
                else
                    continue;

                Guid? cardId = null;
                if (article.CardId.HasValue)
                {
                    if (!cardIdMap.TryGetValue(article.CardId.Value, out var mappedCardId))
                        throw new BadRequestException($"Article {article.Name} names a card that is not in the file.");
                    cardId = mappedCardId;
                }

                var articleEntity = new ArticleEntity
                {
                    Id = Guid.NewGuid(),
                    CollectionId = collectionId,
                    ExhibitId = exhibitId,
                    CardId = cardId,
                    Name = article.Name,
                    Summary = article.Summary,
                    Description = article.Description,
                    Move = article.Move,
                    Inject = article.Inject,
                    Status = article.Status,
                    SourceType = article.SourceType,
                    SourceName = article.SourceName,
                    Url = article.Url,
                    DatePosted = article.DatePosted,
                    OpenInNewTab = article.OpenInNewTab
                };
                articleIdMap[article.Id] = articleEntity.Id;
                articleEntities.Add(articleEntity);
            }

            return (cardEntities, articleEntities, articleIdMap);
        }

        public static CardImport ToImport(CardEntity card) => new()
        {
            Id = card.Id,
            Name = card.Name,
            Description = card.Description,
            Move = card.Move,
            Inject = card.Inject
        };

        public static ArticleImport ToImport(ArticleEntity article) => new()
        {
            Id = article.Id,
            Name = article.Name,
            Summary = article.Summary,
            Description = article.Description,
            ExhibitId = article.ExhibitId,
            CardId = article.CardId,
            Move = article.Move,
            Inject = article.Inject,
            Status = article.Status,
            SourceType = article.SourceType,
            SourceName = article.SourceName,
            Url = article.Url,
            DatePosted = article.DatePosted,
            OpenInNewTab = article.OpenInNewTab
        };

        public static TeamImport ToImport(TeamEntity team) => new()
        {
            Id = team.Id,
            Name = team.Name,
            ShortName = team.ShortName,
            Email = team.Email
        };

        public static TeamCardImport ToImport(TeamCardEntity teamCard) => new()
        {
            TeamId = teamCard.TeamId,
            CardId = teamCard.CardId,
            Move = teamCard.Move,
            Inject = teamCard.Inject,
            IsShownOnWall = teamCard.IsShownOnWall,
            CanPostArticles = teamCard.CanPostArticles
        };

        public static TeamArticleImport ToImport(TeamArticleEntity teamArticle) => new()
        {
            TeamId = teamArticle.TeamId,
            ArticleId = teamArticle.ArticleId
        };
    }
}
