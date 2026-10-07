// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Gallery.Api.Data.Enumerations;
using Gallery.Api.Data.Models;
using Gallery.Api.Tests.Support;
using Gallery.Api.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Gallery.Api.Tests.Controllers;

/// <summary>
/// <c>CollectionController</c> over HTTP. A collection gate passes on the system permission or on the
/// matching collection permission of a membership; the near misses are a neighbouring permission on the
/// collection and the right permission on another collection.
/// </summary>
public class CollectionControllerTests(DatabaseFixture fixture, GalleryAppFactory factory) : ApiTestBase(fixture, factory)
{
    // ---- GetAll / GetMine ----------------------------------------------------------------------

    [Fact]
    public async Task GetAll_returns_every_collection_to_a_caller_holding_ViewCollections()
    {
        var collection = await SeedCollection();
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewCollections).SeedAsync();

        var collections = await ReadAsync<List<Collection>>(await Client(actor).GetAsync("api/collections", Ct));

        Assert.Contains(collections, x => x.Id == collection.Id);
    }

    /// <summary>Without ViewCollections the list is the caller's own memberships, with their permissions attached.</summary>
    [Fact]
    public async Task GetAll_returns_only_the_member_s_collections_with_their_permissions()
    {
        var mine = await SeedCollection("Mine");
        var other = await SeedCollection("Other");
        var actor = await Actor().OnCollection(mine, [CollectionPermission.ViewCollection]).SeedAsync();

        var collections = await ReadAsync<List<Collection>>(await Client(actor).GetAsync("api/collections", Ct));

        var listed = Assert.Single(collections);
        Assert.Equal(mine.Id, listed.Id);
        Assert.Equal(["ViewCollection"], listed.CollectionPermissions);
    }

    /// <summary>A request with no identity is answered with a 401; the anonymous client is the case under test.</summary>
    [Fact]
    public async Task GetAll_without_an_identity_is_unauthorized()
    {
        await AssertStatus(HttpStatusCode.Unauthorized, await Client().GetAsync("api/collections", Ct));
    }

    [Fact]
    public async Task GetMine_returns_the_collections_of_the_caller_s_exhibit_teams()
    {
        var collection = await SeedCollection("Played");
        var exhibit = TestData.Exhibit(collection.Id);
        var team = TestData.Team(exhibit.Id);
        await Seed(exhibit, team);
        await SeedCollection("Unplayed");
        var actor = await Actor().OnTeam(team).SeedAsync();

        var collections = await ReadAsync<List<Collection>>(await Client(actor).GetAsync("api/my-collections", Ct));

        Assert.Equal(collection.Id, Assert.Single(collections).Id);
    }

    // ---- Get -----------------------------------------------------------------------------------

    [Fact]
    public async Task Get_returns_the_collection_to_a_caller_holding_ViewCollections()
    {
        var collection = await SeedCollection("Readable");
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewCollections).SeedAsync();

        var got = await ReadAsync<Collection>(await Client(actor).GetAsync($"api/collections/{collection.Id}", Ct));

        Assert.Equal("Readable", got.Name);
    }

    [Fact]
    public async Task Get_returns_the_collection_to_a_member_holding_ViewCollection()
    {
        var collection = await SeedCollection();
        var actor = await Actor().OnCollection(collection, [CollectionPermission.ViewCollection]).SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).GetAsync($"api/collections/{collection.Id}", Ct));
    }

    [Fact]
    public async Task Get_is_forbidden_for_a_caller_holding_only_EditCollection()
    {
        var collection = await SeedCollection();
        var actor = await Actor().OnCollection(collection, [CollectionPermission.EditCollection]).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/collections/{collection.Id}", Ct));
    }

    [Fact]
    public async Task Get_is_forbidden_for_a_caller_holding_ViewCollection_only_on_another_collection()
    {
        var collection = await SeedCollection();
        var actor = await Actor().OnNewCollection(CollectionPermission.ViewCollection).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/collections/{collection.Id}", Ct));
    }

    [Fact]
    public async Task Get_is_forbidden_for_a_caller_holding_only_ViewExhibits()
    {
        var collection = await SeedCollection();
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewExhibits).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/collections/{collection.Id}", Ct));
    }

    [Fact]
    public async Task Get_reports_an_unknown_collection_as_not_found()
    {
        await AssertProblem(HttpStatusCode.NotFound, await RootClient.GetAsync($"api/collections/{Guid.NewGuid()}", Ct));
    }

    // ---- Create --------------------------------------------------------------------------------

    /// <summary>The creator becomes the collection's Manager, which is what lets a Content Developer edit what they made.</summary>
    [Fact]
    public async Task Create_persists_the_collection_and_makes_the_creator_its_manager()
    {
        var actor = await Actor().WithSystemPermissions(SystemPermission.CreateCollections).SeedAsync();

        var response = await Client(actor).PostAsJsonAsync("api/collections", new { name = "Created", description = "New" }, Ct);

        await AssertStatus(HttpStatusCode.Created, response);
        var created = await ReadAsync<Collection>(response);
        await using var db = NewContext();
        Assert.Equal(actor.Id, (await db.Collections.SingleAsync(x => x.Id == created.Id, Ct)).CreatedBy);
        var membership = await db.CollectionMemberships.SingleAsync(x => x.CollectionId == created.Id, Ct);
        Assert.Equal((actor.Id, TestData.MembershipRoles.Manager), (membership.UserId.Value, membership.RoleId));
    }

    [Fact]
    public async Task Create_is_forbidden_for_a_caller_holding_only_ViewCollections()
    {
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewCollections).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).PostAsJsonAsync("api/collections", new { name = "Nope" }, Ct));
    }

    // ---- Copy ----------------------------------------------------------------------------------

    [Fact]
    public async Task Copy_creates_a_collection_with_copies_of_the_cards_and_articles()
    {
        var collection = await SeedCollection("Original");
        var card = TestData.Card(collection.Id);
        var article = TestData.Article(collection.Id, card.Id, name: "Copied Article");
        await Seed(card, article);

        var response = await RootClient.PostAsync($"api/collections/{collection.Id}/copy", null, Ct);

        await AssertStatus(HttpStatusCode.Created, response);
        var copy = await ReadAsync<Collection>(response);
        Assert.Equal("Original - Root", copy.Name);
        await using var db = NewContext();
        var copiedCard = await db.Cards.SingleAsync(x => x.CollectionId == copy.Id, Ct);
        var copiedArticle = await db.Articles.SingleAsync(x => x.CollectionId == copy.Id, Ct);
        Assert.Equal(copiedCard.Id, copiedArticle.CardId);
    }

    /// <summary>An article posted during an exhibit is copied too, still naming the source collection's exhibit.</summary>
    [Fact]
    public async Task Copy_copies_an_exhibit_s_posted_article_into_the_new_collection()
    {
        var collection = await SeedCollection("Played");
        var exhibit = TestData.Exhibit(collection.Id);
        await Seed(exhibit, TestData.Article(collection.Id, exhibitId: exhibit.Id, name: "Posted During Play"));

        var copy = await ReadAsync<Collection>(await RootClient.PostAsync($"api/collections/{collection.Id}/copy", null, Ct));

        await using var db = NewContext();
        var copied = await db.Articles.SingleAsync(x => x.CollectionId == copy.Id, Ct);
        Assert.Equal(exhibit.Id, copied.ExhibitId);
    }

    [Fact]
    public async Task Copy_is_allowed_for_a_caller_holding_CreateCollections_and_ViewCollection_on_it()
    {
        var collection = await SeedCollection();
        var actor = await Actor()
            .WithSystemPermissions(SystemPermission.CreateCollections)
            .OnCollection(collection, [CollectionPermission.ViewCollection])
            .SeedAsync();

        await AssertStatus(HttpStatusCode.Created, await Client(actor).PostAsync($"api/collections/{collection.Id}/copy", null, Ct));
    }

    [Fact]
    public async Task Copy_is_forbidden_for_a_caller_holding_only_CreateCollections()
    {
        var collection = await SeedCollection();
        var actor = await Actor().WithSystemPermissions(SystemPermission.CreateCollections).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).PostAsync($"api/collections/{collection.Id}/copy", null, Ct));
    }

    [Fact]
    public async Task Copy_is_forbidden_for_a_caller_holding_only_ViewCollections()
    {
        var collection = await SeedCollection();
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewCollections).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).PostAsync($"api/collections/{collection.Id}/copy", null, Ct));
    }

    [Fact]
    public async Task Copy_reports_an_unknown_collection_as_not_found()
    {
        await AssertProblem(HttpStatusCode.NotFound, await RootClient.PostAsync($"api/collections/{Guid.NewGuid()}/copy", null, Ct));
    }

    // ---- Update --------------------------------------------------------------------------------

    [Fact]
    public async Task Update_persists_the_change_for_a_member_holding_EditCollection()
    {
        var collection = await SeedCollection("Before");
        var actor = await Actor().OnCollection(collection, [CollectionPermission.EditCollection]).SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).PutAsJsonAsync($"api/collections/{collection.Id}",
            new { id = collection.Id, name = "After", description = "Edited" }, Ct));

        await using var db = NewContext();
        var stored = await db.Collections.SingleAsync(x => x.Id == collection.Id, Ct);
        Assert.Equal(("After", actor.Id), (stored.Name, stored.ModifiedBy.Value));
    }

    [Fact]
    public async Task Update_is_allowed_for_a_caller_holding_EditCollections()
    {
        var collection = await SeedCollection("Before");
        var actor = await Actor().WithSystemPermissions(SystemPermission.EditCollections).SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).PutAsJsonAsync($"api/collections/{collection.Id}",
            new { id = collection.Id, name = "After" }, Ct));
    }

    [Fact]
    public async Task Update_is_forbidden_for_a_caller_holding_only_ViewCollection()
    {
        var collection = await SeedCollection("Before");
        var actor = await Actor().OnCollection(collection, [CollectionPermission.ViewCollection]).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).PutAsJsonAsync($"api/collections/{collection.Id}",
            new { id = collection.Id, name = "After" }, Ct));

        await using var db = NewContext();
        Assert.Equal("Before", (await db.Collections.SingleAsync(x => x.Id == collection.Id, Ct)).Name);
    }

    [Fact]
    public async Task Update_is_forbidden_for_a_caller_holding_EditCollection_only_on_another_collection()
    {
        var collection = await SeedCollection("Before");
        var actor = await Actor().OnNewCollection(CollectionPermission.EditCollection).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).PutAsJsonAsync($"api/collections/{collection.Id}",
            new { id = collection.Id, name = "After" }, Ct));
    }

    // ---- Delete --------------------------------------------------------------------------------

    [Fact]
    public async Task Delete_removes_the_collection_and_its_cards_for_a_member_holding_ManageCollection()
    {
        var collection = await SeedCollection();
        var card = TestData.Card(collection.Id);
        await Seed(card);
        var actor = await Actor().OnCollection(collection, [CollectionPermission.ManageCollection]).SeedAsync();

        await AssertStatus(HttpStatusCode.NoContent, await Client(actor).DeleteAsync($"api/collections/{collection.Id}", Ct));

        await using var db = NewContext();
        Assert.False(await db.Collections.AnyAsync(x => x.Id == collection.Id, Ct));
        Assert.False(await db.Cards.AnyAsync(x => x.Id == card.Id, Ct));
    }

    [Fact]
    public async Task Delete_is_allowed_for_a_caller_holding_ManageCollections()
    {
        var collection = await SeedCollection();
        var actor = await Actor().WithSystemPermissions(SystemPermission.ManageCollections).SeedAsync();

        await AssertStatus(HttpStatusCode.NoContent, await Client(actor).DeleteAsync($"api/collections/{collection.Id}", Ct));
    }

    [Fact]
    public async Task Delete_is_forbidden_for_a_caller_holding_only_EditCollection()
    {
        var collection = await SeedCollection();
        var actor = await Actor().OnCollection(collection, [CollectionPermission.EditCollection]).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).DeleteAsync($"api/collections/{collection.Id}", Ct));

        await using var db = NewContext();
        Assert.True(await db.Collections.AnyAsync(x => x.Id == collection.Id, Ct));
    }

    [Fact]
    public async Task Delete_is_forbidden_for_a_caller_holding_ManageCollection_only_on_another_collection()
    {
        var collection = await SeedCollection();
        var actor = await Actor().OnNewCollection(CollectionPermission.ManageCollection).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).DeleteAsync($"api/collections/{collection.Id}", Ct));
    }

    [Fact]
    public async Task Delete_reports_an_unknown_collection_as_not_found()
    {
        await AssertProblem(HttpStatusCode.NotFound, await RootClient.DeleteAsync($"api/collections/{Guid.NewGuid()}", Ct));
    }

    // ---- Download / Upload ---------------------------------------------------------------------

    [Fact]
    public async Task DownloadJson_returns_the_collection_file_to_a_member_holding_ViewCollection()
    {
        var collection = await SeedCollection("Exported");
        await Seed(TestData.Card(collection.Id, "Exported Card"));
        var actor = await Actor().OnCollection(collection, [CollectionPermission.ViewCollection]).SeedAsync();

        var response = await Client(actor).GetAsync($"api/collections/{collection.Id}/json", Ct);

        await AssertStatus(HttpStatusCode.OK, response);
        Assert.Equal("Exported.json", response.Content.Headers.ContentDisposition?.FileName);
        Assert.Contains("Exported Card", await response.Content.ReadAsStringAsync(Ct));
    }

    [Fact]
    public async Task DownloadJson_is_forbidden_for_a_caller_holding_ViewCollection_only_on_another_collection()
    {
        var collection = await SeedCollection();
        var actor = await Actor().OnNewCollection(CollectionPermission.ViewCollection).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/collections/{collection.Id}/json", Ct));
    }

    /// <summary>A downloaded file uploads as a new collection holding the same cards.</summary>
    [Fact]
    public async Task UploadJson_creates_a_copy_of_a_downloaded_collection()
    {
        var collection = await SeedCollection("Round Trip");
        await Seed(TestData.Card(collection.Id, "Round Trip Card"));
        var file = await (await RootClient.GetAsync($"api/collections/{collection.Id}/json", Ct)).Content.ReadAsByteArrayAsync(Ct);

        var uploaded = await ReadAsync<Collection>(await RootClient.PostAsync("api/collections/json", Upload(file), Ct));

        await using var db = NewContext();
        Assert.Equal("Round Trip Card", (await db.Cards.SingleAsync(x => x.CollectionId == uploaded.Id, Ct)).Name);
    }

    [Fact]
    public async Task UploadJson_creates_the_collection_for_a_caller_holding_CreateCollections()
    {
        var collection = await SeedCollection("Uploaded");
        var file = await (await RootClient.GetAsync($"api/collections/{collection.Id}/json", Ct)).Content.ReadAsByteArrayAsync(Ct);
        var actor = await Actor().WithSystemPermissions(SystemPermission.CreateCollections).SeedAsync();

        var uploaded = await ReadAsync<Collection>(await Client(actor).PostAsync("api/collections/json", Upload(file), Ct));

        await using var db = NewContext();
        Assert.Equal(actor.Id, (await db.Collections.SingleAsync(x => x.Id == uploaded.Id, Ct)).CreatedBy);
    }

    [Fact]
    public async Task UploadJson_is_forbidden_for_a_caller_holding_only_ViewCollections()
    {
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewCollections).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden,
            await Client(actor).PostAsync("api/collections/json", Upload(Encoding.UTF8.GetBytes("{}")), Ct));
    }

    [Fact]
    public async Task UploadJson_answers_a_file_that_is_not_json_with_a_bad_request()
    {
        var problem = await AssertProblem(HttpStatusCode.BadRequest,
            await RootClient.PostAsync("api/collections/json", Upload(Encoding.UTF8.GetBytes("not json")), Ct));

        Assert.Equal("The uploaded file is not valid JSON.", problem.Title);
    }

    private async Task<CollectionEntity> SeedCollection(string name = "Test Collection")
    {
        var collection = TestData.Collection(name);
        await Seed(collection);

        return collection;
    }

    private static MultipartFormDataContent Upload(byte[] file)
    {
        var content = new ByteArrayContent(file);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        return new MultipartFormDataContent { { content, "ToUpload", "collection.json" } };
    }
}
