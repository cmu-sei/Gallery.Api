// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
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

namespace Gallery.Api.Controllers;

public class CollectionRolesController : BaseController
{
    private readonly IGalleryAuthorizationService _authorizationService;
    private readonly ICollectionRoleService _eventRoleService;

    public CollectionRolesController(IGalleryAuthorizationService authorizationService, ICollectionRoleService eventRoleService)
    {
        _authorizationService = authorizationService;
        _eventRoleService = eventRoleService;
    }

    /// <summary>
    /// Get a single CollectionRole.
    /// </summary>
    /// <param name="id">ID of a CollectionRole.</param>
    /// <returns></returns>
    [HttpGet("collection-roles/{id}")]
    [ProducesResponseType(typeof(CollectionRole), (int)HttpStatusCode.OK)]
    [SwaggerOperation(OperationId = "GetCollectionRole")]
    public async Task<IActionResult> Get([FromRoute] Guid id, CancellationToken ct)
    {
        if (!await _authorizationService.AuthorizeAsync([SystemPermission.ViewRoles], ct))
            throw new ForbiddenException();

        var result = await _eventRoleService.GetAsync(id, ct);
        return Ok(result);
    }

    /// <summary>
    /// Get all CollectionRoles.
    /// </summary>
    /// <returns></returns>
    [HttpGet("collection-roles")]
    [ProducesResponseType(typeof(IEnumerable<CollectionRole>), (int)HttpStatusCode.OK)]
    [SwaggerOperation(OperationId = "GetAllCollectionRoles")]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        if (!await _authorizationService.AuthorizeAsync([SystemPermission.ViewRoles], ct))
            throw new ForbiddenException();

        var result = await _eventRoleService.GetAsync(ct);
        return Ok(result);
    }
}
