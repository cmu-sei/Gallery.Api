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
    public class UserPermissionController : BaseController
    {
        private readonly IUserPermissionService _userPermissionService;
        private readonly IAuthorizationService _authorizationService;

        public UserPermissionController(IUserPermissionService userPermissionService, IAuthorizationService authorizationService)
        {
            _userPermissionService = userPermissionService;
            _authorizationService = authorizationService;
        }

        /// <summary>
        /// Gets all UserPermissions in the system
        /// </summary>
        /// <remarks>
        /// Returns a list of all of the UserPermissions in the system.
        /// <para />
        /// Only accessible to a SuperUser
        /// </remarks>
        /// <returns></returns>
        [HttpGet("userpermissions")]
        [ProducesResponseType(typeof(IEnumerable<UserPermission>), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "getUserPermissions")]
        public async Task<IActionResult> Get(CancellationToken ct)
        {
            var list = await _userPermissionService.GetAsync(ct);
            return Ok(list);
        }

        /// <summary>
        /// Gets a specific UserPermission by id
        /// </summary>
        /// <remarks>
        /// Returns the UserPermission with the id specified
        /// <para />
        /// Only accessible to a SuperUser
        /// </remarks>
        /// <param name="id">The id of the UserPermission</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpGet("userpermissions/{id}")]
        [ProducesResponseType(typeof(UserPermission), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "getUserPermission")]
        public async Task<IActionResult> Get(Guid id, CancellationToken ct)
        {
            var permission = await _userPermissionService.GetAsync(id, ct);

            if (permission == null)
                throw new EntityNotFoundException<UserPermission>();

            return Ok(permission);
        }

        /// <summary>
        /// Creates a new UserPermission
        /// </summary>
        /// <remarks>
        /// Creates a new UserPermission with the attributes specified
        /// <para />
        /// Accessible only to a SuperUser
        /// </remarks>
        /// <param name="permission">The data to create the UserPermission with</param>
        /// <param name="ct"></param>
        [HttpPost("userpermissions")]
        [ProducesResponseType(typeof(UserPermission), (int)HttpStatusCode.Created)]
        [SwaggerOperation(OperationId = "createUserPermission")]
        public async Task<IActionResult> Create([FromBody] UserPermission permission, CancellationToken ct)
        {
            permission.CreatedBy = User.GetId();
            var createdUserPermission = await _userPermissionService.CreateAsync(permission, ct);
            return CreatedAtAction(nameof(this.Get), new { id = createdUserPermission.Id }, createdUserPermission);
        }

        /// <summary>
        /// Deletes a UserPermission
        /// </summary>
        /// <remarks>
        /// Deletes a UserPermission with the specified id
        /// <para />
        /// Accessible only to a SuperUser
        /// </remarks>
        /// <param name="id">The id of the UserPermission to delete</param>
        /// <param name="ct"></param>
        [HttpDelete("userpermissions/{id}")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [SwaggerOperation(OperationId = "deleteUserPermission")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            await _userPermissionService.DeleteAsync(id, ct);
            return NoContent();
        }

        /// <summary>
        /// Deletes a UserPermission by user ID and permission ID
        /// </summary>
        /// <remarks>
        /// Deletes a UserPermission with the specified user ID and permission ID
        /// <para />
        /// Accessible only to a SuperUser
        /// </remarks>
        /// <param name="userId">ID of a user.</param>
        /// <param name="permissionId">ID of a permission.</param>
        /// <param name="ct"></param>
        [HttpDelete("users/{userId}/permissions/{permissionId}")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [SwaggerOperation(OperationId = "deleteUserPermissionByIds")]
        public async Task<IActionResult> Delete(Guid userId, Guid permissionId, CancellationToken ct)
        {
            await _userPermissionService.DeleteByIdsAsync(userId, permissionId, ct);
            return NoContent();
        }

    }
}

