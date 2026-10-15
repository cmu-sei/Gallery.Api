// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

// gallery.api: one "Permission" claim per system permission, and one JSON claim per collection, exhibit
// and team (CollectionPermissionClaim, ExhibitPermissionClaim, TeamPermissionClaim), as
// UserClaimsService.GetPermissionClaims writes them. For tests of the authorization stack itself, never
// for an HTTP test's caller (that is TestActor).

using System.Security.Claims;
using Gallery.Api.Data.Enumerations;
using Gallery.Api.Infrastructure.Authorization;

namespace Gallery.Api.Tests.Support;

/// <summary>Builds the principal the authorization stack sees for a signed-in user.</summary>
public sealed class ClaimsPrincipalBuilder
{
    private readonly List<Claim> _claims = [];
    private Guid _userId = Guid.NewGuid();
    private string _name = "Test User";

    public Guid UserId => _userId;

    public ClaimsPrincipalBuilder WithUserId(Guid userId)
    {
        _userId = userId;
        return this;
    }

    public ClaimsPrincipalBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    /// <summary>Adds system permissions, which grant across every resource.</summary>
    public ClaimsPrincipalBuilder WithSystemPermissions(params SystemPermission[] permissions)
    {
        foreach (var permission in permissions)
        {
            _claims.Add(new Claim(AuthorizationConstants.PermissionClaimType, permission.ToString()));
        }

        return this;
    }

    /// <summary>A raw system-permission value, for values that are not enum names.</summary>
    public ClaimsPrincipalBuilder WithRawSystemPermission(string value)
    {
        _claims.Add(new Claim(AuthorizationConstants.PermissionClaimType, value));
        return this;
    }

    /// <summary>The claim a collection membership produces.</summary>
    public ClaimsPrincipalBuilder WithCollection(Guid collectionId, params CollectionPermission[] permissions)
    {
        var claim = new CollectionPermissionClaim { CollectionId = collectionId, Permissions = permissions };
        _claims.Add(new Claim(AuthorizationConstants.CollectionPermissionClaimType, claim.ToString()));
        return this;
    }

    /// <summary>The claim an exhibit membership produces.</summary>
    public ClaimsPrincipalBuilder WithExhibit(Guid exhibitId, params ExhibitPermission[] permissions)
    {
        var claim = new ExhibitPermissionClaim { ExhibitId = exhibitId, Permissions = permissions };
        _claims.Add(new Claim(AuthorizationConstants.ExhibitPermissionClaimType, claim.ToString()));
        return this;
    }

    /// <summary>The claim a team membership produces.</summary>
    public ClaimsPrincipalBuilder WithTeam(Guid teamId, params TeamPermission[] permissions)
    {
        var claim = new TeamPermissionClaim { TeamId = teamId, Permissions = permissions };
        _claims.Add(new Claim(AuthorizationConstants.TeamPermissionClaimType, claim.ToString()));
        return this;
    }

    /// <summary>An arbitrary claim, for asserting that unrelated claim types are ignored.</summary>
    public ClaimsPrincipalBuilder WithClaim(string type, string value)
    {
        _claims.Add(new Claim(type, value));
        return this;
    }

    public ClaimsPrincipal Build()
    {
        var claims = new List<Claim>(_claims) { new("sub", _userId.ToString()), new("name", _name) };

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    /// <summary>An authenticated principal with no permissions, the baseline every check must reject.</summary>
    public static ClaimsPrincipal Anonymous() => new ClaimsPrincipalBuilder().Build();
}
