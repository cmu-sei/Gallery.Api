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
    public class TeamCardController : BaseController
    {
        private readonly ITeamService _teamService;
        private readonly ITeamCardService _teamCardService;
        private readonly IGalleryAuthorizationService _authorizationService;

        public TeamCardController(
            ITeamService teamService,
            ITeamCardService teamCardService,
            IGalleryAuthorizationService authorizationService)
        {
            _teamService = teamService;
            _teamCardService = teamCardService;
            _authorizationService = authorizationService;
        }

        /// <summary>
        /// Gets all TeamCards in the system
        /// </summary>
        /// <remarks>
        /// Returns a list of all of the TeamCards in the system.
        /// </remarks>
        /// <returns></returns>
        [HttpGet("teamcards")]
        [ProducesResponseType(typeof(IEnumerable<TeamCard>), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "getTeamCards")]
        public async Task<IActionResult> Get(CancellationToken ct)
        {
            if (!await _authorizationService.AuthorizeAsync([SystemPermission.ManageExhibits], ct))
                throw new ForbiddenException();

            var list = await _teamCardService.GetAsync(ct);
            return Ok(list);
        }

        /// <summary>
        /// Gets all TeamCards for an exhibit
        /// </summary>
        /// <remarks>
        /// Returns a list of all of the TeamCards for the exhibit.
        /// </remarks>
        /// <param name="exhibitId">The id of the Exhibit</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpGet("exhibits/{exhibitId}/teamcards")]
        [ProducesResponseType(typeof(IEnumerable<TeamCard>), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "getExhibitTeamCards")]
        public async Task<IActionResult> GetByExhibit(Guid exhibitId, CancellationToken ct)
        {
            if (!await _authorizationService.AuthorizeAsync<Exhibit>(exhibitId, [SystemPermission.ManageExhibits], [ExhibitPermission.ManageExhibit], ct))
                throw new ForbiddenException();

            var list = await _teamCardService.GetByExhibitAsync(exhibitId, ct);
            return Ok(list);
        }

        /// <summary>
        /// Gets all TeamCards for an exhibit team
        /// </summary>
        /// <remarks>
        /// Returns a list of all of the TeamCards for the exhibit team.
        /// </remarks>
        /// <param name="exhibitId">The id of the Exhibit</param>
        /// <param name="teamId">The id of the Team</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpGet("exhibits/{exhibitId}/teams/{teamId}/teamcards")]
        [ProducesResponseType(typeof(IEnumerable<TeamCard>), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "getByExhibitTeam")]
        public async Task<IActionResult> GetByExhibitTeam(Guid exhibitId, Guid teamId, CancellationToken ct)
        {
            if (!await _authorizationService.AuthorizeAsync<Team>(teamId, [TeamPermission.ViewTeam], ct))
                throw new ForbiddenException();

            var list = await _teamCardService.GetByExhibitTeamAsync(exhibitId, teamId, ct);
            return Ok(list);
        }

        /// <summary>
        /// Gets a specific TeamCard by id
        /// </summary>
        /// <remarks>
        /// Returns the TeamCard with the id specified
        /// </remarks>
        /// <param name="id">The id of the TeamCard</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpGet("teamcards/{id}")]
        [ProducesResponseType(typeof(TeamCard), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "getTeamCard")]
        public async Task<IActionResult> Get(Guid id, CancellationToken ct)
        {
            var teamCard = await _teamCardService.GetAsync(id, ct);

            if (teamCard == null)
                throw new EntityNotFoundException<TeamCard>();

            if (!await _authorizationService.AuthorizeAsync<Team>(teamCard.TeamId, [TeamPermission.ViewTeam], ct))
                throw new ForbiddenException();

            return Ok(teamCard);
        }

        /// <summary>
        /// Creates a new TeamCard
        /// </summary>
        /// <remarks>
        /// Creates a new TeamCard with the attributes specified
        /// <para />
        /// Accessible only to a SuperCard
        /// </remarks>
        /// <param name="teamCard">The data to create the TeamCard with</param>
        /// <param name="ct"></param>
        [HttpPost("teamcards")]
        [ProducesResponseType(typeof(TeamCard), (int)HttpStatusCode.Created)]
        [SwaggerOperation(OperationId = "createTeamCard")]
        public async Task<IActionResult> Create([FromBody] TeamCard teamCard, CancellationToken ct)
        {
            var team = await _teamService.GetAsync(teamCard.TeamId, ct);
            if (!await _authorizationService.AuthorizeAsync<Exhibit>(team.ExhibitId, [SystemPermission.ManageExhibits], [ExhibitPermission.ManageExhibit], ct))
                throw new ForbiddenException();

            var createdTeamCard = await _teamCardService.CreateAsync(teamCard, ct);
            return CreatedAtAction(nameof(this.Get), new { id = createdTeamCard.Id }, createdTeamCard);
        }

        /// <summary>
        /// Updates an TeamCard
        /// </summary>
        /// <remarks>
        /// Updates a TeamCard with the attributes specified.
        /// The ID from the route MUST MATCH the ID contained in the teamCard parameter
        /// <para />
        /// Accessible only to a ContentDeveloper or an Administrator
        /// </remarks>
        /// <param name="id">The Id of the TeamCard to update</param>
        /// <param name="teamCard">The updated TeamCard values</param>
        /// <param name="ct"></param>
        [HttpPut("teamCards/{id}")]
        [ProducesResponseType(typeof(TeamCard), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "updateTeamCard")]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] TeamCard teamCard, CancellationToken ct)
        {
            var team = await _teamService.GetAsync(teamCard.TeamId, ct);
            if (!await _authorizationService.AuthorizeAsync<Exhibit>(team.ExhibitId, [SystemPermission.ManageExhibits], [ExhibitPermission.ManageExhibit], ct))
                throw new ForbiddenException();

            var updatedTeamCard = await _teamCardService.UpdateAsync(id, teamCard, ct);
            return Ok(updatedTeamCard);
        }

        /// <summary>
        /// Deletes a TeamCard
        /// </summary>
        /// <remarks>
        /// Deletes a TeamCard with the specified id
        /// <para />
        /// Accessible only to a SuperCard
        /// </remarks>
        /// <param name="id">The id of the TeamCard to delete</param>
        /// <param name="ct"></param>
        [HttpDelete("teamcards/{id}")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [SwaggerOperation(OperationId = "deleteTeamCard")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var teamCard = await _teamCardService.GetAsync(id, ct);
            var team = await _teamService.GetAsync(teamCard.TeamId, ct);
            if (!await _authorizationService.AuthorizeAsync<Exhibit>(team.ExhibitId, [SystemPermission.ManageExhibits], [ExhibitPermission.ManageExhibit], ct))
                throw new ForbiddenException();

            await _teamCardService.DeleteAsync(id, ct);
            return NoContent();
        }

        /// <summary>
        /// Deletes a TeamCard by card ID and team ID
        /// </summary>
        /// <remarks>
        /// Deletes a TeamCard with the specified card ID and team ID
        /// <para />
        /// Accessible only to a SuperCard
        /// </remarks>
        /// <param name="cardId">ID of a card.</param>
        /// <param name="teamId">ID of a team.</param>
        /// <param name="ct"></param>
        [HttpDelete("teams/{teamId}/cards/{cardId}")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [SwaggerOperation(OperationId = "deleteTeamCardByIds")]
        public async Task<IActionResult> Delete(Guid teamId, Guid cardId, CancellationToken ct)
        {
            var team = await _teamService.GetAsync(teamId, ct);
            if (!await _authorizationService.AuthorizeAsync<Exhibit>(team.ExhibitId, [SystemPermission.ManageExhibits], [ExhibitPermission.ManageExhibit], ct))
                throw new ForbiddenException();

            await _teamCardService.DeleteByIdsAsync(teamId, cardId, ct);
            return NoContent();
        }

    }
}
