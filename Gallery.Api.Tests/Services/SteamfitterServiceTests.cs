// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Gallery.Api.Services;

namespace Gallery.Api.Tests.Services;

/// <summary><c>SteamfitterService.ScenarioUri</c>, the address the Scenario check asks Steamfitter at.</summary>
public class SteamfitterServiceTests
{
    private static readonly Guid ScenarioId = Guid.Parse("11111111-2222-3333-4444-555555555555");

    /// <summary>
    /// Steamfitter can sit under a path. Resolved as a relative path, <c>api/scenarios/...</c> would replace the
    /// path's last segment when the configured url has no trailing slash.
    /// </summary>
    [Theory]
    [InlineData("http://localhost:4400/", "http://localhost:4400/api/scenarios/11111111-2222-3333-4444-555555555555")]
    [InlineData("http://localhost:4400", "http://localhost:4400/api/scenarios/11111111-2222-3333-4444-555555555555")]
    [InlineData("https://crucible.example/steamfitter/", "https://crucible.example/steamfitter/api/scenarios/11111111-2222-3333-4444-555555555555")]
    [InlineData("https://crucible.example/steamfitter", "https://crucible.example/steamfitter/api/scenarios/11111111-2222-3333-4444-555555555555")]
    public void ScenarioUri_keeps_every_segment_of_the_steamfitter_url(string steamfitterApiUrl, string expected)
    {
        Assert.Equal(expected, SteamfitterService.ScenarioUri(steamfitterApiUrl, ScenarioId).AbsoluteUri);
    }

    /// <summary>A missing or relative url throws the exceptions the Scenario check answers as unavailable.</summary>
    [Fact]
    public void ScenarioUri_of_a_missing_or_relative_url_throws()
    {
        Assert.Throws<ArgumentNullException>(() => SteamfitterService.ScenarioUri(null, ScenarioId));
        Assert.Throws<UriFormatException>(() => SteamfitterService.ScenarioUri("", ScenarioId));
        Assert.Throws<UriFormatException>(() => SteamfitterService.ScenarioUri("steamfitter", ScenarioId));
    }
}
