// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Security.Claims;
using System.Threading.Tasks;
using Gallery.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Gallery.Api.Infrastructure.Extensions;

namespace Gallery.Api.Infrastructure.Authorization
{
    class AuthorizationClaimsTransformer : IClaimsTransformation
    {
        private IUserClaimsService _claimsService;

        public AuthorizationClaimsTransformer(IUserClaimsService claimsService)
        {
            _claimsService = claimsService;
        }

        public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            var user = principal.NormalizeScopeClaims();
            user = await _claimsService.AddUserClaims(user, true);
            _claimsService.SetCurrentClaimsPrincipal(user);
            return user;
        }
    }
}

