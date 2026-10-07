// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

// gallery.api ships Database:Provider=PostgreSQL, so the provider needs no override; the factory points
// the connection string at the host's throwaway database (step 1B).

namespace Gallery.Api.Tests.Support;

/// <summary>
/// The configuration the app factory layers over the application's own <c>appsettings.json</c>.
/// </summary>
/// <remarks>
/// <c>WebApplicationFactory</c> resolves the content root to the API project directory, so the shipped
/// configuration is already in force and only keys whose shipped value breaks or weakens a test run belong
/// here. Every entry states which.
/// </remarks>
internal static class TestConfiguration
{
    public static Dictionary<string, string> Values => new()
    {
        // One host serves the whole run, and UserClaimsService caches claims in the singleton
        // IMemoryCache keyed on user id alone, so cached claims would leak across tests: a user whose
        // permissions one test seeds would keep them in the next test that uses the same id. A test whose
        // subject is the cache drives it directly.
        ["ClaimsTransformation:EnableCaching"] = "false",
    };
}
