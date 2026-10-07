// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Security.Claims;
using Gallery.Api.Infrastructure.Extensions;

namespace Gallery.Api.Tests.Infrastructure.Extensions;

/// <summary><c>ClaimsPrincipalExtensions</c> and <c>StringExtensions</c>, which the claims transformer and the event handlers rely on.</summary>
public class ExtensionTests
{
    private const string NameIdentifier = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier";

    [Fact]
    public void GetId_reads_the_sub_claim()
    {
        var id = Guid.NewGuid();
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", id.ToString())], "Test"));

        Assert.Equal(id, principal.GetId());
    }

    [Fact]
    public void GetId_falls_back_to_the_name_identifier_claim()
    {
        var id = Guid.NewGuid();
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(NameIdentifier, id.ToString())], "Test"));

        Assert.Equal(id, principal.GetId());
    }

    [Fact]
    public void NormalizeScopeClaims_splits_a_space_separated_scope_claim()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("scope", "gallery steamfitter")], "Test"));

        var normalized = principal.NormalizeScopeClaims();

        Assert.Equal(["gallery", "steamfitter"], normalized.FindAll("scope").Select(x => x.Value));
    }

    [Theory]
    [InlineData("CurrentMove", "currentMove")]
    [InlineData("Id", "id")]
    [InlineData("X", "X")]
    [InlineData("", "")]
    public void TitleCaseToCamelCase_lowers_the_first_letter_of_a_longer_name(string input, string expected)
    {
        Assert.Equal(expected, input.TitleCaseToCamelCase());
    }
}
