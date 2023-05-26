// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Gallery.Api.Infrastructure.Exceptions;
using Gallery.Api.Services;
using Gallery.Api.ViewModels;
using Swashbuckle.AspNetCore.Annotations;

namespace Gallery.Api.Controllers
{
    public class XApiController : BaseController
    {
        private readonly IArticleService _articleService;
        private readonly ICardService _cardService;
        private readonly ICollectionService _collectionService;
        private readonly IExhibitService _exhibitService;
        private readonly ITeamService _teamService;
        private readonly IXApiService _xApiService;
        private readonly IAuthorizationService _authorizationService;

        public XApiController(
            IArticleService articleService,
            ICardService cardService,
            ICollectionService collectionService,
            IExhibitService exhibitService,
            ITeamService teamService,
            IXApiService xApiService,
            IAuthorizationService authorizationService)
        {
            _articleService = articleService;
            _cardService = cardService;
            _collectionService = collectionService;
            _exhibitService = exhibitService;
            _teamService = teamService;
            _xApiService = xApiService;
            _authorizationService = authorizationService;
        }


        /// <summary>
        /// Logs xAPI viewed statement for Card by id
        /// </summary>
        /// <remarks>
        /// Returns status
        /// </remarks>
        /// <param name="exhibitId">The id of the Exhibit</param>
        /// <param name="cardId">The id of the Card</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpGet("xapi/viewed/exhibit/{exhibitId}/card/{cardId}")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "viewedCard")]
        public async Task<IActionResult> ViewCard(Guid exhibitId, Guid cardId, CancellationToken ct)
        {
            var card = await _cardService.GetAsync(cardId, ct);
            if (card == null)
                throw new EntityNotFoundException<Card>();

            var collection = await _collectionService.GetAsync(card.CollectionId, ct);
            if (collection == null)
                throw new EntityNotFoundException<Collection>();

            var exhibit = await _exhibitService.GetAsync(exhibitId, ct);
            if (exhibit == null)
                throw new EntityNotFoundException<Exhibit>();


            if (!await _xApiService.CardViewedAsync(card, exhibit, collection, ct))
                throw new Exception();

            return Ok();
        }

        /// <summary>
        /// Logs xAPI viewed statement for Wall by Exhibit id
        /// </summary>
        /// <remarks>
        /// Returns status
        /// </remarks>
        /// <param name="exhibitId">The id of the Exhibit</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpGet("xapi/viewed/exhibit/{exhibitId}/wall")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "viewedExhibitWall")]
        public async Task<IActionResult> ViewExhibitWall(Guid exhibitId, CancellationToken ct)
        {
            var exhibit = await _exhibitService.GetAsync(exhibitId, ct);
            if (exhibit == null)
                throw new EntityNotFoundException<Exhibit>();

            var collection = await _collectionService.GetAsync(exhibit.CollectionId, ct);
            if (collection == null)
                throw new EntityNotFoundException<Collection>();

            if (!await _xApiService.ExhibitWallViewedAsync(exhibit, collection, ct))
                throw new Exception();

            return Ok();
        }


        /// <summary>
        /// Logs xAPI viewed statement for Archive by Exhibit id
        /// </summary>
        /// <remarks>
        /// Returns status
        /// </remarks>
        /// <param name="exhibitId">The id of the Exhibit</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpGet("xapi/viewed/exhibit/{exhibitId}/archive")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "viewedExhibitArchive")]
        public async Task<IActionResult> ViewExhibitArchive(Guid exhibitId, CancellationToken ct)
        {
            var exhibit = await _exhibitService.GetAsync(exhibitId, ct);
            if (exhibit == null)
                throw new EntityNotFoundException<Exhibit>();

            var collection = await _collectionService.GetAsync(exhibit.CollectionId, ct);
            if (collection == null)
                throw new EntityNotFoundException<Collection>();

            if (!await _xApiService.ExhibitArchiveViewedAsync(exhibit, collection, ct))
                throw new Exception();

            return Ok();
        }

        /// <summary>
        /// Logs xAPI viewed statement for Article by id
        /// </summary>
        /// <remarks>
        /// Returns status
        /// </remarks>
        /// <param name="articleId">The id of the Article</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpGet("xapi/viewed/article/{articleId}")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "viewedArticle")]
        public async Task<IActionResult> ViewArticle(Guid articleId, CancellationToken ct)
        {
            var article = await _articleService.GetAsync(articleId, ct);
            if (article == null)
                throw new EntityNotFoundException<Article>();

            var card = await _cardService.GetAsync(article.CardId.Value, ct);
            if (card == null)
                throw new EntityNotFoundException<Card>();

            var collection = await _collectionService.GetAsync(article.CollectionId, ct);
            if (collection == null)
                throw new EntityNotFoundException<Collection>();

            if (!await _xApiService.ArticleViewedAsync(article, card, collection, ct))
                throw new Exception();

            return Ok();
        }


    }
}

