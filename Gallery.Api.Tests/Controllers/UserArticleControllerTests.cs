// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Net;
using System.Net.Http.Json;
using Gallery.Api.Data.Enumerations;
using Gallery.Api.Data.Models;
using Gallery.Api.Services;
using Gallery.Api.Tests.Support;
using Gallery.Api.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Gallery.Api.Tests.Controllers;

/// <summary>
/// <c>UserArticleController</c> over HTTP: the articles delivered to one user in one exhibit, which is what
/// a participant's archive and CITE's unread count read.
/// </summary>
public class UserArticleControllerTests(DatabaseFixture fixture, GalleryAppFactory factory) : ApiTestBase(fixture, factory)
{
    [Fact]
    public async Task GetByExhibit_returns_the_delivered_articles_to_a_member_holding_ViewExhibit()
    {
        var (exhibit, _, userArticle) = await SeedDelivery();
        var actor = await Actor().OnExhibit(exhibit, [ExhibitPermission.ViewExhibit]).SeedAsync();

        var userArticles = await ReadAsync<List<UserArticle>>(await Client(actor).GetAsync($"api/exhibits/{exhibit.Id}/userarticles", Ct));

        Assert.Equal(userArticle.Id, Assert.Single(userArticles).Id);
    }

    [Fact]
    public async Task GetByExhibit_is_forbidden_for_a_caller_holding_ViewExhibit_only_on_another_exhibit()
    {
        var (exhibit, _, _) = await SeedDelivery();
        var actor = await Actor().OnNewExhibit(exhibit.CollectionId, ExhibitPermission.ViewExhibit).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/exhibits/{exhibit.Id}/userarticles", Ct));
    }

    /// <summary>A request with no identity is answered with a 401; the anonymous client is the case under test.</summary>
    [Fact]
    public async Task GetByExhibit_without_an_identity_is_unauthorized()
    {
        await AssertStatus(HttpStatusCode.Unauthorized, await Client().GetAsync($"api/exhibits/{Guid.NewGuid()}/userarticles", Ct));
    }

    /// <summary>A participant reading their own team's archive gets the team's due articles delivered to them.</summary>
    [Fact]
    public async Task GetByExhibitTeam_delivers_and_returns_the_participant_s_due_articles()
    {
        var (exhibit, team, _) = await SeedDelivery();
        var actor = await Actor().OnTeam(team).SeedAsync();

        var userArticles = await ReadAsync<List<UserArticle>>(
            await Client(actor).GetAsync($"api/exhibits/{exhibit.Id}/teams/{team.Id}/userarticles", Ct));

        Assert.Equal(actor.Id, Assert.Single(userArticles).UserId);
    }

    [Fact]
    public async Task GetByExhibitTeam_is_forbidden_for_a_caller_holding_ViewTeam_only_on_another_team()
    {
        var (exhibit, team, _) = await SeedDelivery();
        var actor = await Actor().OnNewTeam(exhibit.Id).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/exhibits/{exhibit.Id}/teams/{team.Id}/userarticles", Ct));
    }

    [Fact]
    public async Task GetUnreadCount_counts_the_user_s_unread_due_articles_for_a_member_holding_ViewExhibit()
    {
        var (exhibit, _, userArticle) = await SeedDelivery();
        var actor = await Actor().OnExhibit(exhibit, [ExhibitPermission.ViewExhibit]).SeedAsync();

        var unread = await ReadAsync<UnreadArticles>(
            await Client(actor).GetAsync($"api/exhibits/{exhibit.Id}/users/{userArticle.UserId}/articles/unread", Ct));

        Assert.Equal("1", unread.Count);
    }

    [Fact]
    public async Task GetUnreadCount_is_forbidden_for_a_caller_holding_only_ViewCollections()
    {
        var (exhibit, _, userArticle) = await SeedDelivery();
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewCollections).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden,
            await Client(actor).GetAsync($"api/exhibits/{exhibit.Id}/users/{userArticle.UserId}/articles/unread", Ct));
    }

    [Fact]
    public async Task Create_persists_the_user_article_for_a_member_holding_EditExhibit()
    {
        var (exhibit, _, userArticle) = await SeedDelivery();
        var article = TestData.Article(exhibit.CollectionId, name: "Handed Out");
        await Seed(article);
        var actor = await Actor().OnExhibit(exhibit, [ExhibitPermission.EditExhibit]).SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).PostAsJsonAsync("api/userarticles",
            new { exhibitId = exhibit.Id, userId = userArticle.UserId, articleId = article.Id, actualDatePosted = TestData.DefaultDatePosted }, Ct));

        await using var db = NewContext();
        Assert.True(await db.UserArticles.AnyAsync(x => x.ArticleId == article.Id && x.UserId == userArticle.UserId, Ct));
    }

    [Fact]
    public async Task Create_is_forbidden_for_a_caller_holding_only_ViewExhibit()
    {
        var (exhibit, _, userArticle) = await SeedDelivery();
        var article = TestData.Article(exhibit.CollectionId, name: "Withheld");
        await Seed(article);
        var actor = await Actor().OnExhibit(exhibit, [ExhibitPermission.ViewExhibit]).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).PostAsJsonAsync("api/userarticles",
            new { exhibitId = exhibit.Id, userId = userArticle.UserId, articleId = article.Id }, Ct));
    }

    /// <summary>Sharing an article delivers it to the users of the teams named.</summary>
    [Fact]
    public async Task Share_delivers_the_article_to_the_named_team_s_users_for_its_owner_holding_EditExhibit()
    {
        var (exhibit, _, _) = await SeedDelivery();
        var article = TestData.Article(exhibit.CollectionId, name: "Shared");
        var toTeam = TestData.Team(exhibit.Id, "Recipients");
        var recipient = TestData.User(name: "Recipient");
        await Seed(article, toTeam, recipient, TestData.TeamUser(toTeam.Id, recipient.Id));
        var actor = await Actor().OnExhibit(exhibit, [ExhibitPermission.EditExhibit]).SeedAsync();
        var owned = TestData.UserArticle(exhibit.Id, actor.Id, article.Id);
        await Seed(owned);

        await AssertStatus(HttpStatusCode.OK, await Client(actor).PutAsJsonAsync($"api/userarticles/{owned.Id}/share",
            new { exhibitId = exhibit.Id, toTeamIdList = new[] { toTeam.Id }, subject = "s", message = "m" }, Ct));

        await using var db = NewContext();
        Assert.True(await db.UserArticles.AnyAsync(x => x.UserId == recipient.Id && x.ArticleId == article.Id, Ct));
    }

    /// <summary>A participant sharing an article delivered to them is refused; being on a team grants no EditExhibit.</summary>
    [Fact]
    public async Task Share_is_forbidden_for_the_participant_who_owns_the_user_article()
    {
        var (exhibit, team, userArticle) = await SeedDelivery();
        var participant = await Actor().OnTeam(team).SeedAsync();
        var owned = TestData.UserArticle(exhibit.Id, participant.Id, userArticle.ArticleId);
        await Seed(owned);

        await AssertProblem(HttpStatusCode.Forbidden, await Client(participant).PutAsJsonAsync($"api/userarticles/{owned.Id}/share",
            new { exhibitId = exhibit.Id, toTeamIdList = new[] { team.Id } }, Ct));
    }

    [Fact]
    public async Task Share_is_forbidden_for_an_owner_holding_EditExhibit_only_on_another_exhibit()
    {
        var (exhibit, team, userArticle) = await SeedDelivery();
        var actor = await Actor().OnNewExhibit(exhibit.CollectionId, ExhibitPermission.EditExhibit).SeedAsync();
        var owned = TestData.UserArticle(exhibit.Id, actor.Id, userArticle.ArticleId);
        await Seed(owned);

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).PutAsJsonAsync($"api/userarticles/{owned.Id}/share",
            new { exhibitId = exhibit.Id, toTeamIdList = new[] { team.Id } }, Ct));
    }

    [Fact]
    public async Task Share_is_forbidden_for_an_exhibit_editor_who_does_not_own_the_user_article()
    {
        var (exhibit, team, userArticle) = await SeedDelivery();
        var actor = await Actor().OnExhibit(exhibit, [ExhibitPermission.EditExhibit]).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).PutAsJsonAsync($"api/userarticles/{userArticle.Id}/share",
            new { exhibitId = exhibit.Id, toTeamIdList = new[] { team.Id } }, Ct));
    }

    [Fact]
    public async Task SetIsRead_marks_the_owner_s_user_article_read()
    {
        var (exhibit, team, userArticle) = await SeedDelivery();
        var owner = await Actor().OnTeam(team).SeedAsync();
        var owned = TestData.UserArticle(exhibit.Id, owner.Id, userArticle.ArticleId);
        await Seed(owned);

        await AssertStatus(HttpStatusCode.OK, await Client(owner).PutAsJsonAsync($"api/userarticles/{owned.Id}/isread", true, Ct));

        await using var db = NewContext();
        Assert.True((await db.UserArticles.SingleAsync(x => x.Id == owned.Id, Ct)).IsRead);
    }

    [Fact]
    public async Task SetIsRead_is_forbidden_for_a_teammate_of_the_owner()
    {
        var (_, team, userArticle) = await SeedDelivery();
        var teammate = await Actor().OnTeam(team).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(teammate).PutAsJsonAsync($"api/userarticles/{userArticle.Id}/isread", true, Ct));

        await using var db = NewContext();
        Assert.False((await db.UserArticles.SingleAsync(x => x.Id == userArticle.Id, Ct)).IsRead);
    }

    [Fact]
    public async Task Delete_removes_the_user_article_for_a_caller_holding_EditExhibits()
    {
        var (_, _, userArticle) = await SeedDelivery();
        var actor = await Actor().WithSystemPermissions(SystemPermission.EditExhibits).SeedAsync();

        await AssertStatus(HttpStatusCode.NoContent, await Client(actor).DeleteAsync($"api/userarticles/{userArticle.Id}", Ct));

        await using var db = NewContext();
        Assert.False(await db.UserArticles.AnyAsync(x => x.Id == userArticle.Id, Ct));
    }

    [Fact]
    public async Task Delete_is_forbidden_for_a_caller_holding_EditExhibit_only_on_another_exhibit()
    {
        var (exhibit, _, userArticle) = await SeedDelivery();
        var actor = await Actor().OnNewExhibit(exhibit.CollectionId, ExhibitPermission.EditExhibit).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).DeleteAsync($"api/userarticles/{userArticle.Id}", Ct));
    }

    /// <summary>An exhibit at move 0 inject 0 with a team, a team article that is due, and its delivery to one user.</summary>
    private async Task<(ExhibitEntity Exhibit, TeamEntity Team, UserArticleEntity UserArticle)> SeedDelivery()
    {
        var collection = TestData.Collection();
        var exhibit = TestData.Exhibit(collection.Id);
        var team = TestData.Team(exhibit.Id);
        var article = TestData.Article(collection.Id);
        var user = TestData.User(name: "Reader");
        var userArticle = TestData.UserArticle(exhibit.Id, user.Id, article.Id);
        await Seed(collection, exhibit, team, article, user, TestData.TeamUser(team.Id, user.Id),
            TestData.TeamArticle(exhibit.Id, team.Id, article.Id), userArticle);

        return (exhibit, team, userArticle);
    }
}
