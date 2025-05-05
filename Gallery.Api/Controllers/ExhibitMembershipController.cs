// Copyright 2024 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using Gallery.Api.Infrastructure.Exceptions;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Gallery.Api.Data;
using Gallery.Api.Infrastructure.Authorization;
using Gallery.Api.Services;
using Gallery.Api.ViewModels;
using System.Threading;

namespace Gallery.Api.Controllers;

public class ExhibitMembershipsController : BaseController
{
    private readonly IGalleryAuthorizationService _authorizationService;
    private readonly IExhibitMembershipService _exhibitMembershipService;

    public ExhibitMembershipsController(IGalleryAuthorizationService authorizationService, IExhibitMembershipService exhibitMembershipService)
    {
        _authorizationService = authorizationService;
        _exhibitMembershipService = exhibitMembershipService;
    }

    /// <summary>
    /// Get a single ExhibitMembership.
    /// </summary>
    /// <param name="id">ID of a ExhibitMembership.</param>
    /// <returns></returns>
    [HttpGet("exhibits/memberships/{id}")]
    [ProducesResponseType(typeof(ExhibitMembership), (int)HttpStatusCode.OK)]
    [SwaggerOperation(OperationId = "GetExhibitMembership")]
    public async Task<IActionResult> Get([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _exhibitMembershipService.GetAsync(id, ct);
        if (!await _authorizationService.AuthorizeAsync<Exhibit>(result.ExhibitId, [SystemPermission.ViewExhibits], [ExhibitPermission.ViewExhibit], ct))
            throw new ForbiddenException();

        return Ok(result);
    }

    /// <summary>
    /// Get all ExhibitMemberships.
    /// </summary>
    /// <returns></returns>
    [HttpGet("exhibits/{id}/memberships")]
    [ProducesResponseType(typeof(IEnumerable<ExhibitMembership>), (int)HttpStatusCode.OK)]
    [SwaggerOperation(OperationId = "GetAllExhibitMemberships")]
    public async Task<IActionResult> GetAll(Guid id, CancellationToken ct)
    {
        if (!await _authorizationService.AuthorizeAsync<Exhibit>(id, [SystemPermission.ViewExhibits], [ExhibitPermission.ViewExhibit], ct))
            throw new ForbiddenException();

        var result = await _exhibitMembershipService.GetByExhibitAsync(id, ct);
        return Ok(result);
    }

    /// <summary>
    /// Create a new Exhibit Membership.
    /// </summary>
    /// <param name="exhibitId"></param>
    /// <param name="exhibitMembership"></param>
    /// <returns></returns>
    [HttpPost("exhibits/{exhibitId}/memberships")]
    [ProducesResponseType(typeof(ExhibitMembership), (int)HttpStatusCode.Created)]
    [SwaggerOperation(OperationId = "CreateExhibitMembership")]
    public async Task<IActionResult> CreateMembership([FromRoute] Guid exhibitId, ExhibitMembership exhibitMembership, CancellationToken ct)
    {
        if (!await _authorizationService.AuthorizeAsync<Exhibit>(exhibitMembership.ExhibitId, [SystemPermission.ManageExhibits], [ExhibitPermission.ManageExhibit], ct))
            throw new ForbiddenException();

        var result = await _exhibitMembershipService.CreateAsync(exhibitMembership, ct);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    /// <summary>
    /// Updates a ExhibitMembership
    /// </summary>
    /// <remarks>
    /// Updates a ExhibitMembership with the attributes specified
    /// </remarks>
    /// <param name="id">The Id of the Exericse to update</param>
    /// <param name="exhibitMembership">The updated ExhibitMembership values</param>
    /// <param name="ct"></param>
    [HttpPut("Exhibits/Memberships/{id}")]
    [ProducesResponseType(typeof(ExhibitMembership), (int)HttpStatusCode.OK)]
    [SwaggerOperation(OperationId = "updateExhibitMembership")]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] ExhibitMembership exhibitMembership, CancellationToken ct)
    {
        if (!await _authorizationService.AuthorizeAsync<Exhibit>(exhibitMembership.ExhibitId, [SystemPermission.ManageExhibits], [ExhibitPermission.ManageExhibit], ct))
            throw new ForbiddenException();

        var updatedExhibitMembership = await _exhibitMembershipService.UpdateAsync(id, exhibitMembership, ct);
        return Ok(updatedExhibitMembership);
    }

    /// <summary>
    /// Delete a Exhibit Membership.
    /// </summary>
    /// <returns></returns>
    [HttpDelete("exhibits/memberships/{id}")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [SwaggerOperation(OperationId = "DeleteExhibitMembership")]
    public async Task<IActionResult> DeleteMembership([FromRoute] Guid id, CancellationToken ct)
    {
        var exhibitMembership = await _exhibitMembershipService.GetAsync(id, ct);
        if (!await _authorizationService.AuthorizeAsync<Exhibit>(exhibitMembership.ExhibitId, [SystemPermission.ManageExhibits], [ExhibitPermission.ManageExhibit], ct))
            throw new ForbiddenException();

        await _exhibitMembershipService.DeleteAsync(id, ct);
        return NoContent();
    }


}
