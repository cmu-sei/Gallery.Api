// Copyright 2024 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Gallery.Api.Infrastructure.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System;

namespace Gallery.Api.Controllers;

public class ExhibitPermissionsController : BaseController
{
    private readonly IGalleryAuthorizationService _authorizationService;

    public ExhibitPermissionsController(IGalleryAuthorizationService authorizationService)
    {
        _authorizationService = authorizationService;
    }

    /// <summary>
    /// Get all SystemPermissions for the calling User.
    /// </summary>
    /// <returns></returns>
    [HttpGet("exhibit-permissions")]
    [ProducesResponseType(typeof(IEnumerable<ExhibitPermissionClaim>), (int)HttpStatusCode.OK)]
    [SwaggerOperation(OperationId = "GetMyExhibitPermissions")]
    public async Task<IActionResult> GetMine()
    {
        var result = _authorizationService.GetExhibitPermissions();
        return Ok(result);
    }
}
