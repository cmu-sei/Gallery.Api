// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Gallery.Api.Infrastructure.Options;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using IdentityModel.Client;

namespace Gallery.Api.Infrastructure.Extensions
{
    public static class ApiClientsExtensions
    {
        public static HttpClient GetHttpClient(IHttpClientFactory httpClientFactory, string apiUrl)
        {
            var client = httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(apiUrl);
            return client;
        }

        public static async Task<TokenResponse> RequestTokenAsync(ResourceOwnerAuthorizationOptions authorizationOptions, HttpClient httpClient)
        {
            var disco = await httpClient.GetDiscoveryDocumentAsync(
                new DiscoveryDocumentRequest
                {
                    Address = authorizationOptions.Authority,
                    Policy =
                    {
                        ValidateIssuerName = authorizationOptions.ValidateDiscoveryDocument,
                        ValidateEndpoints = authorizationOptions.ValidateDiscoveryDocument,
                    },
                }
            );

            if (disco.IsError) throw new Exception(disco.Error);

            PasswordTokenRequest request = new PasswordTokenRequest
            {
                Address = disco.TokenEndpoint,
                ClientId = authorizationOptions.ClientId,
                ClientSecret = string.IsNullOrEmpty(authorizationOptions.ClientSecret) ? null : authorizationOptions.ClientSecret,
                Password = authorizationOptions.Password,
                Scope = authorizationOptions.Scope,
                UserName = authorizationOptions.UserName
            };

            return await httpClient.RequestPasswordTokenAsync(request);
        }

    }
}
