// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Gallery.Api.Infrastructure.Extensions;
using Gallery.Api.Infrastructure.Exceptions;
using Gallery.Api.Services;
using Gallery.Api.ViewModels;
using Swashbuckle.AspNetCore.Annotations;

namespace Gallery.Api.Controllers
{
    public class ArticleCardController : BaseController
    {
        private readonly IArticleCardService _articleCardService;
        private readonly IAuthorizationService _authorizationService;

        public ArticleCardController(IArticleCardService articleCardService, IAuthorizationService authorizationService)
        {
            _articleCardService = articleCardService;
            _authorizationService = authorizationService;
        }

        /// <summary>
        /// Gets all ArticleCards in the system
        /// </summary>
        /// <remarks>
        /// Returns a list of all of the ArticleCards in the system.
        /// <para />
        /// Only accessible to a SuperUser
        /// </remarks>
        /// <returns></returns>
        [HttpGet("articlecards")]
        [ProducesResponseType(typeof(IEnumerable<ArticleCard>), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "getArticleCards")]
        public async Task<IActionResult> Get(CancellationToken ct)
        {
            var list = await _articleCardService.GetAsync(ct);
            return Ok(list);
        }

        /// <summary>
        /// Gets all ArticleCards for a collection
        /// </summary>
        /// <remarks>
        /// Returns a list of all of the ArticleCards for the collection.
        /// </remarks>
        /// <param name="collectionId">The id of the Collection</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpGet("collections/{collectionId}/articlecards")]
        [ProducesResponseType(typeof(IEnumerable<ArticleCard>), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "getCollectionArticleCards")]
        public async Task<IActionResult> GetByCollection(Guid collectionId, CancellationToken ct)
        {
            var list = await _articleCardService.GetByCollectionAsync(collectionId, ct);
            return Ok(list);
        }

        /// <summary>
        /// Gets all ArticleCards for an card
        /// </summary>
        /// <remarks>
        /// Returns a list of all of the ArticleCards for the card.
        /// </remarks>
        /// <param name="cardId">The id of the ArticleCard</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpGet("cards/{cardId}/articlecards")]
        [ProducesResponseType(typeof(IEnumerable<ArticleCard>), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "getCardArticleCards")]
        public async Task<IActionResult> GetByCard(Guid cardId, CancellationToken ct)
        {
            var list = await _articleCardService.GetByCardAsync(cardId, ct);
            return Ok(list);
        }

        /// <summary>
        /// Gets all ArticleCards for an article
        /// </summary>
        /// <remarks>
        /// Returns a list of all of the ArticleCards for the article.
        /// </remarks>
        /// <param name="articleId">The id of the ArticleCard</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpGet("articles/{articleId}/articlecards")]
        [ProducesResponseType(typeof(IEnumerable<ArticleCard>), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "getArticleArticleCards")]
        public async Task<IActionResult> GetByArticle(Guid articleId, CancellationToken ct)
        {
            var list = await _articleCardService.GetByArticleAsync(articleId, ct);
            return Ok(list);
        }

        /// <summary>
        /// Gets a specific ArticleCard by id
        /// </summary>
        /// <remarks>
        /// Returns the ArticleCard with the id specified
        /// <para />
        /// Only accessible to a SuperUser
        /// </remarks>
        /// <param name="id">The id of the ArticleCard</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpGet("articlecards/{id}")]
        [ProducesResponseType(typeof(ArticleCard), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "getArticleCard")]
        public async Task<IActionResult> Get(Guid id, CancellationToken ct)
        {
            var article = await _articleCardService.GetAsync(id, ct);

            if (article == null)
                throw new EntityNotFoundException<ArticleCard>();

            return Ok(article);
        }

        /// <summary>
        /// Creates a new ArticleCard
        /// </summary>
        /// <remarks>
        /// Creates a new ArticleCard with the attributes specified
        /// <para />
        /// Accessible only to a SuperUser
        /// </remarks>
        /// <param name="articleCard">The data to create the ArticleCard with</param>
        /// <param name="ct"></param>
        [HttpPost("articlecards")]
        [ProducesResponseType(typeof(ArticleCard), (int)HttpStatusCode.Created)]
        [SwaggerOperation(OperationId = "createArticleCard")]
        public async Task<IActionResult> Create([FromBody] ArticleCard articleCard, CancellationToken ct)
        {
            articleCard.CreatedBy = User.GetId();
            var createdArticleCard = await _articleCardService.CreateAsync(articleCard, ct);
            return CreatedAtAction(nameof(this.Get), new { id = createdArticleCard.Id }, createdArticleCard);
        }

        /// <summary>
        /// Updates an ArticleCard
        /// </summary>
        /// <remarks>
        /// Updates a ArticleCard with the attributes specified.
        /// The ID from the route MUST MATCH the ID contained in the articleCard parameter
        /// <para />
        /// Accessible only to a ContentDeveloper or an Administrator
        /// </remarks>
        /// <param name="id">The Id of the ArticleCard to update</param>
        /// <param name="articleCard">The updated ArticleCard values</param>
        /// <param name="ct"></param>
        [HttpPut("articleCards/{id}")]
        [ProducesResponseType(typeof(ArticleCard), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "updateArticleCard")]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] ArticleCard articleCard, CancellationToken ct)
        {
            articleCard.ModifiedBy = User.GetId();
            var updatedArticleCard = await _articleCardService.UpdateAsync(id, articleCard, ct);
            return Ok(updatedArticleCard);
        }

        /// <summary>
        /// Deletes a ArticleCard
        /// </summary>
        /// <remarks>
        /// Deletes a ArticleCard with the specified id
        /// <para />
        /// Accessible only to a SuperUser
        /// </remarks>
        /// <param name="id">The id of the ArticleCard to delete</param>
        /// <param name="ct"></param>
        [HttpDelete("articlecards/{id}")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [SwaggerOperation(OperationId = "deleteArticleCard")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            await _articleCardService.DeleteAsync(id, ct);
            return NoContent();
        }

        /// <summary>
        /// Deletes a ArticleCard by card ID and article ID
        /// </summary>
        /// <remarks>
        /// Deletes a ArticleCard with the specified card ID and article ID
        /// <para />
        /// Accessible only to a SuperUser
        /// </remarks>
        /// <param name="cardId">ID of a card.</param>
        /// <param name="articleId">ID of a article.</param>
        /// <param name="ct"></param>
        [HttpDelete("articles/{articleId}/cards/{cardId}")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [SwaggerOperation(OperationId = "deleteArticleCardByIds")]
        public async Task<IActionResult> Delete(Guid articleId, Guid cardId, CancellationToken ct)
        {
            await _articleCardService.DeleteByIdsAsync(articleId, cardId, ct);
            return NoContent();
        }

    }
}

