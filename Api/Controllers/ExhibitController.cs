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
    public class ExhibitController : BaseController
    {
        private readonly IExhibitService _exhibitService;
        private readonly IAuthorizationService _authorizationService;

        public ExhibitController(IExhibitService exhibitService, IAuthorizationService authorizationService)
        {
            _exhibitService = exhibitService;
            _authorizationService = authorizationService;
        }

        /// <summary>
        /// Gets Exhibits
        /// </summary>
        /// <remarks>
        /// Returns a list of Exhibits.
        /// </remarks>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpGet("exhibits")]
        [ProducesResponseType(typeof(IEnumerable<Exhibit>), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "getExhibits")]
        public async Task<IActionResult> Get(CancellationToken ct)
        {
            var list = await _exhibitService.GetAsync(ct);
            return Ok(list);
        }

        /// <summary>
        /// Get User's Exhibits
        /// </summary>
        /// <remarks>
        /// Returns a list of Exhibits.
        /// </remarks>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpGet("exhibits/mine")]
        [ProducesResponseType(typeof(IEnumerable<Exhibit>), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "getMyExhibits")]
        public async Task<IActionResult> GetMine(CancellationToken ct)
        {
            var list = await _exhibitService.GetMineAsync(ct);
            return Ok(list);
        }

        /// <summary>
        /// Gets Exhibits for a Collection
        /// </summary>
        /// <remarks>
        /// Returns a list of Exhibits based on the collection ID
        /// </remarks>
        /// <param name="collectionId">The id of the Collection</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpGet("collections/{collectionId}/exhibits")]
        [ProducesResponseType(typeof(IEnumerable<Exhibit>), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "getCollectionExhibits")]
        public async Task<IActionResult> GetByCollection(Guid collectionId, CancellationToken ct)
        {
            var list = await _exhibitService.GetByCollectionAsync(collectionId, ct);
            return Ok(list);
        }

        /// <summary>
        /// Gets User's Exhibits for a Collection
        /// </summary>
        /// <remarks>
        /// Returns a list of Exhibits based on the user and collection ID
        /// </remarks>
        /// <param name="collectionId">The id of the Collection</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpGet("collections/{collectionId}/exhibits/mine")]
        [ProducesResponseType(typeof(IEnumerable<Exhibit>), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "getMyCollectionExhibits")]
        public async Task<IActionResult> GetMineByCollection(Guid collectionId, CancellationToken ct)
        {
            var list = await _exhibitService.GetMineByCollectionAsync(collectionId, ct);
            return Ok(list);
        }

        /// <summary>
        /// Gets a specific Exhibit by id
        /// </summary>
        /// <remarks>
        /// Returns the Exhibit with the id specified
        /// </remarks>
        /// <param name="id">The id of the Exhibit</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpGet("exhibits/{id}")]
        [ProducesResponseType(typeof(Exhibit), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "getExhibit")]
        public async Task<IActionResult> Get(Guid id, CancellationToken ct)
        {
            var exhibit = await _exhibitService.GetAsync(id, ct);

            if (exhibit == null)
                throw new EntityNotFoundException<Exhibit>();

            return Ok(exhibit);
        }

        /// <summary>
        /// Creates a new Exhibit
        /// </summary>
        /// <remarks>
        /// Creates a new Exhibit with the attributes specified
        /// <para />
        /// Accessible only to a ContentDeveloper or an Administrator
        /// </remarks>
        /// <param name="exhibit">The data used to create the Exhibit</param>
        /// <param name="ct"></param>
        [HttpPost("exhibits")]
        [ProducesResponseType(typeof(Exhibit), (int)HttpStatusCode.Created)]
        [SwaggerOperation(OperationId = "createExhibit")]
        public async Task<IActionResult> Create([FromBody] Exhibit exhibit, CancellationToken ct)
        {
            exhibit.CreatedBy = User.GetId();
            var createdExhibit = await _exhibitService.CreateAsync(exhibit, ct);
            return CreatedAtAction(nameof(this.Get), new { id = createdExhibit.Id }, createdExhibit);
        }

        /// <summary>
        /// Updates an Exhibit
        /// </summary>
        /// <remarks>
        /// Updates an Exhibit with the attributes specified.
        /// The ID from the route MUST MATCH the ID contained in the exhibit parameter
        /// <para />
        /// Accessible only to a ContentDeveloper or an Administrator
        /// </remarks>
        /// <param name="id">The Id of the Exhibit to update</param>
        /// <param name="exhibit">The updated Exhibit values</param>
        /// <param name="ct"></param>
        [HttpPut("exhibits/{id}")]
        [ProducesResponseType(typeof(Exhibit), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "updateExhibit")]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] Exhibit exhibit, CancellationToken ct)
        {
            exhibit.ModifiedBy = User.GetId();
            var updatedExhibit = await _exhibitService.UpdateAsync(id, exhibit, ct);
            return Ok(updatedExhibit);
        }

        /// <summary>
        /// Updates an Exhibit's current move and inject
        /// </summary>
        /// <remarks>
        /// Updates an Exhibit with the move and inject numbers specified.
        /// </remarks>
        /// <param name="id">The Id of the Exhibit to update</param>
        /// <param name="move">The move value to set</param>
        /// <param name="inject">The inject value to set</param>
        /// <param name="ct"></param>
        [HttpPut("exhibits/{id}/move/{move}/inject/{inject}")]
        [ProducesResponseType(typeof(Exhibit), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "setExhibitMoveAndInject")]
        public async Task<IActionResult> setExhibitMoveAndInject([FromRoute] Guid id, int move, int inject, CancellationToken ct)
        {
            var updatedExhibit = await _exhibitService.SetMoveAndInjectAsync(id, move, inject, ct);
            return Ok(updatedExhibit);
        }

        /// <summary>
        /// Deletes an Exhibit
        /// </summary>
        /// <remarks>
        /// Deletes an Exhibit with the specified id
        /// <para />
        /// Accessible only to a ContentDeveloper or an Administrator
        /// </remarks>
        /// <param name="id">The id of the Exhibit to delete</param>
        /// <param name="ct"></param>
        [HttpDelete("exhibits/{id}")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [SwaggerOperation(OperationId = "deleteExhibit")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            await _exhibitService.DeleteAsync(id, ct);
            return NoContent();
        }

    }
}

