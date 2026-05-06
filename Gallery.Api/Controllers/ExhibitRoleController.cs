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

public class ExhibitRolesController : BaseController
{
    private readonly IGalleryAuthorizationService _authorizationService;
    private readonly IExhibitRoleService _exhibitRoleService;

    public ExhibitRolesController(IGalleryAuthorizationService authorizationService, IExhibitRoleService exhibitRoleService)
    {
        _authorizationService = authorizationService;
        _exhibitRoleService = exhibitRoleService;
    }

    /// <summary>
    /// Get a single ExhibitRole.
    /// </summary>
    /// <param name="id">ID of a ExhibitRole.</param>
    /// <returns></returns>
    [HttpGet("exhibit-roles/{id}")]
    [ProducesResponseType(typeof(ExhibitRole), (int)HttpStatusCode.OK)]
    [SwaggerOperation(OperationId = "GetExhibitRole")]
    public async Task<IActionResult> Get([FromRoute] Guid id, CancellationToken ct)
    {
        if (!await _authorizationService.AuthorizeAsync([SystemPermission.ViewRoles], ct))
            throw new ForbiddenException();

        var result = await _exhibitRoleService.GetAsync(id, ct);
        return Ok(result);
    }

    /// <summary>
    /// Get all ExhibitRoles.
    /// </summary>
    /// <returns></returns>
    [HttpGet("exhibit-roles")]
    [ProducesResponseType(typeof(IEnumerable<ExhibitRole>), (int)HttpStatusCode.OK)]
    [SwaggerOperation(OperationId = "GetAllExhibitRoles")]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        if (!await _authorizationService.AuthorizeAsync([SystemPermission.ViewRoles], ct))
            throw new ForbiddenException();

        var result = await _exhibitRoleService.GetAsync(ct);
        return Ok(result);
    }
}
