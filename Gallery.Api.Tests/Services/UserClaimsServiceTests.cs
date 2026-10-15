// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Security.Claims;
using Gallery.Api.Data.Enumerations;
using Gallery.Api.Infrastructure.Authorization;
using Gallery.Api.Infrastructure.Options;
using Gallery.Api.Services;
using Gallery.Api.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Gallery.Api.Tests.Services;

/// <summary>
/// <c>UserClaimsService</c> driven directly, for the options the hosted application turns off
/// (<see cref="TestConfiguration"/>): the claims cache and roles and groups read from the token.
/// </summary>
public class UserClaimsServiceTests(DatabaseFixture fixture) : DatabaseTestBase(fixture)
{
    /// <summary>The claim the identity provider puts the realm roles in, a JSON object as Keycloak sends it.</summary>
    private const string RealmAccess = "realm_access";

    [Fact]
    public async Task A_first_identity_creates_the_user_row_with_its_name()
    {
        var id = Guid.NewGuid();

        await AddClaims(Identity(id, "Newcomer"), update: true);

        await using var db = NewContext();
        Assert.Equal("Newcomer", (await db.Users.SingleAsync(x => x.Id == id, Ct)).Name);
    }

    [Fact]
    public async Task A_known_identity_with_a_new_name_renames_the_user()
    {
        var user = TestData.User(name: "Old Name");
        await Seed(user);

        await AddClaims(Identity(user.Id, "New Name"), update: true);

        await using var db = NewContext();
        Assert.Equal("New Name", (await db.Users.SingleAsync(x => x.Id == user.Id, Ct)).Name);
    }

    /// <summary>With caching on, a role taken away keeps granting until the cache entry expires.</summary>
    [Fact]
    public async Task With_caching_on_a_removed_role_keeps_granting_from_the_cache()
    {
        var role = TestData.SystemRole(permissions: [SystemPermission.ViewGroups]);
        var user = TestData.User(roleId: role.Id);
        await Seed(role, user);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var options = Options(caching: true);
        await AddClaims(Identity(user.Id), update: false, cache, options);
        await using (var db = NewContext())
        {
            (await db.Users.SingleAsync(x => x.Id == user.Id, Ct)).RoleId = null;
            await db.SaveChangesAsync(Ct);
        }

        var principal = await AddClaims(Identity(user.Id), update: false, cache, options);

        Assert.Equal(["ViewGroups"], Permissions(principal));
    }

    [Fact]
    public async Task A_realm_role_in_the_token_grants_the_system_role_of_that_name()
    {
        var principal = Identity(Guid.NewGuid(), extra: new Claim(RealmAccess, """{"roles":["content developer"]}""", JsonClaimValueTypes.Json));
        await Seed(TestData.User(Guid.Parse(principal.FindFirst("sub").Value)));

        var claims = await AddClaims(principal, update: false, options: Options(rolesFromIdP: true));

        Assert.Contains("CreateExhibits", Permissions(claims));
    }

    [Fact]
    public async Task A_group_named_in_the_token_grants_the_group_s_collection_membership()
    {
        var group = TestData.Group("White Cell");
        var collection = TestData.Collection();
        var user = TestData.User();
        await Seed(group, collection, user, new Gallery.Api.Data.Models.CollectionMembershipEntity(collection.Id, null, group.Id)
        {
            Id = Guid.NewGuid(),
            RoleId = TestData.MembershipRoles.Observer
        });

        var claims = await AddClaims(Identity(user.Id, extra: new Claim("groups", "white cell")), update: false, options: Options(groupsFromIdP: true));

        var claim = CollectionPermissionClaim.FromString(claims.FindFirst(AuthorizationConstants.CollectionPermissionClaimType).Value);
        Assert.Equal((collection.Id, CollectionPermission.ViewCollection), (claim.CollectionId, Assert.Single(claim.Permissions)));
    }

    /// <summary>A membership role with AllPermissions grants every exhibit permission, participating included.</summary>
    [Fact]
    public async Task An_exhibit_manager_holds_every_exhibit_permission()
    {
        var collection = TestData.Collection();
        var exhibit = TestData.Exhibit(collection.Id);
        var user = TestData.User();
        await Seed(collection, exhibit, user, TestData.ExhibitMembership(exhibit.Id, user.Id, TestData.MembershipRoles.Manager));

        var claims = await AddClaims(Identity(user.Id), update: false);

        var claim = ExhibitPermissionClaim.FromString(claims.FindFirst(AuthorizationConstants.ExhibitPermissionClaimType).Value);
        Assert.Equal(Enum.GetValues<ExhibitPermission>(), claim.Permissions.Order());
    }

    private async Task<ClaimsPrincipal> AddClaims(
        ClaimsPrincipal principal,
        bool update,
        IMemoryCache cache = null,
        ClaimsTransformationOptions options = null)
    {
        await using var context = NewContext();
        using var ownCache = new MemoryCache(new MemoryCacheOptions());

        return await new UserClaimsService(context, cache ?? ownCache, options ?? Options()).AddUserClaims(principal, update);
    }

    private static ClaimsTransformationOptions Options(bool caching = false, bool rolesFromIdP = false, bool groupsFromIdP = false) =>
        new()
        {
            EnableCaching = caching,
            CacheExpirationSeconds = 60,
            UseRolesFromIdP = rolesFromIdP,
            RolesClaimPath = "realm_access.roles",
            UseGroupsFromIdP = groupsFromIdP,
            GroupsClaimPath = "groups"
        };

    private static ClaimsPrincipal Identity(Guid id, string name = "Test User", Claim extra = null)
    {
        List<Claim> claims = [new("sub", id.ToString()), new("name", name)];

        if (extra is not null)
        {
            claims.Add(extra);
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    private static string[] Permissions(ClaimsPrincipal principal) =>
        [.. principal.FindAll(AuthorizationConstants.PermissionClaimType).Select(x => x.Value).Order()];
}
