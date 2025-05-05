// Copyright 2024 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using Gallery.Api.Infrastructure.Exceptions;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Gallery.Api.Data.Enumerations;
using Gallery.Api.Infrastructure.Authorization;
using Gallery.Api.Services;
using Gallery.Api.ViewModels;
using System.Threading;
using System.Data;

namespace Gallery.Api.Controllers;

public class CollectionMembershipsController : BaseController
{
    private readonly IGalleryAuthorizationService _authorizationService;
    private readonly ICollectionMembershipService _collectionMembershipService;

    public CollectionMembershipsController(IGalleryAuthorizationService authorizationService, ICollectionMembershipService collectionMembershipService)
    {
        _authorizationService = authorizationService;
        _collectionMembershipService = collectionMembershipService;
    }

    /// <summary>
    /// Get a single CollectionMembership.
    /// </summary>
    /// <param name="id">ID of a CollectionMembership.</param>
    /// <returns></returns>
    [HttpGet("collections/memberships/{id}")]
    [ProducesResponseType(typeof(CollectionMembership), (int)HttpStatusCode.OK)]
    [SwaggerOperation(OperationId = "GetCollectionMembership")]
    public async Task<IActionResult> Get([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _collectionMembershipService.GetAsync(id, ct);
        if (!await _authorizationService.AuthorizeAsync<Collection>(result.CollectionId, [SystemPermission.ViewCollections], [CollectionPermission.ViewCollection], ct))
            throw new ForbiddenException();

        return Ok(result);
    }

    /// <summary>
    /// Get all CollectionMemberships.
    /// </summary>
    /// <returns></returns>
    [HttpGet("collections/{id}/memberships")]
    [ProducesResponseType(typeof(IEnumerable<CollectionMembership>), (int)HttpStatusCode.OK)]
    [SwaggerOperation(OperationId = "GetAllCollectionMemberships")]
    public async Task<IActionResult> GetAll(Guid id, CancellationToken ct)
    {
        if (!await _authorizationService.AuthorizeAsync<Collection>(id, [SystemPermission.ViewCollections], [CollectionPermission.ViewCollection], ct))
            throw new ForbiddenException();

        var result = await _collectionMembershipService.GetByCollectionAsync(id, ct);
        return Ok(result);
    }

    /// <summary>
    /// Create a new Collection Membership.
    /// </summary>
    /// <param name="collectionId"></param>
    /// <param name="collectionMembership"></param>
    /// <returns></returns>
    [HttpPost("collections/{collectionId}/memberships")]
    [ProducesResponseType(typeof(CollectionMembership), (int)HttpStatusCode.Created)]
    [SwaggerOperation(OperationId = "CreateCollectionMembership")]
    public async Task<IActionResult> CreateMembership([FromRoute] Guid collectionId, CollectionMembership collectionMembership, CancellationToken ct)
    {
        if (!await _authorizationService.AuthorizeAsync<Collection>(collectionId, [SystemPermission.ManageCollections], [CollectionPermission.ManageCollection], ct))
            throw new ForbiddenException();

        if (collectionMembership.CollectionId != collectionId)
            throw new DataException("The CollectionId of the membership must match the CollectionId of the URL.");

        var result = await _collectionMembershipService.CreateAsync(collectionMembership, ct);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    /// <summary>
    /// Updates a CollectionMembership
    /// </summary>
    /// <remarks>
    /// Updates a CollectionMembership with the attributes specified
    /// </remarks>
    /// <param name="id">The Id of the Exericse to update</param>
    /// <param name="collectionMembership">The updated CollectionMembership values</param>
    /// <param name="ct"></param>
    [HttpPut("Collections/Memberships/{id}")]
    [ProducesResponseType(typeof(CollectionMembership), (int)HttpStatusCode.OK)]
    [SwaggerOperation(OperationId = "updateCollectionMembership")]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] CollectionMembership collectionMembership, CancellationToken ct)
    {
        var membership = await _collectionMembershipService.GetAsync(id, ct);
        if (!await _authorizationService.AuthorizeAsync<Collection>(membership.CollectionId, [SystemPermission.ManageCollections], [CollectionPermission.ManageCollection], ct))
            throw new ForbiddenException();

        var updatedCollectionMembership = await _collectionMembershipService.UpdateAsync(id, collectionMembership, ct);
        return Ok(updatedCollectionMembership);
    }

    /// <summary>
    /// Delete a Collection Membership.
    /// </summary>
    /// <returns></returns>
    [HttpDelete("collections/memberships/{id}")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [SwaggerOperation(OperationId = "DeleteCollectionMembership")]
    public async Task<IActionResult> DeleteMembership([FromRoute] Guid id, CancellationToken ct)
    {
        var membership = await _collectionMembershipService.GetAsync(id, ct);
        if (!await _authorizationService.AuthorizeAsync<Collection>(membership.CollectionId, [SystemPermission.ManageCollections], [CollectionPermission.ManageCollection], ct))
            throw new ForbiddenException();

        await _collectionMembershipService.DeleteAsync(id, ct);
        return NoContent();
    }

}
