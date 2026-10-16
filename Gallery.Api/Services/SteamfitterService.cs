// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Gallery.Api.Infrastructure.Extensions;
using Gallery.Api.Infrastructure.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using SAC = Steamfitter.Api.Client;

namespace Gallery.Api.Services
{
    public interface ISteamfitterService
    {
        Task<ICollection<SAC.Result>> CreateAndExecuteTaskAsnyc(SAC.TaskForm taskForm, CancellationToken ct);
        Task<bool> CanCallerReadScenarioAsync(Guid scenarioId, CancellationToken ct);
    }

    public class SteamfitterService : ISteamfitterService
    {
        private readonly ResourceOwnerAuthorizationOptions _resourceOwnerAuthorizationOptions;
        private readonly ClientOptions _clientOptions;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SteamfitterService(
            IHttpClientFactory httpClientFactory,
            IHttpContextAccessor httpContextAccessor,
            ClientOptions clientOptions,
            ResourceOwnerAuthorizationOptions resourceOwnerAuthorizationOptions)
        {
            _httpClientFactory = httpClientFactory;
            _httpContextAccessor = httpContextAccessor;
            _clientOptions = clientOptions;
            _resourceOwnerAuthorizationOptions = resourceOwnerAuthorizationOptions;
        }

        public async Task<ICollection<SAC.Result>> CreateAndExecuteTaskAsnyc(SAC.TaskForm taskForm, CancellationToken ct)
        {
            var client = ApiClientsExtensions.GetHttpClient(_httpClientFactory, _clientOptions.SteamfitterApiUrl);
            var tokenResponse = await ApiClientsExtensions.RequestTokenAsync(_resourceOwnerAuthorizationOptions, client);
            client.DefaultRequestHeaders.Add("authorization", $"{tokenResponse.TokenType} {tokenResponse.AccessToken}");
            var steamfitterApiClient = new SAC.SteamfitterApiClient(client);
            var results = await steamfitterApiClient.CreateAndExecuteTaskAsync(taskForm, ct);

            return results;
        }

        /// <summary>
        /// Whether the caller can read the Scenario in Steamfitter, asked with the caller's own token rather than
        /// Gallery's service account. An exhibit names the Scenario that its share email goes through with the
        /// service account, so only a Scenario the caller can already read may be named.
        /// </summary>
        public async Task<bool> CanCallerReadScenarioAsync(Guid scenarioId, CancellationToken ct)
        {
            var authorization = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
            if (string.IsNullOrEmpty(authorization))
                return false;

            var client = ApiClientsExtensions.GetHttpClient(_httpClientFactory, _clientOptions.SteamfitterApiUrl);
            client.DefaultRequestHeaders.Add("authorization", authorization);
            var steamfitterApiClient = new SAC.SteamfitterApiClient(client);
            try
            {
                return await steamfitterApiClient.GetScenarioAsync(scenarioId, ct) != null;
            }
            catch (SAC.ApiException ex) when (ex.StatusCode is 401 or 403 or 404)
            {
                return false;
            }
        }

    }
}

