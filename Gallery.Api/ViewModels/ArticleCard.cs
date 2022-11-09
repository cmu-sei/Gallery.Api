// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;

namespace Gallery.Api.ViewModels
{
    public class ArticleCard : Base
    {
        public ArticleCard() {}

        public ArticleCard(Guid cardId, Guid articleId)
        {
            CardId = cardId;
            ArticleId = articleId;
        }

        public Guid Id { get; set; }
        public Guid ArticleId { get; set; }
        public Article Article {get; set; }
        public Guid CardId { get; set; }
        public Card Card { get; set; }
    }

}

