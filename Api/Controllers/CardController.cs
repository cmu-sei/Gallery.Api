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
    public class CardController : BaseController
    {
        private readonly ICardService _cardService;
        private readonly IAuthorizationService _authorizationService;

        public CardController(ICardService cardService, IAuthorizationService authorizationService)
        {
            _cardService = cardService;
            _authorizationService = authorizationService;
        }

        /// <summary>
        /// Gets Cards
        /// </summary>
        /// <remarks>
        /// Returns a list of Cards.
        /// </remarks>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpGet("cards")]
        [ProducesResponseType(typeof(IEnumerable<Card>), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "getCards")]
        public async Task<IActionResult> Get(CancellationToken ct)
        {
            var list = await _cardService.GetAsync(ct);
            return Ok(list);
        }

        /// <summary>
        /// Gets Cards for a Collection
        /// </summary>
        /// <remarks>
        /// Returns a list of Cards based on the collection ID.
        /// </remarks>
        /// <param name="collectionId">The id of the Collection</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpGet("collections/{collectionId}/cards")]
        [ProducesResponseType(typeof(IEnumerable<Card>), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "getCollectionCards")]
        public async Task<IActionResult> GetByCollection(Guid collectionId, CancellationToken ct)
        {
            var list = await _cardService.GetByCollectionAsync(collectionId, ct);
            return Ok(list);
        }

        /// <summary>
        /// Gets Cards for an Exhibit
        /// </summary>
        /// <remarks>
        /// Returns a list of Cards based on the exhibit's current move and current inject.
        /// </remarks>
        /// <param name="exhibitId">The id of the Exhibit</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpGet("exhibits/{exhibitId}/cards")]
        [ProducesResponseType(typeof(IEnumerable<Card>), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "getExhibitCards")]
        public async Task<IActionResult> GetByExhibit(Guid exhibitId, CancellationToken ct)
        {
            var list = await _cardService.GetByExhibitAsync(exhibitId, ct);
            return Ok(list);
        }

        /// <summary>
        /// Gets Cards for an exhibit for the user
        /// </summary>
        /// <remarks>
        /// Returns a list of Cards based on the exhibit's current move and current inject for the user.
        /// </remarks>
        /// <param name="exhibitId">The id of the Exhibit</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpGet("exhibits/{exhibitId}/cards/mine")]
        [ProducesResponseType(typeof(IEnumerable<Card>), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "getExhibitCardsMine")]
        public async Task<IActionResult> GetByExhibitUser(Guid exhibitId, CancellationToken ct)
        {
            var list = await _cardService.GetByExhibitUserAsync(exhibitId, ct);
            return Ok(list);
        }

        /// <summary>
        /// Gets a specific Card by id
        /// </summary>
        /// <remarks>
        /// Returns the Card with the id specified
        /// </remarks>
        /// <param name="id">The id of the Card</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpGet("cards/{id}")]
        [ProducesResponseType(typeof(Card), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "getCard")]
        public async Task<IActionResult> Get(Guid id, CancellationToken ct)
        {
            var card = await _cardService.GetAsync(id, ct);

            if (card == null)
                throw new EntityNotFoundException<Card>();

            return Ok(card);
        }

        /// <summary>
        /// Creates a new Card
        /// </summary>
        /// <remarks>
        /// Creates a new Card with the attributes specified
        /// <para />
        /// Accessible only to a ContentDeveloper or an Administrator
        /// </remarks>
        /// <param name="card">The data used to create the Card</param>
        /// <param name="ct"></param>
        [HttpPost("cards")]
        [ProducesResponseType(typeof(Card), (int)HttpStatusCode.Created)]
        [SwaggerOperation(OperationId = "createCard")]
        public async Task<IActionResult> Create([FromBody] Card card, CancellationToken ct)
        {
            card.CreatedBy = User.GetId();
            var createdCard = await _cardService.CreateAsync(card, ct);
            return CreatedAtAction(nameof(this.Get), new { id = createdCard.Id }, createdCard);
        }

        /// <summary>
        /// Updates a  Card
        /// </summary>
        /// <remarks>
        /// Updates a Card with the attributes specified.
        /// The ID from the route MUST MATCH the ID contained in the card parameter
        /// <para />
        /// Accessible only to a ContentDeveloper or an Administrator
        /// </remarks>
        /// <param name="id">The Id of the Card to update</param>
        /// <param name="card">The updated Card values</param>
        /// <param name="ct"></param>
        [HttpPut("cards/{id}")]
        [ProducesResponseType(typeof(Card), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "updateCard")]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] Card card, CancellationToken ct)
        {
            card.ModifiedBy = User.GetId();
            var updatedCard = await _cardService.UpdateAsync(id, card, ct);
            return Ok(updatedCard);
        }

        /// <summary>
        /// Deletes a  Card
        /// </summary>
        /// <remarks>
        /// Deletes a  Card with the specified id
        /// <para />
        /// Accessible only to a ContentDeveloper or an Administrator
        /// </remarks>
        /// <param name="id">The id of the Card to delete</param>
        /// <param name="ct"></param>
        [HttpDelete("cards/{id}")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [SwaggerOperation(OperationId = "deleteCard")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            await _cardService.DeleteAsync(id, ct);
            return NoContent();
        }

    }
}

