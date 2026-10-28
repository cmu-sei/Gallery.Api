// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Gallery.Api.Infrastructure.Exceptions;
using Gallery.Api.Infrastructure.Extensions;
using Gallery.Api.Infrastructure.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using SAC = Steamfitter.Api.Client;

namespace Gallery.Api.Services
{
    public interface ISteamfitterService
    {
        Task<ICollection<SAC.Result>> CreateAndExecuteTaskAsnyc(SAC.TaskCreate taskForm, CancellationToken ct);
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

        public async Task<ICollection<SAC.Result>> CreateAndExecuteTaskAsnyc(SAC.TaskCreate taskForm, CancellationToken ct)
        {
            var client = ApiClientsExtensions.GetHttpClient(_httpClientFactory, _clientOptions.SteamfitterApiUrl);
            var tokenResponse = await ApiClientsExtensions.RequestTokenAsync(_resourceOwnerAuthorizationOptions, client);
            client.DefaultRequestHeaders.Add("authorization", $"{tokenResponse.TokenType} {tokenResponse.AccessToken}");
            var steamfitterApiClient = new SAC.SteamfitterApiClient(client);
            var results = await steamfitterApiClient.CreateAndExecuteTaskAsync(taskForm, ct);

            return results;
        }

        /// <summary>
        /// Asks Steamfitter for the Scenario with the caller's own token rather than Gallery's service account. An
        /// exhibit names the Scenario that its share email goes through with the service account, so it may name only
        /// a Scenario the caller can read, and one that does not exist is no more readable than one they cannot see.
        /// A 403, a 404 or an empty answer all mean the caller cannot use it; which one Steamfitter gives for an id
        /// it does not know depends on its version and on what the caller may see. A server error is answered as
        /// unavailable. The request is sent directly so that every answer, an empty one included, can be read.
        /// </summary>
        public async Task<bool> CanCallerReadScenarioAsync(Guid scenarioId, CancellationToken ct)
        {
            var authorization = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
            if (string.IsNullOrEmpty(authorization))
                return false;

            HttpStatusCode status;
            string body = null;
            try
            {
                var client = ApiClientsExtensions.GetHttpClient(_httpClientFactory, _clientOptions.SteamfitterApiUrl);
                client.DefaultRequestHeaders.Add("authorization", authorization);
                using var response = await client.GetAsync($"api/scenarios/{scenarioId}", ct);
                status = response.StatusCode;
                if (status == HttpStatusCode.OK)
                    body = await response.Content.ReadAsStringAsync(ct);
            }
            // Steamfitter is down, timed out or is not configured; a cancelled request is not one of these
            catch (Exception ex) when (ex is HttpRequestException or UriFormatException or ArgumentNullException ||
                ex is TaskCanceledException && !ct.IsCancellationRequested)
            {
                throw new ServiceUnavailableException("Steamfitter could not be reached to check the Scenario.", ex);
            }

            switch (status)
            {
                case HttpStatusCode.OK:
                    return !string.IsNullOrWhiteSpace(body) && body.Trim() != "null";
                case HttpStatusCode.NoContent:
                case HttpStatusCode.NotFound:
                case HttpStatusCode.Unauthorized:
                case HttpStatusCode.Forbidden:
                    return false;
                default:
                    if ((int)status >= 200 && (int)status < 300)
                        return false;
                    throw new ServiceUnavailableException($"Steamfitter answered {(int)status} when asked for the Scenario.");
            }
        }

    }
}

