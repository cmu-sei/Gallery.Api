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
using Gallery.Api.Infrastructure.Extensions;
using Gallery.Api.Services;
using Gallery.Api.ViewModels;
using Swashbuckle.AspNetCore.Annotations;

namespace Gallery.Api.Controllers
{
    public class UserArticleController : BaseController
    {
        private readonly IUserArticleService _userArticleService;
        private readonly IGalleryAuthorizationService _authorizationService;

        public UserArticleController(IUserArticleService userArticleService, IGalleryAuthorizationService authorizationService)
        {
            _userArticleService = userArticleService;
            _authorizationService = authorizationService;
        }

        /// <summary>
        /// Gets UserArticles for an Exhibit
        /// </summary>
        /// <remarks>
        /// Returns a list of UserArticles for the exhibit.
        /// </remarks>
        /// <param name="exhibitId">The id of the Exhibit</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpGet("exhibits/{exhibitId}/userArticles")]
        [ProducesResponseType(typeof(IEnumerable<UserArticle>), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "getExhibitUserArticles")]
        public async Task<IActionResult> GetByExhibit(Guid exhibitId, CancellationToken ct)
        {
            if (!await _authorizationService.AuthorizeAsync<Exhibit>(exhibitId, [SystemPermission.ViewExhibits], [ExhibitPermission.ViewExhibit], ct))
                throw new ForbiddenException();

            var list = await _userArticleService.GetByExhibitAsync(exhibitId, ct);
            return Ok(list);
        }

        /// <summary>
        /// Gets UserArticles for an exhibit team
        /// </summary>
        /// <remarks>
        /// Returns a list of UserArticles based on the exhibit's current move and current inject for the team.
        /// </remarks>
        /// <param name="exhibitId">The id of the Exhibit</param>
        /// <param name="teamId">The id of the Team</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpGet("exhibits/{exhibitId}/teams/{teamId}/userarticles")]
        [ProducesResponseType(typeof(IEnumerable<UserArticle>), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "getExhibitTeamUserArticles")]
        public async Task<IActionResult> GetByExhibitTeam(Guid exhibitId, Guid teamId, CancellationToken ct)
        {
            if (!await _authorizationService.AuthorizeAsync<Team>(teamId, [TeamPermission.ViewTeam], ct))
                throw new ForbiddenException();

            var list = await _userArticleService.GetByExhibitTeamAsync(exhibitId, teamId, ct);
            return Ok(list);
        }

        /// <summary>
        /// Gets the count of unread UserArticles for an exhibit for the user
        /// </summary>
        /// <remarks>
        /// Returns the count of unread UserArticles for an exhibit for the user.
        /// </remarks>
        /// <param name="exhibitId">The id of the Exhibit</param>
        /// <param name="userId">The id of the User</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpGet("exhibits/{exhibitId}/users/{userId}/Articles/unread")]
        [ProducesResponseType(typeof(UnreadArticles), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "getUnreadCount")]
        public async Task<IActionResult> GetUnreadCount(Guid exhibitId, Guid userId, CancellationToken ct)
        {
            if (!await _authorizationService.AuthorizeAsync<Exhibit>(exhibitId, [SystemPermission.ViewExhibits], [ExhibitPermission.ViewExhibit], ct))
                throw new ForbiddenException();

            var unreadArticles = await _userArticleService.GetUnreadCountAsync(exhibitId, userId, ct);
            return Ok(unreadArticles);
        }

        /// <summary>
        /// Creates a new UserArticle
        /// </summary>
        /// <remarks>
        /// Creates a new UserArticle with the attributes specified
        /// </remarks>
        /// <param name="userArticle">The data used to create the UserArticle</param>
        /// <param name="ct"></param>
        [HttpPost("userArticles")]
        [ProducesResponseType(typeof(UserArticle), (int)HttpStatusCode.Created)]
        [SwaggerOperation(OperationId = "createUserArticle")]
        public async Task<IActionResult> Create([FromBody] UserArticle userArticle, CancellationToken ct)
        {
            if (!await _authorizationService.AuthorizeAsync<Exhibit>(userArticle.ExhibitId, [SystemPermission.EditExhibits], [ExhibitPermission.EditExhibit], ct))
                throw new ForbiddenException();

            var createdUserArticle = await _userArticleService.CreateAsync(userArticle, ct);
            return Ok(createdUserArticle);
        }

        /// <summary>
        /// Shares a UserArticle
        /// </summary>
        /// <remarks>
        /// Shares a UserArticle with another team.
        /// </remarks>
        /// <param name="id">The ID of the UserArticle to share</param>
        /// <param name="shareDetails">List of team IDs to share with and the message to be sent to the users</param>
        /// <param name="ct"></param>
        [HttpPut("userArticles/{id}/share")]
        [ProducesResponseType(typeof(UserArticle), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "shareUserArticle")]
        public async Task<IActionResult> Share([FromRoute] Guid id, [FromBody] ShareDetails shareDetails, CancellationToken ct)
        {
            var userArticle = await _userArticleService.GetAsync(id, ct);
            if (!await _authorizationService.AuthorizeAsync<Exhibit>(userArticle.ExhibitId, [SystemPermission.EditExhibits], [ExhibitPermission.EditExhibit], ct))
                throw new ForbiddenException();

            var sharedUserArticle = await _userArticleService.ShareAsync(id, shareDetails, ct);
            return Ok(sharedUserArticle);
        }

        /// <summary>
        /// Sets the IsRead status of a UserArticle
        /// </summary>
        /// <remarks>
        /// Sets the IsRead status of a UserArticle
        /// </remarks>
        /// <param name="id">The ID of the UserArticle</param>
        /// <param name="isRead">The state to set IsRead</param>
        /// <param name="ct"></param>
        [HttpPut("userArticles/{id}/isread")]
        [ProducesResponseType(typeof(UserArticle), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "setIsRead")]
        public async Task<IActionResult> SetIsRead([FromRoute] Guid id, [FromBody] bool isRead, CancellationToken ct)
        {
            var userArticle = await _userArticleService.GetAsync(id, ct);
            if (userArticle.UserId != User.GetId())
                throw new ForbiddenException();

            userArticle = await _userArticleService.SetIsReadAsync(id, isRead, ct);
            return Ok(userArticle);
        }

        /// <summary>
        /// Deletes a UserArticle
        /// </summary>
        /// <remarks>
        /// Deletes a UserArticle with the specified id
        /// <para />
        /// Accessible only to a ContentDeveloper or an Administrator
        /// </remarks>
        /// <param name="id">The id of the UserArticle to delete</param>
        /// <param name="ct"></param>
        [HttpDelete("userArticles/{id}")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [SwaggerOperation(OperationId = "deleteUserArticle")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var userArticle = await _userArticleService.GetAsync(id, ct);
            if (!await _authorizationService.AuthorizeAsync<Exhibit>(userArticle.ExhibitId, [SystemPermission.EditExhibits], [ExhibitPermission.EditExhibit], ct))
                throw new ForbiddenException();

            await _userArticleService.DeleteAsync(id, ct);
            return NoContent();
        }

    }

}
