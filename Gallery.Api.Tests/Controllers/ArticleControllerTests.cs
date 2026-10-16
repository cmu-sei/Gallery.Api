// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Gallery.Api.Data.Enumerations;
using Gallery.Api.Data.Models;
using Gallery.Api.Tests.Support;
using Gallery.Api.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Gallery.Api.Tests.Controllers;

/// <summary>
/// <c>ArticleController</c> over HTTP. A collection article is gated on its collection, an article a
/// participant posted during an exhibit on that exhibit, and a participant may post where one of their
/// team's cards allows it.
/// </summary>
public class ArticleControllerTests(DatabaseFixture fixture, GalleryAppFactory factory) : ApiTestBase(fixture, factory)
{
    // ---- Lists ---------------------------------------------------------------------------------

    [Fact]
    public async Task GetByCard_returns_the_card_s_collection_articles_to_a_member_holding_ViewCollection()
    {
        var (collection, card, article) = await SeedArticle();
        var actor = await Actor().OnCollection(collection, [CollectionPermission.ViewCollection]).SeedAsync();

        var articles = await ReadAsync<List<Article>>(await Client(actor).GetAsync($"api/cards/{card.Id}/articles", Ct));

        Assert.Equal(article.Id, Assert.Single(articles).Id);
    }

    [Fact]
    public async Task GetByCard_is_forbidden_for_a_caller_holding_ViewCollection_only_on_another_collection()
    {
        var (_, card, _) = await SeedArticle();
        var actor = await Actor().OnNewCollection(CollectionPermission.ViewCollection).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/cards/{card.Id}/articles", Ct));
    }

    [Fact]
    public async Task GetByCard_returns_the_card_s_collection_articles_to_a_caller_holding_ViewCollections()
    {
        var (_, card, article) = await SeedArticle();
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewCollections).SeedAsync();

        var articles = await ReadAsync<List<Article>>(await Client(actor).GetAsync($"api/cards/{card.Id}/articles", Ct));

        Assert.Equal(article.Id, Assert.Single(articles).Id);
    }

    [Fact]
    public async Task GetByCard_is_forbidden_for_a_caller_holding_only_ViewExhibits()
    {
        var (_, card, _) = await SeedArticle();
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewExhibits).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/cards/{card.Id}/articles", Ct));
    }

    [Fact]
    public async Task GetByCard_reports_an_unknown_card_as_not_found()
    {
        await AssertProblem(HttpStatusCode.NotFound, await RootClient.GetAsync($"api/cards/{Guid.NewGuid()}/articles", Ct));
    }

    /// <summary>A request with no identity is answered with a 401; the anonymous client is the case under test.</summary>
    [Fact]
    public async Task GetByCard_without_an_identity_is_unauthorized()
    {
        await AssertStatus(HttpStatusCode.Unauthorized, await Client().GetAsync($"api/cards/{Guid.NewGuid()}/articles", Ct));
    }

    [Fact]
    public async Task GetByCollection_returns_the_collection_articles_to_a_caller_holding_ViewCollections()
    {
        var (collection, _, article) = await SeedArticle();
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewCollections).SeedAsync();

        var articles = await ReadAsync<List<Article>>(await Client(actor).GetAsync($"api/collections/{collection.Id}/articles", Ct));

        Assert.Equal(article.Id, Assert.Single(articles).Id);
    }

    [Fact]
    public async Task GetByCollection_returns_the_collection_articles_to_a_member_holding_ViewCollection()
    {
        var (collection, _, article) = await SeedArticle();
        var actor = await Actor().OnCollection(collection, [CollectionPermission.ViewCollection]).SeedAsync();

        var articles = await ReadAsync<List<Article>>(await Client(actor).GetAsync($"api/collections/{collection.Id}/articles", Ct));

        Assert.Equal(article.Id, Assert.Single(articles).Id);
    }

    [Fact]
    public async Task GetByCollection_is_forbidden_for_a_caller_holding_only_EditCollection()
    {
        var (collection, _, _) = await SeedArticle();
        var actor = await Actor().OnCollection(collection, [CollectionPermission.EditCollection]).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/collections/{collection.Id}/articles", Ct));
    }

    [Fact]
    public async Task GetByCollection_is_forbidden_for_a_caller_holding_only_ViewExhibits()
    {
        var (collection, _, _) = await SeedArticle();
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewExhibits).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/collections/{collection.Id}/articles", Ct));
    }

    /// <summary>The articles due at the exhibit's move and inject, posted ones included.</summary>
    [Fact]
    public async Task GetByExhibit_returns_the_articles_due_to_a_caller_holding_ViewExhibits()
    {
        var (collection, card, due) = await SeedArticle();
        var exhibit = TestData.Exhibit(collection.Id);
        await Seed(exhibit, TestData.Article(collection.Id, card.Id, move: 1, name: "Not Yet"));
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewExhibits).SeedAsync();

        var articles = await ReadAsync<List<Article>>(await Client(actor).GetAsync($"api/exhibits/{exhibit.Id}/articles", Ct));

        Assert.Equal(due.Id, Assert.Single(articles).Id);
    }

    /// <summary>A member holding ViewExhibit on the exhibit is answered with a 500.</summary>
    [Fact]
    public async Task GetByExhibit_answers_a_member_holding_ViewExhibit_with_a_server_error()
    {
        var (collection, _, _) = await SeedArticle();
        var exhibit = TestData.Exhibit(collection.Id);
        await Seed(exhibit);
        var actor = await Actor().OnExhibit(exhibit, [ExhibitPermission.ViewExhibit]).SeedAsync();

        var problem = await AssertProblem(HttpStatusCode.InternalServerError, await Client(actor).GetAsync($"api/exhibits/{exhibit.Id}/articles", Ct));

        Assert.Equal("Handler for type Collection is not implemented.", problem.Detail);
    }

    // Same case as GetByExhibit_answers_a_member_holding_ViewExhibit_with_a_server_error.
    /// <summary>A caller holding ViewExhibit only on another exhibit is answered with a 500.</summary>
    [Fact]
    public async Task GetByExhibit_answers_a_caller_holding_ViewExhibit_only_on_another_exhibit_with_a_server_error()
    {
        var (collection, _, _) = await SeedArticle();
        var exhibit = TestData.Exhibit(collection.Id);
        await Seed(exhibit);
        var actor = await Actor().OnNewExhibit(collection.Id, ExhibitPermission.ViewExhibit).SeedAsync();

        var problem = await AssertProblem(HttpStatusCode.InternalServerError, await Client(actor).GetAsync($"api/exhibits/{exhibit.Id}/articles", Ct));

        Assert.Equal("Handler for type Collection is not implemented.", problem.Detail);
    }

    // Same case as GetByExhibit_answers_a_member_holding_ViewExhibit_with_a_server_error.
    /// <summary>A caller holding only ViewCollections is answered with a 500.</summary>
    [Fact]
    public async Task GetByExhibit_answers_a_caller_holding_only_ViewCollections_with_a_server_error()
    {
        var (collection, _, _) = await SeedArticle();
        var exhibit = TestData.Exhibit(collection.Id);
        await Seed(exhibit);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewCollections).SeedAsync();

        var problem = await AssertProblem(HttpStatusCode.InternalServerError, await Client(actor).GetAsync($"api/exhibits/{exhibit.Id}/articles", Ct));

        Assert.Equal("Handler for type Collection is not implemented.", problem.Detail);
    }

    // ---- Get -----------------------------------------------------------------------------------

    [Fact]
    public async Task Get_returns_a_collection_article_to_a_member_holding_ViewCollection()
    {
        var (collection, _, article) = await SeedArticle();
        var actor = await Actor().OnCollection(collection, [CollectionPermission.ViewCollection]).SeedAsync();

        var got = await ReadAsync<Article>(await Client(actor).GetAsync($"api/articles/{article.Id}", Ct));

        Assert.Equal(article.Name, got.Name);
    }

    [Fact]
    public async Task Get_is_forbidden_for_a_caller_holding_ViewCollection_only_on_another_collection()
    {
        var (_, _, article) = await SeedArticle();
        var actor = await Actor().OnNewCollection(CollectionPermission.ViewCollection).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/articles/{article.Id}", Ct));
    }

    [Fact]
    public async Task Get_returns_a_collection_article_to_a_caller_holding_ViewCollections()
    {
        var (_, _, article) = await SeedArticle();
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewCollections).SeedAsync();

        var got = await ReadAsync<Article>(await Client(actor).GetAsync($"api/articles/{article.Id}", Ct));

        Assert.Equal(article.Name, got.Name);
    }

    [Fact]
    public async Task Get_of_a_collection_article_is_forbidden_for_a_caller_holding_only_ViewExhibits()
    {
        var (_, _, article) = await SeedArticle();
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewExhibits).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/articles/{article.Id}", Ct));
    }

    [Fact]
    public async Task Get_returns_a_posted_article_to_a_caller_holding_ViewExhibits()
    {
        var (collection, card, _) = await SeedArticle();
        var exhibit = TestData.Exhibit(collection.Id);
        var posted = TestData.Article(collection.Id, card.Id, exhibit.Id, "Posted");
        await Seed(exhibit, posted);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewExhibits).SeedAsync();

        var got = await ReadAsync<Article>(await Client(actor).GetAsync($"api/articles/{posted.Id}", Ct));

        Assert.Equal((posted.Id, exhibit.Id), (got.Id, got.ExhibitId));
    }

    [Fact]
    public async Task Get_of_a_posted_article_is_forbidden_for_a_caller_holding_only_ViewCollections()
    {
        var (collection, card, _) = await SeedArticle();
        var exhibit = TestData.Exhibit(collection.Id);
        var posted = TestData.Article(collection.Id, card.Id, exhibit.Id, "Posted");
        await Seed(exhibit, posted);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewCollections).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/articles/{posted.Id}", Ct));
    }

    [Fact]
    public async Task Get_returns_a_posted_article_to_a_member_holding_ViewExhibit()
    {
        var (collection, card, _) = await SeedArticle();
        var exhibit = TestData.Exhibit(collection.Id);
        var posted = TestData.Article(collection.Id, card.Id, exhibit.Id, "Posted");
        await Seed(exhibit, posted);
        var actor = await Actor().OnExhibit(exhibit, [ExhibitPermission.ViewExhibit]).SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).GetAsync($"api/articles/{posted.Id}", Ct));
    }

    [Fact]
    public async Task Get_is_forbidden_for_a_caller_holding_ViewExhibit_only_on_another_exhibit()
    {
        var (collection, card, _) = await SeedArticle();
        var exhibit = TestData.Exhibit(collection.Id);
        var posted = TestData.Article(collection.Id, card.Id, exhibit.Id, "Posted");
        await Seed(exhibit, posted);
        var actor = await Actor().OnNewExhibit(collection.Id, ExhibitPermission.ViewExhibit).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/articles/{posted.Id}", Ct));
    }

    /// <summary>A participant with no exhibit or collection permission opens an article delivered to them.</summary>
    [Fact]
    public async Task Get_returns_an_article_delivered_to_the_caller()
    {
        var (collection, _, article) = await SeedArticle();
        var exhibit = TestData.Exhibit(collection.Id);
        var team = TestData.Team(exhibit.Id);
        await Seed(exhibit, team);
        var actor = await Actor().OnTeam(team).SeedAsync();
        await Seed(TestData.UserArticle(exhibit.Id, actor.Id, article.Id));

        await AssertStatus(HttpStatusCode.OK, await Client(actor).GetAsync($"api/articles/{article.Id}", Ct));
    }

    [Fact]
    public async Task Get_is_forbidden_for_a_participant_to_whom_the_article_was_not_delivered()
    {
        var (collection, _, article) = await SeedArticle();
        var exhibit = TestData.Exhibit(collection.Id);
        await Seed(exhibit);
        var actor = await Actor().OnNewTeam(exhibit.Id).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/articles/{article.Id}", Ct));
    }

    [Fact]
    public async Task Get_reports_an_unknown_article_as_not_found()
    {
        await AssertProblem(HttpStatusCode.NotFound, await RootClient.GetAsync($"api/articles/{Guid.NewGuid()}", Ct));
    }

    // ---- Create --------------------------------------------------------------------------------

    [Fact]
    public async Task Create_persists_a_collection_article_for_a_member_holding_EditCollection()
    {
        var (collection, card, _) = await SeedArticle();
        var actor = await Actor().OnCollection(collection, [CollectionPermission.EditCollection]).SeedAsync();

        var response = await Client(actor).PostAsJsonAsync("api/articles", NewArticle(collection.Id, card.Id), Ct);

        await AssertStatus(HttpStatusCode.Created, response);
        var created = await ReadAsync<Article>(response);
        await using var db = NewContext();
        var stored = await db.Articles.SingleAsync(x => x.Id == created.Id, Ct);
        Assert.Equal((collection.Id, DateTimeKind.Utc), (stored.CollectionId, stored.DatePosted.Kind));
    }

    [Fact]
    public async Task Create_of_a_collection_article_is_forbidden_for_a_caller_holding_only_ViewCollection()
    {
        var (collection, card, _) = await SeedArticle();
        var actor = await Actor().OnCollection(collection, [CollectionPermission.ViewCollection]).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).PostAsJsonAsync("api/articles", NewArticle(collection.Id, card.Id), Ct));
    }

    [Fact]
    public async Task Create_persists_a_collection_article_for_a_caller_holding_EditCollections()
    {
        var (collection, card, _) = await SeedArticle();
        var actor = await Actor().WithSystemPermissions(SystemPermission.EditCollections).SeedAsync();

        var response = await Client(actor).PostAsJsonAsync("api/articles", NewArticle(collection.Id, card.Id), Ct);

        await AssertStatus(HttpStatusCode.Created, response);
        var created = await ReadAsync<Article>(response);
        await using var db = NewContext();
        var stored = await db.Articles.SingleAsync(x => x.Id == created.Id, Ct);
        Assert.Equal((collection.Id, (Guid?)null), (stored.CollectionId, stored.ExhibitId));
    }

    [Fact]
    public async Task Create_of_a_collection_article_is_forbidden_for_a_caller_holding_only_ViewCollections()
    {
        var (collection, card, _) = await SeedArticle();
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewCollections).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).PostAsJsonAsync("api/articles",
            NewArticle(collection.Id, card.Id, name: "Refused"), Ct));

        await using var db = NewContext();
        Assert.False(await db.Articles.AnyAsync(x => x.Name == "Refused", Ct));
    }

    /// <summary>
    /// A participant whose team card allows posting posts an article into the exhibit, and it is delivered
    /// to the users of every team that has the card.
    /// </summary>
    [Fact]
    public async Task Create_lets_a_participant_post_where_their_team_card_allows_it_and_delivers_it()
    {
        var (collection, card, _) = await SeedArticle();
        var exhibit = TestData.Exhibit(collection.Id);
        var team = TestData.Team(exhibit.Id);
        await Seed(exhibit, team, TestData.TeamCard(team.Id, card.Id, canPostArticles: true));
        var actor = await Actor().OnTeam(team).SeedAsync();

        var response = await Client(actor).PostAsJsonAsync("api/articles", NewArticle(collection.Id, card.Id, exhibit.Id), Ct);

        await AssertStatus(HttpStatusCode.Created, response);
        var created = await ReadAsync<Article>(response);
        await using var db = NewContext();
        Assert.True(await db.TeamArticles.AnyAsync(x => x.ArticleId == created.Id && x.TeamId == team.Id, Ct));
        Assert.True(await db.UserArticles.AnyAsync(x => x.ArticleId == created.Id && x.UserId == actor.Id, Ct));
    }

    [Fact]
    public async Task Create_is_forbidden_for_a_participant_whose_team_card_does_not_allow_posting()
    {
        var (collection, card, _) = await SeedArticle();
        var exhibit = TestData.Exhibit(collection.Id);
        var team = TestData.Team(exhibit.Id);
        await Seed(exhibit, team, TestData.TeamCard(team.Id, card.Id, canPostArticles: false));
        var actor = await Actor().OnTeam(team).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).PostAsJsonAsync("api/articles", NewArticle(collection.Id, card.Id, exhibit.Id), Ct));
    }

    [Fact]
    public async Task Create_of_a_posted_article_is_allowed_for_a_member_holding_EditExhibit()
    {
        var (collection, card, _) = await SeedArticle();
        var exhibit = TestData.Exhibit(collection.Id);
        await Seed(exhibit);
        var actor = await Actor().OnExhibit(exhibit, [ExhibitPermission.EditExhibit]).SeedAsync();

        await AssertStatus(HttpStatusCode.Created, await Client(actor).PostAsJsonAsync("api/articles", NewArticle(collection.Id, card.Id, exhibit.Id), Ct));
    }

    [Fact]
    public async Task Create_of_a_posted_article_persists_it_for_a_caller_holding_EditExhibits()
    {
        var (collection, card, _) = await SeedArticle();
        var exhibit = TestData.Exhibit(collection.Id);
        await Seed(exhibit);
        var actor = await Actor().WithSystemPermissions(SystemPermission.EditExhibits).SeedAsync();

        var response = await Client(actor).PostAsJsonAsync("api/articles", NewArticle(collection.Id, card.Id, exhibit.Id), Ct);

        await AssertStatus(HttpStatusCode.Created, response);
        var created = await ReadAsync<Article>(response);
        await using var db = NewContext();
        Assert.Equal(exhibit.Id, (await db.Articles.SingleAsync(x => x.Id == created.Id, Ct)).ExhibitId);
    }

    [Fact]
    public async Task Create_of_a_posted_article_is_forbidden_for_a_caller_holding_only_ViewExhibits()
    {
        var (collection, card, _) = await SeedArticle();
        var exhibit = TestData.Exhibit(collection.Id);
        await Seed(exhibit);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewExhibits).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).PostAsJsonAsync("api/articles",
            NewArticle(collection.Id, card.Id, exhibit.Id, name: "Refused"), Ct));

        await using var db = NewContext();
        Assert.False(await db.Articles.AnyAsync(x => x.Name == "Refused", Ct));
    }

    [Fact]
    public async Task Create_of_a_posted_article_is_forbidden_for_a_caller_holding_EditExhibit_only_on_another_exhibit()
    {
        var (collection, card, _) = await SeedArticle();
        var exhibit = TestData.Exhibit(collection.Id);
        await Seed(exhibit);
        var actor = await Actor().OnNewExhibit(collection.Id, ExhibitPermission.EditExhibit).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).PostAsJsonAsync("api/articles",
            NewArticle(collection.Id, card.Id, exhibit.Id, name: "Refused"), Ct));

        await using var db = NewContext();
        Assert.False(await db.Articles.AnyAsync(x => x.Name == "Refused", Ct));
    }

    /// <summary>A body with no collection is answered with a 500.</summary>
    [Fact]
    public async Task Create_answers_a_body_without_a_collection_with_a_server_error()
    {
        var problem = await AssertProblem(HttpStatusCode.InternalServerError,
            await RootClient.PostAsJsonAsync("api/articles", new { name = "Orphan", datePosted = TestData.DefaultDatePosted }, Ct));

        Assert.Equal("CollectionId is required", problem.Detail);
    }

    /// <summary>An exhibit's article goes in the exhibit's collection, whatever collection the body names.</summary>
    [Fact]
    public async Task Create_of_a_posted_article_takes_the_collection_from_the_exhibit()
    {
        var (collection, card, exhibit, _, _) = await SeedPostedArticle(canPost: true);
        var other = TestData.Collection("Other");
        await Seed(other);
        var actor = await Actor().OnExhibit(exhibit, [ExhibitPermission.EditExhibit]).SeedAsync();

        var created = await ReadAsync<Article>(await Client(actor).PostAsJsonAsync("api/articles",
            NewArticle(other.Id, card.Id, exhibit.Id, name: "Posted Elsewhere?"), Ct));

        await using var db = NewContext();
        Assert.Equal(collection.Id, (await db.Articles.SingleAsync(x => x.Id == created.Id, Ct)).CollectionId);
    }

    [Fact]
    public async Task Create_answers_a_card_of_another_collection_with_not_found_and_saves_nothing()
    {
        var (collection, _, _) = await SeedArticle();
        var other = TestData.Collection("Other");
        var foreignCard = TestData.Card(other.Id, "Foreign");
        await Seed(other, foreignCard);
        var actor = await Actor().OnCollection(collection, [CollectionPermission.EditCollection]).SeedAsync();

        await AssertProblem(HttpStatusCode.NotFound, await Client(actor).PostAsJsonAsync("api/articles",
            NewArticle(collection.Id, foreignCard.Id, name: "Misfiled"), Ct));

        await using var db = NewContext();
        Assert.False(await db.Articles.AnyAsync(x => x.Name == "Misfiled", Ct));
    }

    [Fact]
    public async Task Create_keeps_a_cardless_article_valid()
    {
        var (collection, _, _) = await SeedArticle();
        var actor = await Actor().OnCollection(collection, [CollectionPermission.EditCollection]).SeedAsync();

        await AssertStatus(HttpStatusCode.Created, await Client(actor).PostAsJsonAsync("api/articles",
            NewArticle(collection.Id, null, name: "Cardless"), Ct));
    }

    // ---- Update --------------------------------------------------------------------------------

    [Fact]
    public async Task Update_persists_the_change_for_a_member_holding_EditCollection()
    {
        var (collection, card, article) = await SeedArticle();
        var actor = await Actor().OnCollection(collection, [CollectionPermission.EditCollection]).SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).PutAsJsonAsync($"api/articles/{article.Id}",
            NewArticle(collection.Id, card.Id, id: article.Id, name: "Rewritten"), Ct));

        await using var db = NewContext();
        Assert.Equal("Rewritten", (await db.Articles.SingleAsync(x => x.Id == article.Id, Ct)).Name);
    }

    [Fact]
    public async Task Update_persists_the_change_for_a_caller_holding_EditCollections()
    {
        var (collection, card, article) = await SeedArticle();
        var actor = await Actor().WithSystemPermissions(SystemPermission.EditCollections).SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).PutAsJsonAsync($"api/articles/{article.Id}",
            NewArticle(collection.Id, card.Id, id: article.Id, name: "Rewritten"), Ct));

        await using var db = NewContext();
        Assert.Equal("Rewritten", (await db.Articles.SingleAsync(x => x.Id == article.Id, Ct)).Name);
    }

    [Fact]
    public async Task Update_is_forbidden_for_a_caller_holding_only_ViewCollections()
    {
        var (collection, card, article) = await SeedArticle();
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewCollections).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).PutAsJsonAsync($"api/articles/{article.Id}",
            NewArticle(collection.Id, card.Id, id: article.Id, name: "Rewritten"), Ct));

        await using var db = NewContext();
        Assert.Equal(article.Name, (await db.Articles.SingleAsync(x => x.Id == article.Id, Ct)).Name);
    }

    [Fact]
    public async Task Update_is_forbidden_for_a_caller_holding_only_ViewCollection()
    {
        var (collection, card, article) = await SeedArticle();
        var actor = await Actor().OnCollection(collection, [CollectionPermission.ViewCollection]).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).PutAsJsonAsync($"api/articles/{article.Id}",
            NewArticle(collection.Id, card.Id, id: article.Id, name: "Rewritten"), Ct));
    }

    /// <summary>The gate reads the stored article's collection, so an editor of another collection cannot take the article.</summary>
    [Fact]
    public async Task Update_is_forbidden_for_an_editor_of_the_collection_the_body_names_and_leaves_the_article()
    {
        var (collection, _, article) = await SeedArticle();
        var actor = await Actor().OnNewCollection(CollectionPermission.EditCollection).SeedAsync();
        var mine = Assert.Single(actor.NewCollectionIds);

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).PutAsJsonAsync($"api/articles/{article.Id}",
            NewArticle(mine, null, id: article.Id, name: "Taken"), Ct));

        await using var db = NewContext();
        var stored = await db.Articles.SingleAsync(x => x.Id == article.Id, Ct);
        Assert.Equal(("Test Article", collection.Id), (stored.Name, stored.CollectionId));
    }

    /// <summary>An edit body's collection and exhibit are ignored: the article stays where it is stored.</summary>
    [Fact]
    public async Task Update_ignores_the_collection_and_exhibit_in_the_body()
    {
        var (collection, card, article) = await SeedArticle();
        var other = TestData.Collection("Other");
        var exhibit = TestData.Exhibit(collection.Id);
        await Seed(other, exhibit);
        var actor = await Actor().OnCollection(collection, [CollectionPermission.EditCollection]).SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).PutAsJsonAsync($"api/articles/{article.Id}",
            NewArticle(other.Id, card.Id, exhibit.Id, article.Id, "Edited"), Ct));

        await using var db = NewContext();
        var stored = await db.Articles.SingleAsync(x => x.Id == article.Id, Ct);
        Assert.Equal(("Edited", collection.Id, (Guid?)null), (stored.Name, stored.CollectionId, stored.ExhibitId));
    }

    /// <summary>An article can move only to a card of its own collection.</summary>
    [Fact]
    public async Task Update_answers_a_card_of_another_collection_with_not_found_and_keeps_the_card()
    {
        var (collection, card, article) = await SeedArticle();
        var other = TestData.Collection("Other");
        var foreignCard = TestData.Card(other.Id, "Foreign");
        await Seed(other, foreignCard);
        var actor = await Actor().OnCollection(collection, [CollectionPermission.EditCollection]).SeedAsync();

        await AssertProblem(HttpStatusCode.NotFound, await Client(actor).PutAsJsonAsync($"api/articles/{article.Id}",
            NewArticle(collection.Id, foreignCard.Id, id: article.Id), Ct));

        await using var db = NewContext();
        Assert.Equal(card.Id, (await db.Articles.SingleAsync(x => x.Id == article.Id, Ct)).CardId);
    }

    [Fact]
    public async Task Update_of_an_unknown_article_is_not_found()
    {
        await AssertProblem(HttpStatusCode.NotFound, await RootClient.PutAsJsonAsync($"api/articles/{Guid.NewGuid()}", NewArticle(Guid.NewGuid(), null), Ct));
    }

    [Fact]
    public async Task Update_of_a_posted_article_is_allowed_for_a_member_holding_EditExhibit()
    {
        var (collection, card, exhibit, _, posted) = await SeedPostedArticle(canPost: false);
        var actor = await Actor().OnExhibit(exhibit, [ExhibitPermission.EditExhibit]).SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).PutAsJsonAsync($"api/articles/{posted.Id}",
            NewArticle(collection.Id, card.Id, exhibit.Id, posted.Id, "Rewritten"), Ct));

        await using var db = NewContext();
        Assert.Equal("Rewritten", (await db.Articles.SingleAsync(x => x.Id == posted.Id, Ct)).Name);
    }

    [Fact]
    public async Task Update_of_a_posted_article_is_allowed_for_a_caller_holding_EditExhibits()
    {
        var (collection, card, exhibit, _, posted) = await SeedPostedArticle(canPost: false);
        var actor = await Actor().WithSystemPermissions(SystemPermission.EditExhibits).SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).PutAsJsonAsync($"api/articles/{posted.Id}",
            NewArticle(collection.Id, card.Id, exhibit.Id, posted.Id, "Rewritten"), Ct));

        await using var db = NewContext();
        Assert.Equal("Rewritten", (await db.Articles.SingleAsync(x => x.Id == posted.Id, Ct)).Name);
    }

    [Fact]
    public async Task Update_of_a_posted_article_is_forbidden_for_a_caller_holding_only_ViewExhibits()
    {
        var (collection, card, exhibit, _, posted) = await SeedPostedArticle(canPost: false);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewExhibits).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).PutAsJsonAsync($"api/articles/{posted.Id}",
            NewArticle(collection.Id, card.Id, exhibit.Id, posted.Id, "Rewritten"), Ct));

        await using var db = NewContext();
        Assert.Equal("Posted", (await db.Articles.SingleAsync(x => x.Id == posted.Id, Ct)).Name);
    }

    [Fact]
    public async Task Update_of_a_posted_article_is_forbidden_for_a_caller_holding_EditExhibit_only_on_another_exhibit()
    {
        var (collection, card, exhibit, _, posted) = await SeedPostedArticle(canPost: false);
        var actor = await Actor().OnNewExhibit(collection.Id, ExhibitPermission.EditExhibit).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).PutAsJsonAsync($"api/articles/{posted.Id}",
            NewArticle(collection.Id, card.Id, exhibit.Id, posted.Id, "Rewritten"), Ct));

        await using var db = NewContext();
        Assert.Equal("Posted", (await db.Articles.SingleAsync(x => x.Id == posted.Id, Ct)).Name);
    }

    [Fact]
    public async Task Update_of_a_posted_article_is_allowed_for_a_participant_whose_team_card_allows_posting()
    {
        var (collection, card, exhibit, team, posted) = await SeedPostedArticle(canPost: true);
        var actor = await Actor().OnTeam(team).SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).PutAsJsonAsync($"api/articles/{posted.Id}",
            NewArticle(collection.Id, card.Id, exhibit.Id, posted.Id, "Rewritten"), Ct));

        await using var db = NewContext();
        Assert.Equal("Rewritten", (await db.Articles.SingleAsync(x => x.Id == posted.Id, Ct)).Name);
    }

    [Fact]
    public async Task Update_of_a_posted_article_is_forbidden_for_a_participant_whose_team_card_does_not_allow_posting()
    {
        var (collection, card, exhibit, team, posted) = await SeedPostedArticle(canPost: false);
        var actor = await Actor().OnTeam(team).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).PutAsJsonAsync($"api/articles/{posted.Id}",
            NewArticle(collection.Id, card.Id, exhibit.Id, posted.Id, "Rewritten"), Ct));

        await using var db = NewContext();
        Assert.Equal("Posted", (await db.Articles.SingleAsync(x => x.Id == posted.Id, Ct)).Name);
    }

    /// <summary>A participant who can post on another card of the exhibit cannot edit an article on a card their team cannot post on.</summary>
    [Fact]
    public async Task Update_of_a_posted_article_is_forbidden_for_a_participant_who_can_post_only_on_the_card_the_body_names()
    {
        var (collection, card, exhibit, team, posted) = await SeedPostedArticle(canPost: false);
        var open = TestData.Card(collection.Id, "Open");
        await Seed(open, TestData.TeamCard(team.Id, open.Id, canPostArticles: true));
        var actor = await Actor().OnTeam(team).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).PutAsJsonAsync($"api/articles/{posted.Id}",
            NewArticle(collection.Id, open.Id, exhibit.Id, posted.Id, "Rewritten"), Ct));

        await using var db = NewContext();
        var stored = await db.Articles.SingleAsync(x => x.Id == posted.Id, Ct);
        Assert.Equal(("Posted", (Guid?)card.Id), (stored.Name, stored.CardId));
    }

    /// <summary>A participant cannot move a posted article to a card their team cannot post on.</summary>
    [Fact]
    public async Task Update_of_a_posted_article_is_forbidden_for_a_participant_moving_it_to_a_card_the_team_cannot_post_on()
    {
        var (collection, card, exhibit, team, posted) = await SeedPostedArticle(canPost: true);
        var closed = TestData.Card(collection.Id, "Closed");
        await Seed(closed);
        var actor = await Actor().OnTeam(team).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).PutAsJsonAsync($"api/articles/{posted.Id}",
            NewArticle(collection.Id, closed.Id, exhibit.Id, posted.Id, "Moved"), Ct));

        await using var db = NewContext();
        Assert.Equal(card.Id, (await db.Articles.SingleAsync(x => x.Id == posted.Id, Ct)).CardId);
    }

    /// <summary>A participant's body naming another exhibit does not change which exhibit the gate reads.</summary>
    [Fact]
    public async Task Update_of_a_template_is_forbidden_for_a_participant_whose_body_names_their_exhibit()
    {
        var (collection, card, exhibit, team, _) = await SeedPostedArticle(canPost: true);
        var template = TestData.Article(collection.Id, card.Id, name: "Template");
        await Seed(template);
        var actor = await Actor().OnTeam(team).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).PutAsJsonAsync($"api/articles/{template.Id}",
            NewArticle(collection.Id, card.Id, exhibit.Id, template.Id, "Rewritten"), Ct));

        await using var db = NewContext();
        var stored = await db.Articles.SingleAsync(x => x.Id == template.Id, Ct);
        Assert.Equal(("Template", (Guid?)null), (stored.Name, stored.ExhibitId));
    }

    // ---- Delete --------------------------------------------------------------------------------

    [Fact]
    public async Task Delete_removes_the_article_for_a_member_holding_EditCollection()
    {
        var (collection, _, article) = await SeedArticle();
        var actor = await Actor().OnCollection(collection, [CollectionPermission.EditCollection]).SeedAsync();

        await AssertStatus(HttpStatusCode.NoContent, await Client(actor).DeleteAsync($"api/articles/{article.Id}", Ct));

        await using var db = NewContext();
        Assert.False(await db.Articles.AnyAsync(x => x.Id == article.Id, Ct));
    }

    [Fact]
    public async Task Delete_removes_the_article_for_a_caller_holding_EditCollections()
    {
        var (_, _, article) = await SeedArticle();
        var actor = await Actor().WithSystemPermissions(SystemPermission.EditCollections).SeedAsync();

        await AssertStatus(HttpStatusCode.NoContent, await Client(actor).DeleteAsync($"api/articles/{article.Id}", Ct));

        await using var db = NewContext();
        Assert.False(await db.Articles.AnyAsync(x => x.Id == article.Id, Ct));
    }

    [Fact]
    public async Task Delete_is_forbidden_for_a_caller_holding_only_ViewCollections()
    {
        var (_, _, article) = await SeedArticle();
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewCollections).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).DeleteAsync($"api/articles/{article.Id}", Ct));

        await using var db = NewContext();
        Assert.True(await db.Articles.AnyAsync(x => x.Id == article.Id, Ct));
    }

    [Fact]
    public async Task Delete_is_forbidden_for_a_caller_holding_EditCollection_only_on_another_collection()
    {
        var (_, _, article) = await SeedArticle();
        var actor = await Actor().OnNewCollection(CollectionPermission.EditCollection).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).DeleteAsync($"api/articles/{article.Id}", Ct));

        await using var db = NewContext();
        Assert.True(await db.Articles.AnyAsync(x => x.Id == article.Id, Ct));
    }

    /// <summary>Deleting a posted article removes its team and user deliveries first.</summary>
    [Fact]
    public async Task Delete_of_a_posted_article_removes_its_deliveries()
    {
        var (collection, card, _) = await SeedArticle();
        var exhibit = TestData.Exhibit(collection.Id);
        var team = TestData.Team(exhibit.Id);
        var posted = TestData.Article(collection.Id, card.Id, exhibit.Id, "Posted");
        var user = TestData.User();
        await Seed(exhibit, team, posted, user, TestData.TeamArticle(exhibit.Id, team.Id, posted.Id),
            TestData.UserArticle(exhibit.Id, user.Id, posted.Id));

        await AssertStatus(HttpStatusCode.NoContent, await RootClient.DeleteAsync($"api/articles/{posted.Id}", Ct));

        await using var db = NewContext();
        Assert.False(await db.UserArticles.AnyAsync(x => x.ArticleId == posted.Id, Ct));
        Assert.False(await db.TeamArticles.AnyAsync(x => x.ArticleId == posted.Id, Ct));
    }

    [Fact]
    public async Task Delete_of_a_posted_article_is_allowed_for_a_member_holding_EditExhibit()
    {
        var (_, _, exhibit, _, posted) = await SeedPostedArticle(canPost: false);
        var actor = await Actor().OnExhibit(exhibit, [ExhibitPermission.EditExhibit]).SeedAsync();

        await AssertStatus(HttpStatusCode.NoContent, await Client(actor).DeleteAsync($"api/articles/{posted.Id}", Ct));

        await using var db = NewContext();
        Assert.False(await db.Articles.AnyAsync(x => x.Id == posted.Id, Ct));
    }

    [Fact]
    public async Task Delete_of_a_posted_article_is_allowed_for_a_caller_holding_EditExhibits()
    {
        var (_, _, _, _, posted) = await SeedPostedArticle(canPost: false);
        var actor = await Actor().WithSystemPermissions(SystemPermission.EditExhibits).SeedAsync();

        await AssertStatus(HttpStatusCode.NoContent, await Client(actor).DeleteAsync($"api/articles/{posted.Id}", Ct));

        await using var db = NewContext();
        Assert.False(await db.Articles.AnyAsync(x => x.Id == posted.Id, Ct));
    }

    [Fact]
    public async Task Delete_of_a_posted_article_is_forbidden_for_a_caller_holding_only_ViewExhibits()
    {
        var (_, _, _, _, posted) = await SeedPostedArticle(canPost: false);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewExhibits).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).DeleteAsync($"api/articles/{posted.Id}", Ct));

        await using var db = NewContext();
        Assert.True(await db.Articles.AnyAsync(x => x.Id == posted.Id, Ct));
    }

    [Fact]
    public async Task Delete_of_a_posted_article_is_forbidden_for_a_caller_holding_EditExhibit_only_on_another_exhibit()
    {
        var (collection, _, _, _, posted) = await SeedPostedArticle(canPost: false);
        var actor = await Actor().OnNewExhibit(collection.Id, ExhibitPermission.EditExhibit).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).DeleteAsync($"api/articles/{posted.Id}", Ct));

        await using var db = NewContext();
        Assert.True(await db.Articles.AnyAsync(x => x.Id == posted.Id, Ct));
    }

    [Fact]
    public async Task Delete_of_a_posted_article_is_allowed_for_a_participant_whose_team_card_allows_posting()
    {
        var (_, _, _, team, posted) = await SeedPostedArticle(canPost: true);
        var actor = await Actor().OnTeam(team).SeedAsync();

        await AssertStatus(HttpStatusCode.NoContent, await Client(actor).DeleteAsync($"api/articles/{posted.Id}", Ct));
    }

    [Fact]
    public async Task Delete_of_a_posted_article_is_forbidden_for_a_participant_whose_team_card_does_not_allow_posting()
    {
        var (_, _, _, team, posted) = await SeedPostedArticle(canPost: false);
        var actor = await Actor().OnTeam(team).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).DeleteAsync($"api/articles/{posted.Id}", Ct));

        await using var db = NewContext();
        Assert.True(await db.Articles.AnyAsync(x => x.Id == posted.Id, Ct));
    }

    // ---- Status names --------------------------------------------------------------------------

    [Theory]
    [InlineData("Gray", 0)]
    [InlineData("Red", 10)]
    [InlineData("Orange", 20)]
    [InlineData("Yellow", 40)]
    [InlineData("Green", 50)]
    public async Task Create_stores_each_color_name_and_answers_with_it(string status, int stored)
    {
        var (collection, card, _) = await SeedArticle();

        var response = await RootClient.PostAsJsonAsync("api/articles", NewArticle(collection.Id, card.Id, status: status), Ct);

        await AssertStatus(HttpStatusCode.Created, response);
        var (id, answered) = await Answered(response);
        Assert.Equal((JsonValueKind.String, status, stored), (answered.ValueKind, answered.ToString(), await StoredStatus(id)));
    }

    /// <summary>A client written before the statuses were named for colors still sends the old names.</summary>
    [Theory]
    [InlineData("Unused", "Gray", 0)]
    [InlineData("Closed", "Red", 10)]
    [InlineData("Critical", "Orange", 20)]
    [InlineData("Affected", "Yellow", 40)]
    [InlineData("Open", "Green", 50)]
    public async Task Create_stores_each_legacy_status_name_as_its_color(string sent, string status, int stored)
    {
        var (collection, card, _) = await SeedArticle();

        var response = await RootClient.PostAsJsonAsync("api/articles", NewArticle(collection.Id, card.Id, status: sent), Ct);

        await AssertStatus(HttpStatusCode.Created, response);
        var (id, answered) = await Answered(response);
        Assert.Equal((status, stored), (answered.ToString(), await StoredStatus(id)));
    }

    [Theory]
    [InlineData(20, "Orange", 20)]
    [InlineData("20", "Orange", 20)]
    [InlineData(0, "Gray", 0)]
    [InlineData("50", "Green", 50)]
    public async Task Create_stores_a_status_number_or_quoted_number_and_answers_with_its_color_name(object sent, string status, int stored)
    {
        var (collection, card, _) = await SeedArticle();

        var response = await RootClient.PostAsJsonAsync("api/articles", NewArticle(collection.Id, card.Id, status: sent), Ct);

        await AssertStatus(HttpStatusCode.Created, response);
        var (id, answered) = await Answered(response);
        Assert.Equal((status, stored), (answered.ToString(), await StoredStatus(id)));
    }

    [Theory]
    [InlineData("Purple", "'Purple' is not a valid ItemStatus value.")]
    [InlineData("Resolved", "'Resolved' is not a valid ItemStatus value.")]
    public async Task Create_answers_an_unknown_status_name_with_a_bad_request_and_stores_nothing(string sent, string message)
    {
        var (collection, card, _) = await SeedArticle();

        var response = await RootClient.PostAsJsonAsync("api/articles", NewArticle(collection.Id, card.Id, name: "Refused", status: sent), Ct);

        await AssertProblem(HttpStatusCode.BadRequest, response);
        Assert.Equal(message, await StatusError(response));
        Assert.False(await ReadBack(db => db.Articles.AnyAsync(x => x.Name == "Refused", Ct)));
    }

    /// <summary>An out-of-range status at the edge: a number no member names is refused before anything is stored.</summary>
    [Theory]
    [InlineData(30, "30 is not a valid ItemStatus value.")]
    [InlineData(999, "999 is not a valid ItemStatus value.")]
    [InlineData(-10, "-10 is not a valid ItemStatus value.")]
    [InlineData("30", "'30' is not a valid ItemStatus value.")]
    public async Task Create_answers_a_status_number_outside_the_defined_ones_with_a_bad_request_and_stores_nothing(object sent, string message)
    {
        var (collection, card, _) = await SeedArticle();

        var response = await RootClient.PostAsJsonAsync("api/articles", NewArticle(collection.Id, card.Id, name: "Refused", status: sent), Ct);

        await AssertProblem(HttpStatusCode.BadRequest, response);
        Assert.Equal(message, await StatusError(response));
        Assert.False(await ReadBack(db => db.Articles.AnyAsync(x => x.Name == "Refused", Ct)));
    }

    /// <summary>A comma-separated pair of status names is stored as the two values combined, and answered as that number in quotes.</summary>
    [Fact]
    public async Task Create_stores_a_comma_separated_pair_of_status_names_as_an_undefined_status()
    {
        var (collection, card, _) = await SeedArticle();

        var response = await RootClient.PostAsJsonAsync("api/articles", NewArticle(collection.Id, card.Id, status: "Red, Orange"), Ct);

        await AssertStatus(HttpStatusCode.Created, response);
        var (id, answered) = await Answered(response);
        Assert.Equal((JsonValueKind.String, "30", 30), (answered.ValueKind, answered.ToString(), await StoredStatus(id)));
    }

    [Fact]
    public async Task Update_stores_a_legacy_status_name_as_its_color()
    {
        var (collection, card, article) = await SeedArticle();

        var response = await RootClient.PutAsJsonAsync($"api/articles/{article.Id}",
            NewArticle(collection.Id, card.Id, id: article.Id, status: "Closed"), Ct);

        await AssertStatus(HttpStatusCode.OK, response);
        var (_, answered) = await Answered(response);
        Assert.Equal(("Red", 10), (answered.ToString(), await StoredStatus(article.Id)));
    }

    [Theory]
    [InlineData("Purple", "'Purple' is not a valid ItemStatus value.")]
    [InlineData(30, "30 is not a valid ItemStatus value.")]
    public async Task Update_answers_a_status_no_member_names_with_a_bad_request_and_keeps_the_stored_status(object sent, string message)
    {
        var (collection, card, article) = await SeedArticle();

        var response = await RootClient.PutAsJsonAsync($"api/articles/{article.Id}",
            NewArticle(collection.Id, card.Id, id: article.Id, name: "Rewritten", status: sent), Ct);

        await AssertProblem(HttpStatusCode.BadRequest, response);
        Assert.Equal(message, await StatusError(response));
        var stored = await ReadBack(db => db.Articles.SingleAsync(x => x.Id == article.Id, Ct));
        Assert.Equal(("Test Article", ItemStatus.Green), (stored.Name, stored.Status));
    }

    [Theory]
    [InlineData(ItemStatus.Gray, "Gray")]
    [InlineData(ItemStatus.Red, "Red")]
    [InlineData(ItemStatus.Orange, "Orange")]
    [InlineData(ItemStatus.Yellow, "Yellow")]
    [InlineData(ItemStatus.Green, "Green")]
    public async Task Get_answers_each_stored_status_by_its_color_name(ItemStatus stored, string status)
    {
        var collection = TestData.Collection();
        var article = TestData.Article(collection.Id, status: stored);
        await Seed(collection, article);

        var response = await RootClient.GetAsync($"api/articles/{article.Id}", Ct);

        await AssertStatus(HttpStatusCode.OK, response);
        var (_, answered) = await Answered(response);
        Assert.Equal((JsonValueKind.String, status), (answered.ValueKind, answered.ToString()));
    }

    /// <summary>A stored status no member names is answered as its number in quotes.</summary>
    [Fact]
    public async Task Get_answers_a_stored_status_outside_the_defined_ones_with_its_number_as_a_string()
    {
        var collection = TestData.Collection();
        var article = TestData.Article(collection.Id, status: (ItemStatus)30);
        await Seed(collection, article);

        var response = await RootClient.GetAsync($"api/articles/{article.Id}", Ct);

        await AssertStatus(HttpStatusCode.OK, response);
        var (_, answered) = await Answered(response);
        Assert.Equal((JsonValueKind.String, "30"), (answered.ValueKind, answered.ToString()));
    }

    /// <summary>The wall colors a card by its articles' statuses, which the card's article list answers by color name.</summary>
    [Fact]
    public async Task GetByCard_answers_each_article_s_status_by_its_color_name()
    {
        var collection = TestData.Collection();
        var card = TestData.Card(collection.Id);
        await Seed(collection, card,
            TestData.Article(collection.Id, card.Id, name: "Worst", status: ItemStatus.Red),
            TestData.Article(collection.Id, card.Id, name: "Middling", status: ItemStatus.Yellow));

        var response = await RootClient.GetAsync($"api/cards/{card.Id}/articles", Ct);

        await AssertStatus(HttpStatusCode.OK, response);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct));
        var statuses = body.RootElement.EnumerateArray()
            .ToDictionary(x => x.GetProperty("name").GetString(), x => x.GetProperty("status").GetString());
        Assert.Equal(new Dictionary<string, string> { ["Worst"] = "Red", ["Middling"] = "Yellow" }, statuses);
    }

    /// <summary>The id and the status member of an article response, as the wire carries them.</summary>
    private static async Task<(Guid Id, JsonElement Status)> Answered(HttpResponseMessage response)
    {
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct));

        return (body.RootElement.GetProperty("id").GetGuid(), body.RootElement.GetProperty("status").Clone());
    }

    /// <summary>The one model error a 400 names for the body's status member.</summary>
    private static async Task<string> StatusError(HttpResponseMessage response)
    {
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct));

        return Assert.Single(body.RootElement.GetProperty("errors").GetProperty("$.status").EnumerateArray()).GetString();
    }

    private Task<int> StoredStatus(Guid id) =>
        ReadBack(db => db.Articles.Where(x => x.Id == id).Select(x => (int)x.Status).SingleAsync(Ct));

    private async Task<(CollectionEntity Collection, CardEntity Card, ArticleEntity Article)> SeedArticle()
    {
        var collection = TestData.Collection();
        var card = TestData.Card(collection.Id);
        var article = TestData.Article(collection.Id, card.Id);
        await Seed(collection, card, article);

        return (collection, card, article);
    }

    /// <summary>An exhibit with one team whose card on the article's card allows posting or not, and an article posted on that card.</summary>
    private async Task<(CollectionEntity Collection, CardEntity Card, ExhibitEntity Exhibit, TeamEntity Team, ArticleEntity Posted)> SeedPostedArticle(bool canPost)
    {
        var (collection, card, _) = await SeedArticle();
        var exhibit = TestData.Exhibit(collection.Id);
        var team = TestData.Team(exhibit.Id);
        var posted = TestData.Article(collection.Id, card.Id, exhibit.Id, "Posted");
        await Seed(exhibit, team, TestData.TeamCard(team.Id, card.Id, canPostArticles: canPost), posted);

        return (collection, card, exhibit, team, posted);
    }

    private static object NewArticle(Guid collectionId, Guid? cardId, Guid? exhibitId = null, Guid? id = null, string name = "New Article", object status = null) =>
        new
        {
            id = id ?? Guid.Empty,
            name,
            summary = "Body of the article",
            collectionId,
            cardId,
            exhibitId,
            sourceType = "News",
            status = status ?? "Green",
            datePosted = TestData.DefaultDatePosted
        };
}
