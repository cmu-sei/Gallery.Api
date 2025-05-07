// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Gallery.Api.Data.Enumerations;
using Gallery.Api.Infrastructure.Authorization;
using Gallery.Api.Infrastructure.Exceptions;
using Gallery.Api.Services;
using Gallery.Api.ViewModels;
using Swashbuckle.AspNetCore.Annotations;

namespace Gallery.Api.Controllers
{
    public class TeamUserController : BaseController
    {
        private readonly ITeamService _teamService;
        private readonly ITeamUserService _teamUserService;
        private readonly IGalleryAuthorizationService _authorizationService;

        public TeamUserController(
            ITeamService teamService,
            ITeamUserService teamUserService,
            IGalleryAuthorizationService authorizationService)
        {
            _teamService = teamService;
            _teamUserService = teamUserService;
            _authorizationService = authorizationService;
        }

        /// <summary>
        /// Gets TeamUsers for the specified exhibit
        /// </summary>
        /// <remarks>
        /// Returns a list of the specified exhibit's TeamUsers.
        /// <para />
        /// Only accessible to an exhibit user
        /// </remarks>
        /// <returns></returns>
        [HttpGet("exhibits/{exhibitId}/teamusers")]
        [ProducesResponseType(typeof(IEnumerable<TeamUser>), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "getExhibitTeamUsers")]
        public async Task<IActionResult> GetByExhibit([FromRoute] Guid exhibitId, CancellationToken ct)
        {
            if (!await _authorizationService.AuthorizeAsync<Exhibit>(exhibitId, [SystemPermission.ViewExhibits], [ExhibitPermission.ViewExhibit], ct))
                throw new ForbiddenException();

            var list = await _teamUserService.GetByExhibitAsync(exhibitId, ct);
            return Ok(list);
        }

        /// <summary>
        /// Gets TeamUsers for the specified team
        /// </summary>
        /// <remarks>
        /// Returns a list of the specified team's TeamUsers.
        /// <para />
        /// Only accessible to an exhibit user
        /// </remarks>
        /// <returns></returns>
        [HttpGet("teams/{teamId}/teamusers")]
        [ProducesResponseType(typeof(IEnumerable<TeamUser>), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "getTeamTeamUsers")]
        public async Task<IActionResult> GetByTeam([FromRoute] Guid teamId, CancellationToken ct)
        {
            if (!await _authorizationService.AuthorizeAsync<Team>(teamId, [TeamPermission.ViewTeam], ct))
                throw new ForbiddenException();

            var list = await _teamUserService.GetByTeamAsync(teamId, ct);
            return Ok(list);
        }

        /// <summary>
        /// Gets a specific TeamUser by id
        /// </summary>
        /// <remarks>
        /// Returns the TeamUser with the id specified
        /// </remarks>
        /// <param name="id">The id of the TeamUser</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpGet("teamusers/{id}")]
        [ProducesResponseType(typeof(TeamUser), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "getTeamUser")]
        public async Task<IActionResult> Get(Guid id, CancellationToken ct)
        {
            var teamUser = await _teamUserService.GetAsync(id, ct);
            if (!await _authorizationService.AuthorizeAsync<Team>(teamUser.TeamId, [TeamPermission.ViewTeam], ct))
                throw new ForbiddenException();

            if (teamUser == null)
                throw new EntityNotFoundException<TeamUser>();

            return Ok(teamUser);
        }

        /// <summary>
        /// Creates a new TeamUser
        /// </summary>
        /// <remarks>
        /// Creates a new TeamUser with the attributes specified
        /// <para />
        /// Accessible only to a SuperUser
        /// </remarks>
        /// <param name="teamUser">The data to create the TeamUser with</param>
        /// <param name="ct"></param>
        [HttpPost("teamusers")]
        [ProducesResponseType(typeof(TeamUser), (int)HttpStatusCode.Created)]
        [SwaggerOperation(OperationId = "createTeamUser")]
        public async Task<IActionResult> Create([FromBody] TeamUser teamUser, CancellationToken ct)
        {
            var team = await _teamService.GetAsync(teamUser.TeamId, ct);
            if (!await _authorizationService.AuthorizeAsync<Exhibit>(team.ExhibitId, [SystemPermission.ViewExhibits], [ExhibitPermission.ViewExhibit], ct))
                throw new ForbiddenException();

            var createdTeamUser = await _teamUserService.CreateAsync(teamUser, ct);
            return CreatedAtAction(nameof(this.Get), new { id = createdTeamUser.Id }, createdTeamUser);
        }

        /// <summary>
        /// Sets the selected TeamUser observer flag
        /// </summary>
        /// <remarks>
        /// Sets the TeamUser to an observer.
        /// </remarks>
        /// <param name="id">The Id of the TeamUser to update</param>
        /// <param name="ct"></param>
        [HttpPut("teamusers/{id}/observer/set")]
        [ProducesResponseType(typeof(TeamUser), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "setObserver")]
        public async Task<IActionResult> SetObserver([FromRoute] Guid id, CancellationToken ct)
        {
            var teamUser = await _teamUserService.GetAsync(id, ct);
            var team = await _teamService.GetAsync(teamUser.TeamId, ct);
            if (!await _authorizationService.AuthorizeAsync<Exhibit>(team.ExhibitId, [SystemPermission.EditExhibits], [ExhibitPermission.EditExhibit], ct))
                throw new ForbiddenException();

            var result = await _teamUserService.SetObserverAsync(id, true, ct);
            return Ok(result);
        }

        /// <summary>
        /// Clears the selected TeamUser observer flag
        /// </summary>
        /// <remarks>
        /// Clears the TeamUser from being an observer.
        /// </remarks>
        /// <param name="id">The Id of the TeamUser to update</param>
        /// <param name="ct"></param>
        [HttpPut("teamusers/{id}/observer/clear")]
        [ProducesResponseType(typeof(TeamUser), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "clearObserver")]
        public async Task<IActionResult> ClearObserver([FromRoute] Guid id, CancellationToken ct)
        {
            var teamUser = await _teamUserService.GetAsync(id, ct);
            var team = await _teamService.GetAsync(teamUser.TeamId, ct);
            if (!await _authorizationService.AuthorizeAsync<Exhibit>(team.ExhibitId, [SystemPermission.EditExhibits], [ExhibitPermission.EditExhibit], ct))
                throw new ForbiddenException();

            var result = await _teamUserService.SetObserverAsync(id, false, ct);
            return Ok(result);
        }

        /// <summary>
        /// Deletes a TeamUser
        /// </summary>
        /// <remarks>
        /// Deletes a TeamUser with the specified id
        /// <para />
        /// Accessible only to a SuperUser
        /// </remarks>
        /// <param name="id">The id of the TeamUser to delete</param>
        /// <param name="ct"></param>
        [HttpDelete("teamusers/{id}")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [SwaggerOperation(OperationId = "deleteTeamUser")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var teamUser = await _teamUserService.GetAsync(id, ct);
            var team = await _teamService.GetAsync(teamUser.TeamId, ct);
            if (!await _authorizationService.AuthorizeAsync<Exhibit>(team.ExhibitId, [SystemPermission.EditExhibits], [ExhibitPermission.EditExhibit], ct))
                throw new ForbiddenException();

            await _teamUserService.DeleteAsync(id, ct);
            return NoContent();
        }

        /// <summary>
        /// Deletes a TeamUser by user ID and team ID
        /// </summary>
        /// <remarks>
        /// Deletes a TeamUser with the specified user ID and team ID
        /// <para />
        /// Accessible only to a SuperUser
        /// </remarks>
        /// <param name="userId">ID of a user.</param>
        /// <param name="teamId">ID of a team.</param>
        /// <param name="ct"></param>
        [HttpDelete("teams/{teamId}/users/{userId}")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [SwaggerOperation(OperationId = "deleteTeamUserByIds")]
        public async Task<IActionResult> Delete(Guid teamId, Guid userId, CancellationToken ct)
        {
            var team = await _teamService.GetAsync(teamId, ct);
            if (!await _authorizationService.AuthorizeAsync<Exhibit>(team.ExhibitId, [SystemPermission.EditExhibits], [ExhibitPermission.EditExhibit], ct))
                throw new ForbiddenException();

            await _teamUserService.DeleteByIdsAsync(teamId, userId, ct);
            return NoContent();
        }

    }
}
