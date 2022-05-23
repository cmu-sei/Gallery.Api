// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Api.Infrastructure.Extensions;
using Api.Infrastructure.Exceptions;
using Api.Services;
using Api.ViewModels;
using Swashbuckle.AspNetCore.Annotations;

namespace Api.Controllers
{
    public class ExhibitTeamController : BaseController
    {
        private readonly IExhibitTeamService _exhibitTeamService;
        private readonly IAuthorizationService _authorizationService;

        public ExhibitTeamController(IExhibitTeamService exhibitTeamService, IAuthorizationService authorizationService)
        {
            _exhibitTeamService = exhibitTeamService;
            _authorizationService = authorizationService;
        }

        /// <summary>
        /// Gets all ExhibitTeams in the system
        /// </summary>
        /// <remarks>
        /// Returns a list of all of the ExhibitTeams in the system.
        /// <para />
        /// Only accessible to a SuperTeam
        /// </remarks>
        /// <returns></returns>
        [HttpGet("exhibitteams")]
        [ProducesResponseType(typeof(IEnumerable<ExhibitTeam>), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "getExhibitTeams")]
        public async Task<IActionResult> Get(CancellationToken ct)
        {
            var list = await _exhibitTeamService.GetAsync(ct);
            return Ok(list);
        }

        /// <summary>
        /// Gets a specific ExhibitTeam by id
        /// </summary>
        /// <remarks>
        /// Returns the ExhibitTeam with the id specified
        /// <para />
        /// Only accessible to a SuperTeam
        /// </remarks>
        /// <param name="id">The id of the ExhibitTeam</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpGet("exhibitteams/{id}")]
        [ProducesResponseType(typeof(ExhibitTeam), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "getExhibitTeam")]
        public async Task<IActionResult> Get(Guid id, CancellationToken ct)
        {
            var exhibit = await _exhibitTeamService.GetAsync(id, ct);

            if (exhibit == null)
                throw new EntityNotFoundException<ExhibitTeam>();

            return Ok(exhibit);
        }

        /// <summary>
        /// Creates a new ExhibitTeam
        /// </summary>
        /// <remarks>
        /// Creates a new ExhibitTeam with the attributes specified
        /// <para />
        /// Accessible only to a SuperTeam
        /// </remarks>
        /// <param name="exhibit">The data to create the ExhibitTeam with</param>
        /// <param name="ct"></param>
        [HttpPost("exhibitteams")]
        [ProducesResponseType(typeof(ExhibitTeam), (int)HttpStatusCode.Created)]
        [SwaggerOperation(OperationId = "createExhibitTeam")]
        public async Task<IActionResult> Create([FromBody] ExhibitTeam exhibit, CancellationToken ct)
        {
            exhibit.CreatedBy = User.GetId();
            var createdExhibitTeam = await _exhibitTeamService.CreateAsync(exhibit, ct);
            return CreatedAtAction(nameof(this.Get), new { id = createdExhibitTeam.Id }, createdExhibitTeam);
        }

        /// <summary>
        /// Deletes a ExhibitTeam
        /// </summary>
        /// <remarks>
        /// Deletes a ExhibitTeam with the specified id
        /// <para />
        /// Accessible only to a SuperTeam
        /// </remarks>
        /// <param name="id">The id of the ExhibitTeam to delete</param>
        /// <param name="ct"></param>
        [HttpDelete("exhibitteams/{id}")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [SwaggerOperation(OperationId = "deleteExhibitTeam")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            await _exhibitTeamService.DeleteAsync(id, ct);
            return NoContent();
        }

        /// <summary>
        /// Deletes a ExhibitTeam by team ID and exhibit ID
        /// </summary>
        /// <remarks>
        /// Deletes a ExhibitTeam with the specified team ID and exhibit ID
        /// <para />
        /// Accessible only to a SuperTeam
        /// </remarks>
        /// <param name="teamId">ID of a team.</param>
        /// <param name="exhibitId">ID of a exhibit.</param>
        /// <param name="ct"></param>
        [HttpDelete("exhibits/{exhibitId}/teams/{teamId}")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [SwaggerOperation(OperationId = "deleteExhibitTeamByIds")]
        public async Task<IActionResult> Delete(Guid exhibitId, Guid teamId, CancellationToken ct)
        {
            await _exhibitTeamService.DeleteByIdsAsync(exhibitId, teamId, ct);
            return NoContent();
        }

    }
}

