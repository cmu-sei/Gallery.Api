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
    public class TeamArticleController : BaseController
    {
        private readonly ITeamArticleService _teamArticleService;
        private readonly IAuthorizationService _authorizationService;

        public TeamArticleController(ITeamArticleService teamArticleService, IAuthorizationService authorizationService)
        {
            _teamArticleService = teamArticleService;
            _authorizationService = authorizationService;
        }

        /// <summary>
        /// Gets all TeamArticles in the system
        /// </summary>
        /// <remarks>
        /// Returns a list of all of the TeamArticles in the system.
        /// <para />
        /// Only accessible to a SuperArticle
        /// </remarks>
        /// <returns></returns>
        [HttpGet("teamarticles")]
        [ProducesResponseType(typeof(IEnumerable<TeamArticle>), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "getTeamArticles")]
        public async Task<IActionResult> Get(CancellationToken ct)
        {
            var list = await _teamArticleService.GetAsync(ct);
            return Ok(list);
        }

        /// <summary>
        /// Gets all TeamArticles for a exhibit
        /// </summary>
        /// <remarks>
        /// Returns a list of all of the TeamArticles for the exhibit.
        /// </remarks>
        /// <param name="exhibitId">The id of the Exhibit</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpGet("exhibits/{exhibitId}/teamarticles")]
        [ProducesResponseType(typeof(IEnumerable<TeamArticle>), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "getExhibitTeamArticles")]
        public async Task<IActionResult> GetByExhibit(Guid exhibitId, CancellationToken ct)
        {
            var list = await _teamArticleService.GetByExhibitAsync(exhibitId, ct);
            return Ok(list);
        }

        /// <summary>
        /// Gets all TeamArticles for an article
        /// </summary>
        /// <remarks>
        /// Returns a list of all of the TeamArticles for the article.
        /// </remarks>
        /// <param name="articleId">The id of the TeamArticle</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpGet("articles/{articleId}/teamarticles")]
        [ProducesResponseType(typeof(IEnumerable<TeamArticle>), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "getArticleTeamArticles")]
        public async Task<IActionResult> GetByArticle(Guid articleId, CancellationToken ct)
        {
            var list = await _teamArticleService.GetByArticleAsync(articleId, ct);
            return Ok(list);
        }

        /// <summary>
        /// Gets all TeamArticles for an team
        /// </summary>
        /// <remarks>
        /// Returns a list of all of the TeamArticles for the team.
        /// </remarks>
        /// <param name="teamId">The id of the TeamArticle</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpGet("teams/{teamId}/teamarticles")]
        [ProducesResponseType(typeof(IEnumerable<TeamArticle>), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "getTeamTeamArticles")]
        public async Task<IActionResult> GetByTeam(Guid teamId, CancellationToken ct)
        {
            var list = await _teamArticleService.GetByTeamAsync(teamId, ct);
            return Ok(list);
        }

        /// <summary>
        /// Gets a specific TeamArticle by id
        /// </summary>
        /// <remarks>
        /// Returns the TeamArticle with the id specified
        /// <para />
        /// Only accessible to a SuperArticle
        /// </remarks>
        /// <param name="id">The id of the TeamArticle</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpGet("teamarticles/{id}")]
        [ProducesResponseType(typeof(TeamArticle), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "getTeamArticle")]
        public async Task<IActionResult> Get(Guid id, CancellationToken ct)
        {
            var team = await _teamArticleService.GetAsync(id, ct);

            if (team == null)
                throw new EntityNotFoundException<TeamArticle>();

            return Ok(team);
        }

        /// <summary>
        /// Creates a new TeamArticle
        /// </summary>
        /// <remarks>
        /// Creates a new TeamArticle with the attributes specified
        /// <para />
        /// Accessible only to a SuperArticle
        /// </remarks>
        /// <param name="teamArticle">The data to create the TeamArticle with</param>
        /// <param name="ct"></param>
        [HttpPost("teamarticles")]
        [ProducesResponseType(typeof(TeamArticle), (int)HttpStatusCode.Created)]
        [SwaggerOperation(OperationId = "createTeamArticle")]
        public async Task<IActionResult> Create([FromBody] TeamArticle teamArticle, CancellationToken ct)
        {
            teamArticle.CreatedBy = User.GetId();
            var createdTeamArticle = await _teamArticleService.CreateAsync(teamArticle, ct);
            return CreatedAtAction(nameof(this.Get), new { id = createdTeamArticle.Id }, createdTeamArticle);
        }

        /// <summary>
        /// Updates an TeamArticle
        /// </summary>
        /// <remarks>
        /// Updates a TeamArticle with the attributes specified.
        /// The ID from the route MUST MATCH the ID contained in the teamArticle parameter
        /// <para />
        /// Accessible only to a ContentDeveloper or an Administrator
        /// </remarks>
        /// <param name="id">The Id of the TeamArticle to update</param>
        /// <param name="teamArticle">The updated TeamArticle values</param>
        /// <param name="ct"></param>
        [HttpPut("teamArticles/{id}")]
        [ProducesResponseType(typeof(TeamArticle), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "updateTeamArticle")]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] TeamArticle teamArticle, CancellationToken ct)
        {
            teamArticle.ModifiedBy = User.GetId();
            var updatedTeamArticle = await _teamArticleService.UpdateAsync(id, teamArticle, ct);
            return Ok(updatedTeamArticle);
        }

        /// <summary>
        /// Deletes a TeamArticle
        /// </summary>
        /// <remarks>
        /// Deletes a TeamArticle with the specified id
        /// <para />
        /// Accessible only to a SuperArticle
        /// </remarks>
        /// <param name="id">The id of the TeamArticle to delete</param>
        /// <param name="ct"></param>
        [HttpDelete("teamarticles/{id}")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [SwaggerOperation(OperationId = "deleteTeamArticle")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            await _teamArticleService.DeleteAsync(id, ct);
            return NoContent();
        }

        /// <summary>
        /// Deletes a TeamArticle by article ID and team ID
        /// </summary>
        /// <remarks>
        /// Deletes a TeamArticle with the specified article ID and team ID
        /// <para />
        /// Accessible only to a SuperArticle
        /// </remarks>
        /// <param name="articleId">ID of a article.</param>
        /// <param name="teamId">ID of a team.</param>
        /// <param name="ct"></param>
        [HttpDelete("teams/{teamId}/articles/{articleId}")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [SwaggerOperation(OperationId = "deleteTeamArticleByIds")]
        public async Task<IActionResult> Delete(Guid teamId, Guid articleId, CancellationToken ct)
        {
            await _teamArticleService.DeleteByIdsAsync(teamId, articleId, ct);
            return NoContent();
        }

    }
}

