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
    public class CollectionController : BaseController
    {
        private readonly ICollectionService _collectionService;
        private readonly IAuthorizationService _authorizationService;

        public CollectionController(ICollectionService collectionService, IAuthorizationService authorizationService)
        {
            _collectionService = collectionService;
            _authorizationService = authorizationService;
        }

        /// <summary>
        /// Gets Collections
        /// </summary>
        /// <remarks>
        /// Returns a list of Collections.
        /// </remarks>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpGet("collections")]
        [ProducesResponseType(typeof(IEnumerable<Collection>), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "getCollections")]
        public async Task<IActionResult> Get(CancellationToken ct)
        {
            var list = await _collectionService.GetAsync(ct);
            return Ok(list);
        }

        /// <summary>
        /// Gets User's Collections
        /// </summary>
        /// <remarks>
        /// Returns a list of Collections.
        /// </remarks>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpGet("collections/mine")]
        [ProducesResponseType(typeof(IEnumerable<Collection>), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "getMyCollections")]
        public async Task<IActionResult> GetMine(CancellationToken ct)
        {
            var list = await _collectionService.GetMineAsync(ct);
            return Ok(list);
        }

        /// <summary>
        /// Gets a specific Collection by id
        /// </summary>
        /// <remarks>
        /// Returns the Collection with the id specified
        /// </remarks>
        /// <param name="id">The id of the Collection</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpGet("collections/{id}")]
        [ProducesResponseType(typeof(Collection), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "getCollection")]
        public async Task<IActionResult> Get(Guid id, CancellationToken ct)
        {
            var collection = await _collectionService.GetAsync(id, ct);

            if (collection == null)
                throw new EntityNotFoundException<Collection>();

            return Ok(collection);
        }

        /// <summary>
        /// Creates a new Collection
        /// </summary>
        /// <remarks>
        /// Creates a new Collection with the attributes specified
        /// <para />
        /// Accessible only to a ContentDeveloper or an Administrator
        /// </remarks>
        /// <param name="collection">The data used to create the Collection</param>
        /// <param name="ct"></param>
        [HttpPost("collections")]
        [ProducesResponseType(typeof(Collection), (int)HttpStatusCode.Created)]
        [SwaggerOperation(OperationId = "createCollection")]
        public async Task<IActionResult> Create([FromBody] Collection collection, CancellationToken ct)
        {
            collection.CreatedBy = User.GetId();
            var createdCollection = await _collectionService.CreateAsync(collection, ct);
            return CreatedAtAction(nameof(this.Get), new { id = createdCollection.Id }, createdCollection);
        }

        /// <summary>
        /// Updates a  Collection
        /// </summary>
        /// <remarks>
        /// Updates a Collection with the attributes specified.
        /// The ID from the route MUST MATCH the ID contained in the collection parameter
        /// <para />
        /// Accessible only to a ContentDeveloper or an Administrator
        /// </remarks>
        /// <param name="id">The Id of the Collection to update</param>
        /// <param name="collection">The updated Collection values</param>
        /// <param name="ct"></param>
        [HttpPut("collections/{id}")]
        [ProducesResponseType(typeof(Collection), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "updateCollection")]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] Collection collection, CancellationToken ct)
        {
            collection.ModifiedBy = User.GetId();
            var updatedCollection = await _collectionService.UpdateAsync(id, collection, ct);
            return Ok(updatedCollection);
        }

        /// <summary>
        /// Deletes a  Collection
        /// </summary>
        /// <remarks>
        /// Deletes a  Collection with the specified id
        /// <para />
        /// Accessible only to a ContentDeveloper or an Administrator
        /// </remarks>
        /// <param name="id">The id of the Collection to delete</param>
        /// <param name="ct"></param>
        [HttpDelete("collections/{id}")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [SwaggerOperation(OperationId = "deleteCollection")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            await _collectionService.DeleteAsync(id, ct);
            return NoContent();
        }

    }
}

