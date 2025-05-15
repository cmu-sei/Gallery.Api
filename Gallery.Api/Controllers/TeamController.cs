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
using Gallery.Api.Infrastructure.Extensions;
using Gallery.Api.Infrastructure.Exceptions;
using Gallery.Api.Services;
using Gallery.Api.ViewModels;
using Swashbuckle.AspNetCore.Annotations;

namespace Gallery.Api.Controllers
{
    public class TeamController : BaseController
    {
        private readonly ITeamService _teamService;
        private readonly IGalleryAuthorizationService _authorizationService;

        public TeamController(ITeamService teamService, IGalleryAuthorizationService authorizationService)
        {
            _teamService = teamService;
            _authorizationService = authorizationService;
        }

        /// <summary>
        /// Gets Exhibit Teams for the current user
        /// </summary>
        /// <remarks>
        /// Returns a list of the current user's Exhibit Teams.
        /// </remarks>
        /// <returns></returns>
        [HttpGet("exhibits/{exhibitId}/my-teams")]
        [ProducesResponseType(typeof(IEnumerable<Team>), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "getMyExhibitTeams")]
        public async Task<IActionResult> GetMineByExhibit(Guid exhibitId, CancellationToken ct)
        {
            var list = await _teamService.GetMineByExhibitAsync(exhibitId, ct);
            return Ok(list);
        }

        /// <summary>
        /// Gets Teams for the specified exhibit
        /// </summary>
        /// <remarks>
        /// Returns a list of the specified exhibit's Teams.
        /// </remarks>
        /// <returns></returns>
        [HttpGet("exhibits/{exhibitId}/teams")]
        [ProducesResponseType(typeof(IEnumerable<Team>), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "getTeamsByExhibit")]
        public async Task<IActionResult> GetByExhibit([FromRoute] Guid exhibitId, CancellationToken ct)
        {
            var checkForTeamMembership = false;
            if (!await _authorizationService.AuthorizeAsync<Exhibit>(exhibitId, [SystemPermission.EditExhibits], [ExhibitPermission.EditExhibit], ct))
                checkForTeamMembership = true;

            var list = await _teamService.GetByExhibitAsync(exhibitId, checkForTeamMembership, ct);
            return Ok(list);
        }

        /// <summary>
        /// Gets a specific Team by id
        /// </summary>
        /// <remarks>
        /// Returns the Team with the id specified
        /// <para />
        /// Accessible to a SuperUser or a User that is a member of a Team within the specified Team
        /// </remarks>
        /// <param name="id">The id of the Team</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpGet("teams/{id}")]
        [ProducesResponseType(typeof(Team), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "getTeam")]
        public async Task<IActionResult> Get(Guid id, CancellationToken ct)
        {
            if (!await _authorizationService.AuthorizeAsync<Team>(id, [TeamPermission.ViewTeam], ct))
                throw new ForbiddenException();

            var team = await _teamService.GetAsync(id, ct);
            if (team == null)
                throw new EntityNotFoundException<Team>();

            return Ok(team);
        }

        /// <summary>
        /// Creates a new Team
        /// </summary>
        /// <remarks>
        /// Creates a new Team with the attributes specified
        /// <para />
        /// Accessible only to a SuperUser or an Administrator
        /// </remarks>
        /// <param name="team">The data to create the Team with</param>
        /// <param name="ct"></param>
        [HttpPost("teams")]
        [ProducesResponseType(typeof(Team), (int)HttpStatusCode.Created)]
        [SwaggerOperation(OperationId = "createTeam")]
        public async Task<IActionResult> Create([FromBody] Team team, CancellationToken ct)
        {
            if (!await _authorizationService.AuthorizeAsync<Exhibit>(team.ExhibitId, [SystemPermission.EditExhibits], [ExhibitPermission.EditExhibit], ct))
                throw new ForbiddenException();

            var createdTeam = await _teamService.CreateAsync(team, ct);
            return CreatedAtAction(nameof(this.Get), new { id = createdTeam.Id }, createdTeam);
        }

        /// <summary>
        /// Updates a Team
        /// </summary>
        /// <remarks>
        /// Updates a Team with the attributes specified
        /// </remarks>
        /// <param name="id">The Id of the Exericse to update</param>
        /// <param name="team">The updated Team values</param>
        /// <param name="ct"></param>
        [HttpPut("teams/{id}")]
        [ProducesResponseType(typeof(Team), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "updateTeam")]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] Team team, CancellationToken ct)
        {
            if (!await _authorizationService.AuthorizeAsync<Exhibit>(team.ExhibitId, [SystemPermission.EditExhibits], [ExhibitPermission.EditExhibit], ct))
                throw new ForbiddenException();

            var updatedTeam = await _teamService.UpdateAsync(id, team, ct);
            return Ok(updatedTeam);
        }

        /// <summary>
        /// Deletes a Team
        /// </summary>
        /// <remarks>
        /// Deletes a Team with the specified id
        /// <para />
        /// Accessible only to a SuperUser or a User on an Admin Team within the specified Team
        /// </remarks>
        /// <param name="id">The id of the Team to delete</param>
        /// <param name="ct"></param>
        [HttpDelete("teams/{id}")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [SwaggerOperation(OperationId = "deleteTeam")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var team = await _teamService.GetAsync(id, ct);
            if (!await _authorizationService.AuthorizeAsync<Exhibit>(team.ExhibitId, [SystemPermission.EditExhibits], [ExhibitPermission.EditExhibit], ct))
                throw new ForbiddenException();

            await _teamService.DeleteAsync(id, ct);
            return NoContent();
        }

    }
}
