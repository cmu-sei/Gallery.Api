// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Api.Infrastructure.Extensions;
using Api.Infrastructure.Exceptions;
using Api.Services;
using Api.ViewModels;
using Swashbuckle.AspNetCore.Annotations;

namespace Api.Controllers
{
    public class ArticleController : BaseController
    {
        private readonly IArticleService _articleService;
        private readonly IAuthorizationService _authorizationService;

        public ArticleController(IArticleService articleService, IAuthorizationService authorizationService)
        {
            _articleService = articleService;
            _authorizationService = authorizationService;
        }

        /// <summary>
        /// Gets Articles
        /// </summary>
        /// <remarks>
        /// Returns a list of Articles.
        /// </remarks>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpGet("articles")]
        [ProducesResponseType(typeof(IEnumerable<Article>), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "getArticles")]
        public async Task<IActionResult> Get(CancellationToken ct)
        {
            var list = await _articleService.GetAsync(ct);
            return Ok(list);
        }

        /// <summary>
        /// Gets Articles for a Card
        /// </summary>
        /// <remarks>
        /// Returns a list of Articles based on the Card ID.
        /// </remarks>
        /// <param name="cardId">The id of the Card</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpGet("cards/{cardId}/articles")]
        [ProducesResponseType(typeof(IEnumerable<Article>), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "getCardArticles")]
        public async Task<IActionResult> GetByCard(Guid cardId, CancellationToken ct)
        {
            var list = await _articleService.GetByCardAsync(cardId, ct);
            return Ok(list);
        }

        /// <summary>
        /// Gets Articles for a Collection
        /// </summary>
        /// <remarks>
        /// Returns a list of Articles based on the collection ID
        /// </remarks>
        /// <param name="collectionId">The id of the Collection</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpGet("collections/{collectionId}/articles")]
        [ProducesResponseType(typeof(IEnumerable<Article>), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "getCollectionArticles")]
        public async Task<IActionResult> GetByCollection(Guid collectionId, CancellationToken ct)
        {
            var list = await _articleService.GetByCollectionAsync(collectionId, ct);
            return Ok(list);
        }

        /// <summary>
        /// Gets Articles for an Exhibit
        /// </summary>
        /// <remarks>
        /// Returns a list of Articles based on the exhibit's current move and current inject.
        /// </remarks>
        /// <param name="exhibitId">The id of the Exhibit</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpGet("exhibits/{exhibitId}/articles")]
        [ProducesResponseType(typeof(IEnumerable<Article>), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "getExhibitArticles")]
        public async Task<IActionResult> GetByExhibit(Guid exhibitId, CancellationToken ct)
        {
            var list = await _articleService.GetByExhibitAsync(exhibitId, ct);
            return Ok(list);
        }

        /// <summary>
        /// Gets a specific Article by id
        /// </summary>
        /// <remarks>
        /// Returns the Article with the id specified
        /// </remarks>
        /// <param name="id">The id of the Article</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpGet("articles/{id}")]
        [ProducesResponseType(typeof(Article), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "getArticle")]
        public async Task<IActionResult> Get(Guid id, CancellationToken ct)
        {
            var article = await _articleService.GetAsync(id, ct);

            if (article == null)
                throw new EntityNotFoundException<Article>();

            return Ok(article);
        }

        /// <summary>
        /// Creates a new Article
        /// </summary>
        /// <remarks>
        /// Creates a new Article with the attributes specified
        /// <para />
        /// Accessible only to a ContentDeveloper or an Administrator
        /// </remarks>
        /// <param name="article">The data used to create the Article</param>
        /// <param name="ct"></param>
        [HttpPost("articles")]
        [ProducesResponseType(typeof(Article), (int)HttpStatusCode.Created)]
        [SwaggerOperation(OperationId = "createArticle")]
        public async Task<IActionResult> Create([FromBody] Article article, CancellationToken ct)
        {
            article.CreatedBy = User.GetId();
            var createdArticle = await _articleService.CreateAsync(article, ct);
            return CreatedAtAction(nameof(this.Get), new { id = createdArticle.Id }, createdArticle);
        }

        /// <summary>
        /// Updates an Article
        /// </summary>
        /// <remarks>
        /// Updates a Article with the attributes specified.
        /// The ID from the route MUST MATCH the ID contained in the article parameter
        /// <para />
        /// Accessible only to a ContentDeveloper or an Administrator
        /// </remarks>
        /// <param name="id">The Id of the Article to update</param>
        /// <param name="article">The updated Article values</param>
        /// <param name="ct"></param>
        [HttpPut("articles/{id}")]
        [ProducesResponseType(typeof(Article), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "updateArticle")]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] Article article, CancellationToken ct)
        {
            article.ModifiedBy = User.GetId();
            var updatedArticle = await _articleService.UpdateAsync(id, article, ct);
            return Ok(updatedArticle);
        }

        /// <summary>
        /// Deletes an Article
        /// </summary>
        /// <remarks>
        /// Deletes an Article with the specified id
        /// <para />
        /// Accessible only to a ContentDeveloper or an Administrator
        /// </remarks>
        /// <param name="id">The id of the Article to delete</param>
        /// <param name="ct"></param>
        [HttpDelete("articles/{id}")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [SwaggerOperation(OperationId = "deleteArticle")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            await _articleService.DeleteAsync(id, ct);
            return NoContent();
        }

    }
}

