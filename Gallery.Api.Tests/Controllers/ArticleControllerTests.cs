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
    public async Task GetByCollection_is_forbidden_for_a_caller_holding_only_EditCollection()
    {
        var (collection, _, _) = await SeedArticle();
        var actor = await Actor().OnCollection(collection, [CollectionPermission.EditCollection]).SeedAsync();

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

    /// <summary>A body with no collection is answered with a 500.</summary>
    [Fact]
    public async Task Create_answers_a_body_without_a_collection_with_a_server_error()
    {
        var problem = await AssertProblem(HttpStatusCode.InternalServerError,
            await RootClient.PostAsJsonAsync("api/articles", new { name = "Orphan", datePosted = TestData.DefaultDatePosted }, Ct));

        Assert.Equal("CollectionId is required", problem.Detail);
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
    public async Task Update_is_forbidden_for_a_caller_holding_only_ViewCollection()
    {
        var (collection, card, article) = await SeedArticle();
        var actor = await Actor().OnCollection(collection, [CollectionPermission.ViewCollection]).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).PutAsJsonAsync($"api/articles/{article.Id}",
            NewArticle(collection.Id, card.Id, id: article.Id, name: "Rewritten"), Ct));
    }

    /// <summary>The gate reads the collection the body names, so the article moves into the caller's collection.</summary>
    [Fact]
    public async Task Update_moves_another_collection_s_article_when_the_body_names_a_collection_the_caller_edits()
    {
        var (_, _, article) = await SeedArticle();
        var actor = await Actor().OnNewCollection(CollectionPermission.EditCollection).SeedAsync();
        var mine = Assert.Single(actor.NewCollectionIds);

        await AssertStatus(HttpStatusCode.OK, await Client(actor).PutAsJsonAsync($"api/articles/{article.Id}",
            NewArticle(mine, null, id: article.Id, name: "Taken"), Ct));

        await using var db = NewContext();
        var stored = await db.Articles.SingleAsync(x => x.Id == article.Id, Ct);
        Assert.Equal(("Taken", mine), (stored.Name, stored.CollectionId));
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

    private static object NewArticle(Guid collectionId, Guid? cardId, Guid? exhibitId = null, Guid? id = null, string name = "New Article") =>
        new
        {
            id = id ?? Guid.Empty,
            name,
            summary = "Body of the article",
            collectionId,
            cardId,
            exhibitId,
            sourceType = "News",
            status = "Open",
            datePosted = TestData.DefaultDatePosted
        };
}
