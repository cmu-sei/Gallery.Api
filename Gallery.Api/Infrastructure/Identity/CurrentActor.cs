// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using Gallery.Api.Data;
using Microsoft.AspNetCore.Http;

namespace Gallery.Api.Infrastructure.Identity
{
    /// <summary>The caller of the current request or hub invocation, from its <c>sub</c> claim.</summary>
    public class CurrentActor : ICurrentActor
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentActor(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Guid? Id =>
            Guid.TryParse(_httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value, out var id) ? id : null;
    }
}
