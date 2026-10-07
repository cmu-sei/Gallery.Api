// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Gallery.Api.Data.Enumerations;
using Gallery.Api.Data.Models;
using Gallery.Api.Tests.Support;
using Gallery.Api.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Gallery.Api.Tests.Controllers;

/// <summary>
/// <c>ExhibitController</c> over HTTP. An exhibit gate passes on the system permission or on the matching
/// permission of an exhibit membership; the near misses are a neighbouring permission on the exhibit, the
/// right permission on another exhibit of the same collection, and a collection permission.
/// </summary>
public class ExhibitControllerTests(DatabaseFixture fixture, GalleryAppFactory factory) : ApiTestBase(fixture, factory)
{
    // ---- Lists ---------------------------------------------------------------------------------

    [Fact]
    public async Task GetAll_returns_every_exhibit_to_a_caller_holding_ViewExhibits()
    {
        var exhibit = await SeedExhibit();
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewExhibits).SeedAsync();

        var exhibits = await ReadAsync<List<Exhibit>>(await Client(actor).GetAsync("api/exhibits", Ct));

        Assert.Contains(exhibits, x => x.Id == exhibit.Id);
    }

    [Fact]
    public async Task GetAll_returns_only_the_member_s_exhibits_to_a_caller_without_ViewExhibits()
    {
        var mine = await SeedExhibit();
        await SeedExhibit();
        var actor = await Actor().OnExhibit(mine, [ExhibitPermission.ViewExhibit]).SeedAsync();

        var exhibits = await ReadAsync<List<Exhibit>>(await Client(actor).GetAsync("api/exhibits", Ct));

        Assert.Equal(mine.Id, Assert.Single(exhibits).Id);
    }

    /// <summary>A request with no identity is answered with a 401; the anonymous client is the case under test.</summary>
    [Fact]
    public async Task GetAll_without_an_identity_is_unauthorized()
    {
        await AssertStatus(HttpStatusCode.Unauthorized, await Client().GetAsync("api/exhibits", Ct));
    }

    [Fact]
    public async Task GetMine_returns_the_exhibits_of_the_caller_s_teams()
    {
        var exhibit = await SeedExhibit();
        var team = await SeedTeam(exhibit);
        await SeedExhibit();
        var actor = await Actor().OnTeam(team).SeedAsync();

        var exhibits = await ReadAsync<List<Exhibit>>(await Client(actor).GetAsync("api/my-exhibits", Ct));

        Assert.Equal(exhibit.Id, Assert.Single(exhibits).Id);
    }

    [Fact]
    public async Task GetUserExhibits_returns_the_caller_s_own_exhibits_without_any_permission()
    {
        var exhibit = await SeedExhibit();
        var team = await SeedTeam(exhibit);
        var actor = await Actor().OnTeam(team).SeedAsync();

        var exhibits = await ReadAsync<List<Exhibit>>(await Client(actor).GetAsync($"api/users/{actor.Id}/exhibits", Ct));

        Assert.Equal(exhibit.Id, Assert.Single(exhibits).Id);
    }

    [Fact]
    public async Task GetUserExhibits_of_another_user_is_allowed_for_a_caller_holding_ViewExhibits()
    {
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewExhibits).SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).GetAsync($"api/users/{Root.Id}/exhibits", Ct));
    }

    [Fact]
    public async Task GetUserExhibits_of_another_user_is_forbidden_for_a_caller_holding_only_ViewCollections()
    {
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewCollections).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/users/{Root.Id}/exhibits", Ct));
    }

    [Fact]
    public async Task GetByCollection_returns_every_exhibit_to_a_collection_manager()
    {
        var exhibit = await SeedExhibit();
        var actor = await Actor().OnCollection(await Collection(exhibit), [CollectionPermission.ManageCollection]).SeedAsync();

        var exhibits = await ReadAsync<List<Exhibit>>(await Client(actor).GetAsync($"api/collections/{exhibit.CollectionId}/exhibits", Ct));

        Assert.Equal(exhibit.Id, Assert.Single(exhibits).Id);
    }

    /// <summary>A collection editor is not refused but sees only the exhibits they are a member of.</summary>
    [Fact]
    public async Task GetByCollection_returns_nothing_to_a_caller_holding_only_EditCollection()
    {
        var exhibit = await SeedExhibit();
        var actor = await Actor().OnCollection(await Collection(exhibit), [CollectionPermission.EditCollection]).SeedAsync();

        var exhibits = await ReadAsync<List<Exhibit>>(await Client(actor).GetAsync($"api/collections/{exhibit.CollectionId}/exhibits", Ct));

        Assert.Empty(exhibits);
    }

    [Fact]
    public async Task GetMineByCollection_returns_the_caller_s_exhibits_of_that_collection()
    {
        var exhibit = await SeedExhibit();
        var team = await SeedTeam(exhibit);
        var actor = await Actor().OnTeam(team).SeedAsync();

        var exhibits = await ReadAsync<List<Exhibit>>(await Client(actor).GetAsync($"api/collections/{exhibit.CollectionId}/my-exhibits", Ct));

        Assert.Equal(exhibit.Id, Assert.Single(exhibits).Id);
    }

    // ---- Get -----------------------------------------------------------------------------------

    [Fact]
    public async Task Get_returns_the_exhibit_to_a_member_holding_ViewExhibit()
    {
        var exhibit = await SeedExhibit("Visible");
        var actor = await Actor().OnExhibit(exhibit, [ExhibitPermission.ViewExhibit]).SeedAsync();

        var got = await ReadAsync<Exhibit>(await Client(actor).GetAsync($"api/exhibits/{exhibit.Id}", Ct));

        Assert.Equal("Visible", got.Name);
    }

    /// <summary>A participant on one of the exhibit's teams reads it with no exhibit permission.</summary>
    [Fact]
    public async Task Get_returns_the_exhibit_to_a_participant_on_one_of_its_teams()
    {
        var exhibit = await SeedExhibit();
        var actor = await Actor().OnNewTeam(exhibit.Id).SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).GetAsync($"api/exhibits/{exhibit.Id}", Ct));
    }

    [Fact]
    public async Task Get_is_forbidden_for_a_caller_holding_ViewExhibit_only_on_another_exhibit()
    {
        var exhibit = await SeedExhibit();
        var actor = await Actor().OnNewExhibit(exhibit.CollectionId, ExhibitPermission.ViewExhibit).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/exhibits/{exhibit.Id}", Ct));
    }

    [Fact]
    public async Task Get_is_forbidden_for_a_caller_holding_only_ViewCollection_on_its_collection()
    {
        var exhibit = await SeedExhibit();
        var actor = await Actor().OnCollection(await Collection(exhibit), [CollectionPermission.ViewCollection]).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/exhibits/{exhibit.Id}", Ct));
    }

    [Fact]
    public async Task Get_reports_an_unknown_exhibit_as_not_found()
    {
        var problem = await AssertProblem(HttpStatusCode.NotFound, await RootClient.GetAsync($"api/exhibits/{Guid.NewGuid()}", Ct));

        Assert.Equal("Exhibit not found", problem.Title);
    }

    // ---- Create --------------------------------------------------------------------------------

    /// <summary>The creator becomes the exhibit's Manager, and the name and description default to the collection's.</summary>
    [Fact]
    public async Task Create_persists_the_exhibit_and_makes_the_creator_its_manager()
    {
        var collection = TestData.Collection("Source Collection");
        await Seed(collection);
        var actor = await Actor().WithSystemPermissions(SystemPermission.CreateExhibits).SeedAsync();

        var response = await Client(actor).PostAsJsonAsync("api/exhibits", new { collectionId = collection.Id }, Ct);

        await AssertStatus(HttpStatusCode.Created, response);
        var created = await ReadAsync<Exhibit>(response);
        await using var db = NewContext();
        Assert.Equal("Source Collection", (await db.Exhibits.SingleAsync(x => x.Id == created.Id, Ct)).Name);
        var membership = await db.ExhibitMemberships.SingleAsync(x => x.ExhibitId == created.Id, Ct);
        Assert.Equal((actor.Id, TestData.MembershipRoles.Manager), (membership.UserId.Value, membership.RoleId));
    }

    [Fact]
    public async Task Create_is_forbidden_for_a_caller_holding_only_CreateCollections()
    {
        var collection = TestData.Collection();
        await Seed(collection);
        var actor = await Actor().WithSystemPermissions(SystemPermission.CreateCollections).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).PostAsJsonAsync("api/exhibits", new { collectionId = collection.Id }, Ct));
    }

    [Fact]
    public async Task Create_reports_an_unknown_collection_as_not_found()
    {
        await AssertProblem(HttpStatusCode.NotFound, await RootClient.PostAsJsonAsync("api/exhibits", new { collectionId = Guid.NewGuid() }, Ct));
    }

    /// <summary>A body with no collection is answered with a 500.</summary>
    [Fact]
    public async Task Create_answers_a_body_without_a_collection_with_a_server_error()
    {
        var problem = await AssertProblem(HttpStatusCode.InternalServerError,
            await RootClient.PostAsJsonAsync("api/exhibits", new { name = "Orphan" }, Ct));

        Assert.Equal("CollectionId is required", problem.Detail);
    }

    // ---- Copy ----------------------------------------------------------------------------------

    [Fact]
    public async Task Copy_creates_an_exhibit_at_the_start_with_copies_of_the_teams()
    {
        var exhibit = await SeedExhibit(move: 2, inject: 1);
        var team = await SeedTeam(exhibit, "Copied Team");

        var response = await RootClient.PostAsync($"api/exhibits/{exhibit.Id}/copy", null, Ct);

        await AssertStatus(HttpStatusCode.Created, response);
        var copy = await ReadAsync<Exhibit>(response);
        await using var db = NewContext();
        var stored = await db.Exhibits.SingleAsync(x => x.Id == copy.Id, Ct);
        Assert.Equal((0, 0, exhibit.CollectionId), (stored.CurrentMove, stored.CurrentInject, stored.CollectionId));
        Assert.Equal("Copied Team", (await db.Teams.SingleAsync(x => x.ExhibitId == copy.Id, Ct)).Name);
    }

    [Fact]
    public async Task Copy_is_forbidden_for_a_caller_holding_only_CreateExhibits()
    {
        var exhibit = await SeedExhibit();
        var actor = await Actor().WithSystemPermissions(SystemPermission.CreateExhibits).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).PostAsync($"api/exhibits/{exhibit.Id}/copy", null, Ct));
    }

    [Fact]
    public async Task Copy_is_forbidden_for_a_caller_holding_only_ViewExhibits()
    {
        var exhibit = await SeedExhibit();
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewExhibits).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).PostAsync($"api/exhibits/{exhibit.Id}/copy", null, Ct));
    }

    [Fact]
    public async Task Copy_is_allowed_for_a_caller_holding_CreateExhibits_and_ViewExhibit_on_it()
    {
        var exhibit = await SeedExhibit();
        var actor = await Actor()
            .WithSystemPermissions(SystemPermission.CreateExhibits)
            .OnExhibit(exhibit, [ExhibitPermission.ViewExhibit])
            .SeedAsync();

        await AssertStatus(HttpStatusCode.Created, await Client(actor).PostAsync($"api/exhibits/{exhibit.Id}/copy", null, Ct));
    }

    // ---- Update --------------------------------------------------------------------------------

    [Fact]
    public async Task Update_persists_the_change_for_a_member_holding_EditExhibit()
    {
        var exhibit = await SeedExhibit("Before");
        var actor = await Actor().OnExhibit(exhibit, [ExhibitPermission.EditExhibit]).SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).PutAsJsonAsync($"api/exhibits/{exhibit.Id}", Body(exhibit, "After"), Ct));

        await using var db = NewContext();
        var stored = await db.Exhibits.SingleAsync(x => x.Id == exhibit.Id, Ct);
        Assert.Equal(("After", actor.Id), (stored.Name, stored.ModifiedBy.Value));
    }

    [Fact]
    public async Task Update_is_forbidden_for_a_caller_holding_only_ViewExhibit()
    {
        var exhibit = await SeedExhibit("Before");
        var actor = await Actor().OnExhibit(exhibit, [ExhibitPermission.ViewExhibit]).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).PutAsJsonAsync($"api/exhibits/{exhibit.Id}", Body(exhibit, "After"), Ct));

        await using var db = NewContext();
        Assert.Equal("Before", (await db.Exhibits.SingleAsync(x => x.Id == exhibit.Id, Ct)).Name);
    }

    [Fact]
    public async Task Update_is_forbidden_for_a_caller_holding_EditExhibit_only_on_another_exhibit()
    {
        var exhibit = await SeedExhibit("Before");
        var actor = await Actor().OnNewExhibit(exhibit.CollectionId, ExhibitPermission.EditExhibit).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).PutAsJsonAsync($"api/exhibits/{exhibit.Id}", Body(exhibit, "After"), Ct));
    }

    [Fact]
    public async Task Update_is_allowed_for_a_caller_holding_EditExhibits()
    {
        var exhibit = await SeedExhibit("Before");
        var actor = await Actor().WithSystemPermissions(SystemPermission.EditExhibits).SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).PutAsJsonAsync($"api/exhibits/{exhibit.Id}", Body(exhibit, "After"), Ct));
    }

    // ---- Move and inject -----------------------------------------------------------------------

    /// <summary>Moving the exhibit delivers the team articles that are now due to the team's users.</summary>
    [Fact]
    public async Task SetMoveAndInject_moves_the_exhibit_and_delivers_the_articles_now_due()
    {
        var exhibit = await SeedExhibit();
        var team = await SeedTeam(exhibit);
        var article = TestData.Article(exhibit.CollectionId, move: 1, inject: 2);
        var user = TestData.User();
        await Seed(article, user, TestData.TeamUser(team.Id, user.Id), TestData.TeamArticle(exhibit.Id, team.Id, article.Id));
        var actor = await Actor().OnExhibit(exhibit, [ExhibitPermission.ManageExhibit]).SeedAsync();

        var response = await Client(actor).PutAsync($"api/exhibits/{exhibit.Id}/move/1/inject/2", null, Ct);

        await AssertStatus(HttpStatusCode.OK, response);
        await using var db = NewContext();
        var stored = await db.Exhibits.SingleAsync(x => x.Id == exhibit.Id, Ct);
        Assert.Equal((1, 2), (stored.CurrentMove, stored.CurrentInject));
        Assert.True(await db.UserArticles.AnyAsync(x => x.UserId == user.Id && x.ArticleId == article.Id, Ct));
    }

    [Fact]
    public async Task SetMoveAndInject_is_forbidden_for_a_caller_holding_only_EditExhibit()
    {
        var exhibit = await SeedExhibit();
        var actor = await Actor().OnExhibit(exhibit, [ExhibitPermission.EditExhibit]).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).PutAsync($"api/exhibits/{exhibit.Id}/move/1/inject/0", null, Ct));

        await using var db = NewContext();
        Assert.Equal(0, (await db.Exhibits.SingleAsync(x => x.Id == exhibit.Id, Ct)).CurrentMove);
    }

    [Fact]
    public async Task SetMoveAndInject_is_forbidden_for_a_caller_holding_ManageExhibit_only_on_another_exhibit()
    {
        var exhibit = await SeedExhibit();
        var actor = await Actor().OnNewExhibit(exhibit.CollectionId, ExhibitPermission.ManageExhibit).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).PutAsync($"api/exhibits/{exhibit.Id}/move/1/inject/0", null, Ct));
    }

    [Fact]
    public async Task Advance_moves_to_the_next_move_and_inject_an_article_has()
    {
        var exhibit = await SeedExhibit();
        await Seed(TestData.Article(exhibit.CollectionId, move: 0, inject: 3), TestData.Article(exhibit.CollectionId, move: 1, inject: 0));
        var actor = await Actor().WithSystemPermissions(SystemPermission.ManageExhibits).SeedAsync();

        var advanced = await ReadAsync<Exhibit>(await Client(actor).PutAsync($"api/exhibits/{exhibit.Id}/advance", null, Ct));

        Assert.Equal((0, 3), (advanced.CurrentMove, advanced.CurrentInject));
    }

    [Fact]
    public async Task Advance_is_allowed_for_a_member_holding_ManageExhibit()
    {
        var exhibit = await SeedExhibit();
        await Seed(TestData.Article(exhibit.CollectionId, move: 1, inject: 0));
        var actor = await Actor().OnExhibit(exhibit, [ExhibitPermission.ManageExhibit]).SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).PutAsync($"api/exhibits/{exhibit.Id}/advance", null, Ct));

        await using var db = NewContext();
        Assert.Equal(1, (await db.Exhibits.SingleAsync(x => x.Id == exhibit.Id, Ct)).CurrentMove);
    }

    [Fact]
    public async Task Advance_is_forbidden_for_a_caller_holding_ManageExhibit_only_on_another_exhibit()
    {
        var exhibit = await SeedExhibit();
        await Seed(TestData.Article(exhibit.CollectionId, move: 1, inject: 0));
        var actor = await Actor().OnNewExhibit(exhibit.CollectionId, ExhibitPermission.ManageExhibit).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).PutAsync($"api/exhibits/{exhibit.Id}/advance", null, Ct));

        await using var db = NewContext();
        Assert.Equal(0, (await db.Exhibits.SingleAsync(x => x.Id == exhibit.Id, Ct)).CurrentMove);
    }

    [Fact]
    public async Task Advance_past_the_last_article_is_answered_with_a_bad_request()
    {
        var exhibit = await SeedExhibit();

        var problem = await AssertProblem(HttpStatusCode.BadRequest, await RootClient.PutAsync($"api/exhibits/{exhibit.Id}/advance", null, Ct));

        Assert.Equal("Cannot advance.", problem.Title);
    }

    [Fact]
    public async Task Advance_is_forbidden_for_a_caller_holding_only_EditExhibits()
    {
        var exhibit = await SeedExhibit();
        var actor = await Actor().WithSystemPermissions(SystemPermission.EditExhibits).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).PutAsync($"api/exhibits/{exhibit.Id}/advance", null, Ct));
    }

    // ---- Delete --------------------------------------------------------------------------------

    [Fact]
    public async Task Delete_removes_the_exhibit_and_its_teams_for_a_member_holding_ManageExhibit()
    {
        var exhibit = await SeedExhibit();
        var team = await SeedTeam(exhibit);
        var actor = await Actor().OnExhibit(exhibit, [ExhibitPermission.ManageExhibit]).SeedAsync();

        await AssertStatus(HttpStatusCode.NoContent, await Client(actor).DeleteAsync($"api/exhibits/{exhibit.Id}", Ct));

        await using var db = NewContext();
        Assert.False(await db.Exhibits.AnyAsync(x => x.Id == exhibit.Id, Ct));
        Assert.False(await db.Teams.AnyAsync(x => x.Id == team.Id, Ct));
    }

    [Fact]
    public async Task Delete_is_forbidden_for_a_caller_holding_only_EditExhibit()
    {
        var exhibit = await SeedExhibit();
        var actor = await Actor().OnExhibit(exhibit, [ExhibitPermission.EditExhibit]).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).DeleteAsync($"api/exhibits/{exhibit.Id}", Ct));

        await using var db = NewContext();
        Assert.True(await db.Exhibits.AnyAsync(x => x.Id == exhibit.Id, Ct));
    }

    [Fact]
    public async Task Delete_is_forbidden_for_a_caller_holding_only_ManageCollection_on_its_collection()
    {
        var exhibit = await SeedExhibit();
        var actor = await Actor().OnCollection(await Collection(exhibit), [CollectionPermission.ManageCollection]).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).DeleteAsync($"api/exhibits/{exhibit.Id}", Ct));
    }

    [Fact]
    public async Task Delete_reports_an_unknown_exhibit_as_not_found()
    {
        await AssertProblem(HttpStatusCode.NotFound, await RootClient.DeleteAsync($"api/exhibits/{Guid.NewGuid()}", Ct));
    }

    /// <summary>An exhibit holding an article a participant posted is refused with a 400, and kept.</summary>
    [Fact]
    public async Task Delete_of_an_exhibit_with_a_posted_article_is_answered_with_a_bad_request()
    {
        var exhibit = await SeedExhibit();
        await Seed(TestData.Article(exhibit.CollectionId, exhibitId: exhibit.Id));

        await AssertProblem(HttpStatusCode.BadRequest, await RootClient.DeleteAsync($"api/exhibits/{exhibit.Id}", Ct));

        await using var db = NewContext();
        Assert.True(await db.Exhibits.AnyAsync(x => x.Id == exhibit.Id, Ct));
    }

    // ---- Download / Upload ---------------------------------------------------------------------

    [Fact]
    public async Task DownloadJson_returns_the_exhibit_file_to_a_member_holding_ManageExhibit()
    {
        var exhibit = await SeedExhibit(createdBy: Root.Id);
        await SeedTeam(exhibit, "Exported Team");
        var actor = await Actor().OnExhibit(exhibit, [ExhibitPermission.ManageExhibit]).SeedAsync();

        var response = await Client(actor).GetAsync($"api/exhibits/{exhibit.Id}/json", Ct);

        await AssertStatus(HttpStatusCode.OK, response);
        Assert.Contains("Exported Team", await response.Content.ReadAsStringAsync(Ct));
    }

    /// <summary>The file name's date part reads the literal letters YYYY and DD around the minute of creation.</summary>
    [Fact]
    public async Task DownloadJson_names_the_file_with_a_literal_date_pattern()
    {
        var exhibit = await SeedExhibit(createdBy: Root.Id);

        var response = await RootClient.GetAsync($"api/exhibits/{exhibit.Id}/json", Ct);

        await AssertStatus(HttpStatusCode.OK, response);
        Assert.Matches(@"^Test Collection-YYYY\d\dDD-Root\.json$", response.Content.Headers.ContentDisposition?.FileName);
    }

    /// <summary>An exhibit whose CreatedBy names no user is answered with a 500.</summary>
    [Fact]
    public async Task DownloadJson_of_an_exhibit_whose_creator_is_not_a_user_answers_a_server_error()
    {
        var exhibit = await SeedExhibit();

        var problem = await AssertProblem(HttpStatusCode.InternalServerError, await RootClient.GetAsync($"api/exhibits/{exhibit.Id}/json", Ct));

        Assert.Equal("Object reference not set to an instance of an object.", problem.Detail);
    }

    [Fact]
    public async Task DownloadJson_is_forbidden_for_a_caller_holding_only_ViewExhibit()
    {
        var exhibit = await SeedExhibit();
        var actor = await Actor().OnExhibit(exhibit, [ExhibitPermission.ViewExhibit]).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/exhibits/{exhibit.Id}/json", Ct));
    }

    [Fact]
    public async Task UploadJson_creates_a_new_collection_and_exhibit_from_a_downloaded_file()
    {
        var exhibit = await SeedExhibit(createdBy: Root.Id);
        var file = await (await RootClient.GetAsync($"api/exhibits/{exhibit.Id}/json", Ct)).Content.ReadAsByteArrayAsync(Ct);

        var uploaded = await ReadAsync<Exhibit>(await RootClient.PostAsync("api/exhibits/json", Upload(file), Ct));

        Assert.NotEqual(exhibit.CollectionId, uploaded.CollectionId);
        await using var context = NewContext();
        Assert.True(await context.Collections.AnyAsync(x => x.Id == uploaded.CollectionId, Ct));
    }

    [Fact]
    public async Task UploadJson_creates_the_exhibit_for_a_caller_holding_CreateExhibits()
    {
        var exhibit = await SeedExhibit(createdBy: Root.Id);
        var file = await (await RootClient.GetAsync($"api/exhibits/{exhibit.Id}/json", Ct)).Content.ReadAsByteArrayAsync(Ct);
        var actor = await Actor().WithSystemPermissions(SystemPermission.CreateExhibits).SeedAsync();

        var uploaded = await ReadAsync<Exhibit>(await Client(actor).PostAsync("api/exhibits/json", Upload(file), Ct));

        await using var db = NewContext();
        Assert.Equal(actor.Id, (await db.Exhibits.SingleAsync(x => x.Id == uploaded.Id, Ct)).CreatedBy);
    }

    [Fact]
    public async Task UploadJson_is_forbidden_for_a_caller_holding_only_CreateCollections()
    {
        var actor = await Actor().WithSystemPermissions(SystemPermission.CreateCollections).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).PostAsync("api/exhibits/json", Upload("{}"u8.ToArray()), Ct));
    }

    [Fact]
    public async Task UploadJson_answers_a_file_without_an_exhibit_with_a_bad_request()
    {
        var problem = await AssertProblem(HttpStatusCode.BadRequest, await RootClient.PostAsync("api/exhibits/json", Upload("{}"u8.ToArray()), Ct));

        Assert.Equal("The uploaded file is not a valid exhibit file.", problem.Title);
    }

    private async Task<ExhibitEntity> SeedExhibit(string name = "Test Exhibit", int move = 0, int inject = 0, Guid createdBy = default)
    {
        var collection = TestData.Collection();
        var exhibit = TestData.Exhibit(collection.Id, name, move, inject);
        exhibit.CreatedBy = createdBy;
        await Seed(collection, exhibit);

        return exhibit;
    }

    private async Task<TeamEntity> SeedTeam(ExhibitEntity exhibit, string name = "Test Team")
    {
        var team = TestData.Team(exhibit.Id, name);
        await Seed(team);

        return team;
    }

    private async Task<CollectionEntity> Collection(ExhibitEntity exhibit) =>
        await Db.Collections.SingleAsync(x => x.Id == exhibit.CollectionId, Ct);

    private static object Body(ExhibitEntity exhibit, string name) =>
        new { id = exhibit.Id, collectionId = exhibit.CollectionId, name, currentMove = exhibit.CurrentMove, currentInject = exhibit.CurrentInject };

    private static MultipartFormDataContent Upload(byte[] file)
    {
        var content = new ByteArrayContent(file);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        return new MultipartFormDataContent { { content, "ToUpload", "exhibit.json" } };
    }
}
