// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Net;
using System.Net.Http.Json;
using Gallery.Api.Data.Enumerations;
using Gallery.Api.Data.Models;
using Gallery.Api.Tests.Support;
using Gallery.Api.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Gallery.Api.Tests.Controllers;

/// <summary>
/// <c>ExhibitMembershipsController</c> over HTTP: reads need ViewExhibits or ViewExhibit, writes
/// ManageExhibits or ManageExhibit.
/// </summary>
public class ExhibitMembershipControllerTests(DatabaseFixture fixture, GalleryAppFactory factory) : ApiTestBase(fixture, factory)
{
    [Fact]
    public async Task GetAll_returns_the_memberships_to_a_member_holding_ViewExhibit()
    {
        var (exhibit, membership) = await SeedMembership();
        var actor = await Actor().OnExhibit(exhibit, [ExhibitPermission.ViewExhibit]).SeedAsync();

        var memberships = await ReadAsync<List<ExhibitMembership>>(await Client(actor).GetAsync($"api/exhibits/{exhibit.Id}/memberships", Ct));

        Assert.Contains(memberships, x => x.Id == membership.Id);
    }

    [Fact]
    public async Task GetAll_is_forbidden_for_a_caller_holding_ViewExhibit_only_on_another_exhibit()
    {
        var (exhibit, _) = await SeedMembership();
        var actor = await Actor().OnNewExhibit(exhibit.CollectionId, ExhibitPermission.ViewExhibit).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/exhibits/{exhibit.Id}/memberships", Ct));
    }

    /// <summary>A request with no identity is answered with a 401; the anonymous client is the case under test.</summary>
    [Fact]
    public async Task GetAll_without_an_identity_is_unauthorized()
    {
        await AssertStatus(HttpStatusCode.Unauthorized, await Client().GetAsync($"api/exhibits/{Guid.NewGuid()}/memberships", Ct));
    }

    [Fact]
    public async Task Get_returns_the_membership_to_a_caller_holding_ViewExhibits()
    {
        var (_, membership) = await SeedMembership();
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewExhibits).SeedAsync();

        var got = await ReadAsync<ExhibitMembership>(await Client(actor).GetAsync($"api/exhibits/memberships/{membership.Id}", Ct));

        Assert.Equal(membership.UserId, got.UserId);
    }

    [Fact]
    public async Task Get_returns_the_membership_to_a_member_holding_ViewExhibit()
    {
        var (exhibit, membership) = await SeedMembership();
        var actor = await Actor().OnExhibit(exhibit, [ExhibitPermission.ViewExhibit]).SeedAsync();

        var got = await ReadAsync<ExhibitMembership>(await Client(actor).GetAsync($"api/exhibits/memberships/{membership.Id}", Ct));

        Assert.Equal(membership.Id, got.Id);
    }

    [Fact]
    public async Task Get_is_forbidden_for_a_caller_holding_ViewExhibit_only_on_another_exhibit()
    {
        var (exhibit, membership) = await SeedMembership();
        var actor = await Actor().OnNewExhibit(exhibit.CollectionId, ExhibitPermission.ViewExhibit).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/exhibits/memberships/{membership.Id}", Ct));
    }

    [Fact]
    public async Task Get_is_forbidden_for_a_caller_holding_only_ViewCollections()
    {
        var (_, membership) = await SeedMembership();
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewCollections).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/exhibits/memberships/{membership.Id}", Ct));
    }

    [Fact]
    public async Task Get_reports_an_unknown_membership_as_not_found()
    {
        await AssertProblem(HttpStatusCode.NotFound, await RootClient.GetAsync($"api/exhibits/memberships/{Guid.NewGuid()}", Ct));
    }

    [Fact]
    public async Task Create_persists_the_membership_for_a_member_holding_ManageExhibit()
    {
        var exhibit = await SeedExhibit();
        var user = TestData.User();
        await Seed(user);
        var actor = await Actor().OnExhibit(exhibit, [ExhibitPermission.ManageExhibit]).SeedAsync();

        await AssertStatus(HttpStatusCode.Created, await Client(actor).PostAsJsonAsync($"api/exhibits/{exhibit.Id}/memberships",
            new { exhibitId = exhibit.Id, userId = user.Id, roleId = TestData.MembershipRoles.Observer }, Ct));

        await using var db = NewContext();
        Assert.Equal(exhibit.Id, (await db.ExhibitMemberships.SingleAsync(x => x.UserId == user.Id, Ct)).ExhibitId);
    }

    [Fact]
    public async Task Create_is_forbidden_for_a_caller_holding_only_EditExhibit()
    {
        var exhibit = await SeedExhibit();
        var user = TestData.User();
        await Seed(user);
        var actor = await Actor().OnExhibit(exhibit, [ExhibitPermission.EditExhibit]).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).PostAsJsonAsync($"api/exhibits/{exhibit.Id}/memberships",
            new { exhibitId = exhibit.Id, userId = user.Id }, Ct));
    }

    /// <summary>The gate and the insert both use the body's exhibit, whatever exhibit the route names.</summary>
    [Fact]
    public async Task Create_adds_the_membership_to_the_exhibit_the_body_names()
    {
        var managed = await SeedExhibit();
        var routed = await SeedExhibit();
        var user = TestData.User();
        await Seed(user);
        var actor = await Actor().OnExhibit(managed, [ExhibitPermission.ManageExhibit]).SeedAsync();

        await AssertStatus(HttpStatusCode.Created, await Client(actor).PostAsJsonAsync($"api/exhibits/{routed.Id}/memberships",
            new { exhibitId = managed.Id, userId = user.Id }, Ct));

        await using var db = NewContext();
        Assert.Equal(managed.Id, (await db.ExhibitMemberships.SingleAsync(x => x.UserId == user.Id, Ct)).ExhibitId);
    }

    [Fact]
    public async Task Update_changes_the_role_for_a_member_holding_ManageExhibit()
    {
        var (exhibit, membership) = await SeedMembership();
        var actor = await Actor().OnExhibit(exhibit, [ExhibitPermission.ManageExhibit]).SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).PutAsJsonAsync($"api/exhibits/memberships/{membership.Id}",
            new { id = membership.Id, exhibitId = exhibit.Id, userId = membership.UserId, roleId = TestData.MembershipRoles.Observer }, Ct));

        await using var db = NewContext();
        Assert.Equal(TestData.MembershipRoles.Observer, (await db.ExhibitMemberships.SingleAsync(x => x.Id == membership.Id, Ct)).RoleId);
    }

    [Fact]
    public async Task Update_is_forbidden_for_a_caller_holding_only_EditExhibit()
    {
        var (exhibit, membership) = await SeedMembership();
        var actor = await Actor().OnExhibit(exhibit, [ExhibitPermission.EditExhibit]).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).PutAsJsonAsync($"api/exhibits/memberships/{membership.Id}",
            new { id = membership.Id, exhibitId = exhibit.Id, userId = membership.UserId, roleId = TestData.MembershipRoles.Manager }, Ct));
    }

    /// <summary>The gate reads the exhibit the body names, so the role of a membership on another exhibit changes.</summary>
    [Fact]
    public async Task Update_changes_a_membership_on_another_exhibit_when_the_body_names_one_the_caller_manages()
    {
        var (exhibit, membership) = await SeedMembership();
        var actor = await Actor().OnNewExhibit(exhibit.CollectionId, ExhibitPermission.ManageExhibit).SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).PutAsJsonAsync($"api/exhibits/memberships/{membership.Id}",
            new { id = membership.Id, exhibitId = Assert.Single(actor.NewExhibitIds), userId = membership.UserId, roleId = TestData.MembershipRoles.Manager }, Ct));

        await using var db = NewContext();
        var stored = await db.ExhibitMemberships.SingleAsync(x => x.Id == membership.Id, Ct);
        Assert.Equal((exhibit.Id, TestData.MembershipRoles.Manager), (stored.ExhibitId, stored.RoleId));
    }

    /// <summary>A caller holding only ManageExhibit gives a member the Manager role, which holds every exhibit permission.</summary>
    [Fact]
    public async Task Update_lets_a_caller_holding_only_ManageExhibit_grant_the_Manager_role()
    {
        var (exhibit, membership) = await SeedMembership();
        var actor = await Actor().OnExhibit(exhibit, [ExhibitPermission.ManageExhibit]).SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).PutAsJsonAsync($"api/exhibits/memberships/{membership.Id}",
            new { id = membership.Id, exhibitId = exhibit.Id, userId = membership.UserId, roleId = TestData.MembershipRoles.Manager }, Ct));

        await using var db = NewContext();
        Assert.Equal(TestData.MembershipRoles.Manager, (await db.ExhibitMemberships.SingleAsync(x => x.Id == membership.Id, Ct)).RoleId);
    }

    [Fact]
    public async Task Delete_removes_the_membership_for_a_caller_holding_ManageExhibits()
    {
        var (_, membership) = await SeedMembership();
        var actor = await Actor().WithSystemPermissions(SystemPermission.ManageExhibits).SeedAsync();

        await AssertStatus(HttpStatusCode.NoContent, await Client(actor).DeleteAsync($"api/exhibits/memberships/{membership.Id}", Ct));

        await using var db = NewContext();
        Assert.False(await db.ExhibitMemberships.AnyAsync(x => x.Id == membership.Id, Ct));
    }

    [Fact]
    public async Task Delete_is_forbidden_for_a_caller_holding_ManageExhibit_only_on_another_exhibit()
    {
        var (exhibit, membership) = await SeedMembership();
        var actor = await Actor().OnNewExhibit(exhibit.CollectionId, ExhibitPermission.ManageExhibit).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).DeleteAsync($"api/exhibits/memberships/{membership.Id}", Ct));

        await using var db = NewContext();
        Assert.True(await db.ExhibitMemberships.AnyAsync(x => x.Id == membership.Id, Ct));
    }

    private async Task<ExhibitEntity> SeedExhibit()
    {
        var collection = TestData.Collection();
        var exhibit = TestData.Exhibit(collection.Id);
        await Seed(collection, exhibit);

        return exhibit;
    }

    private async Task<(ExhibitEntity Exhibit, ExhibitMembershipEntity Membership)> SeedMembership()
    {
        var exhibit = await SeedExhibit();
        var user = TestData.User(name: "Member");
        var membership = TestData.ExhibitMembership(exhibit.Id, user.Id);
        await Seed(user, membership);

        return (exhibit, membership);
    }
}
