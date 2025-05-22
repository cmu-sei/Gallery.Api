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

public class CollectionPermissionsController : BaseController
{
    private readonly IGalleryAuthorizationService _authorizationService;

    public CollectionPermissionsController(IGalleryAuthorizationService authorizationService)
    {
        _authorizationService = authorizationService;
    }

    /// <summary>
    /// Get all SystemPermissions for the calling User.
    /// </summary>
    /// <returns></returns>
    [HttpGet("collection-permissions")]
    [ProducesResponseType(typeof(IEnumerable<CollectionPermissionClaim>), (int)HttpStatusCode.OK)]
    [SwaggerOperation(OperationId = "GetMyCollectionPermissions")]
    public async Task<IActionResult> GetMine()
    {
        var result = _authorizationService.GetCollectionPermissions();
        return Ok(result);
    }
}
