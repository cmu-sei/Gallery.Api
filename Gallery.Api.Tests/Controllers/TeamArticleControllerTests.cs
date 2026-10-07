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
/// <c>TeamArticleController</c> over HTTP: which articles are delivered to a team. Writes are gated on
/// ManageExhibits or ManageExhibit.
/// </summary>
public class TeamArticleControllerTests(DatabaseFixture fixture, GalleryAppFactory factory) : ApiTestBase(fixture, factory)
{
    [Fact]
    public async Task GetByExhibit_returns_the_team_articles_to_a_member_holding_ViewExhibit()
    {
        var (exhibit, _, teamArticle) = await SeedTeamArticle();
        var actor = await Actor().OnExhibit(exhibit, [ExhibitPermission.ViewExhibit]).SeedAsync();

        var teamArticles = await ReadAsync<List<TeamArticle>>(await Client(actor).GetAsync($"api/exhibits/{exhibit.Id}/teamarticles", Ct));

        Assert.Equal(teamArticle.Id, Assert.Single(teamArticles).Id);
    }

    [Fact]
    public async Task GetByExhibit_is_forbidden_for_a_caller_holding_ViewExhibit_only_on_another_exhibit()
    {
        var (exhibit, _, _) = await SeedTeamArticle();
        var actor = await Actor().OnNewExhibit(exhibit.CollectionId, ExhibitPermission.ViewExhibit).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/exhibits/{exhibit.Id}/teamarticles", Ct));
    }

    /// <summary>A request with no identity is answered with a 401; the anonymous client is the case under test.</summary>
    [Fact]
    public async Task GetByExhibit_without_an_identity_is_unauthorized()
    {
        await AssertStatus(HttpStatusCode.Unauthorized, await Client().GetAsync($"api/exhibits/{Guid.NewGuid()}/teamarticles", Ct));
    }

    [Fact]
    public async Task GetByTeam_returns_the_team_articles_to_a_participant_of_the_team()
    {
        var (_, team, teamArticle) = await SeedTeamArticle();
        var actor = await Actor().OnTeam(team).SeedAsync();

        var teamArticles = await ReadAsync<List<TeamArticle>>(await Client(actor).GetAsync($"api/teams/{team.Id}/teamarticles", Ct));

        Assert.Equal(teamArticle.Id, Assert.Single(teamArticles).Id);
    }

    [Fact]
    public async Task GetByTeam_is_forbidden_for_a_caller_holding_ViewTeam_only_on_another_team()
    {
        var (exhibit, team, _) = await SeedTeamArticle();
        var actor = await Actor().OnNewTeam(exhibit.Id).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/teams/{team.Id}/teamarticles", Ct));
    }

    [Fact]
    public async Task Get_returns_the_team_article_to_a_participant_of_the_team()
    {
        var (_, team, teamArticle) = await SeedTeamArticle();
        var actor = await Actor().OnTeam(team).SeedAsync();

        var got = await ReadAsync<TeamArticle>(await Client(actor).GetAsync($"api/teamarticles/{teamArticle.Id}", Ct));

        Assert.Equal(teamArticle.ArticleId, got.ArticleId);
    }

    [Fact]
    public async Task Get_is_forbidden_for_a_caller_holding_ViewTeam_only_on_another_team()
    {
        var (exhibit, _, teamArticle) = await SeedTeamArticle();
        var actor = await Actor().OnNewTeam(exhibit.Id).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/teamarticles/{teamArticle.Id}", Ct));
    }

    [Fact]
    public async Task Get_reports_an_unknown_team_article_as_not_found()
    {
        await AssertProblem(HttpStatusCode.NotFound, await RootClient.GetAsync($"api/teamarticles/{Guid.NewGuid()}", Ct));
    }

    /// <summary>Delivering an article that is already due creates a user article for each user on the team.</summary>
    [Fact]
    public async Task Create_delivers_the_article_to_the_team_s_users_for_a_member_holding_ManageExhibit()
    {
        var (exhibit, team, _) = await SeedTeamArticle();
        var article = TestData.Article(exhibit.CollectionId, name: "Delivered");
        var user = TestData.User();
        await Seed(article, user, TestData.TeamUser(team.Id, user.Id));
        var actor = await Actor().OnExhibit(exhibit, [ExhibitPermission.ManageExhibit]).SeedAsync();

        await AssertStatus(HttpStatusCode.Created, await Client(actor).PostAsJsonAsync("api/teamarticles",
            new { exhibitId = exhibit.Id, teamId = team.Id, articleId = article.Id }, Ct));

        await using var db = NewContext();
        Assert.True(await db.UserArticles.AnyAsync(x => x.UserId == user.Id && x.ArticleId == article.Id && x.ExhibitId == exhibit.Id, Ct));
    }

    [Fact]
    public async Task Create_is_forbidden_for_a_caller_holding_only_EditExhibit()
    {
        var (exhibit, team, _) = await SeedTeamArticle();
        var article = TestData.Article(exhibit.CollectionId, name: "Withheld");
        await Seed(article);
        var actor = await Actor().OnExhibit(exhibit, [ExhibitPermission.EditExhibit]).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).PostAsJsonAsync("api/teamarticles",
            new { exhibitId = exhibit.Id, teamId = team.Id, articleId = article.Id }, Ct));

        await using var db = NewContext();
        Assert.False(await db.TeamArticles.AnyAsync(x => x.ArticleId == article.Id, Ct));
    }

    [Fact]
    public async Task Update_persists_the_change_for_a_member_holding_ManageExhibit()
    {
        var (exhibit, team, teamArticle) = await SeedTeamArticle();
        var other = TestData.Team(exhibit.Id, "Other Team");
        await Seed(other);
        var actor = await Actor().OnExhibit(exhibit, [ExhibitPermission.ManageExhibit]).SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).PutAsJsonAsync($"api/teamarticles/{teamArticle.Id}",
            new { id = teamArticle.Id, exhibitId = exhibit.Id, teamId = other.Id, articleId = teamArticle.ArticleId }, Ct));

        await using var db = NewContext();
        Assert.Equal(other.Id, (await db.TeamArticles.SingleAsync(x => x.Id == teamArticle.Id, Ct)).TeamId);
    }

    /// <summary>The gate reads the exhibit the body names, so the team article moves into the caller's exhibit.</summary>
    [Fact]
    public async Task Update_moves_another_exhibit_s_team_article_when_the_body_names_an_exhibit_the_caller_manages()
    {
        var (exhibit, team, teamArticle) = await SeedTeamArticle();
        var actor = await Actor().OnNewExhibit(exhibit.CollectionId, ExhibitPermission.ManageExhibit).SeedAsync();
        var mine = Assert.Single(actor.NewExhibitIds);

        await AssertStatus(HttpStatusCode.OK, await Client(actor).PutAsJsonAsync($"api/teamarticles/{teamArticle.Id}",
            new { id = teamArticle.Id, exhibitId = mine, teamId = team.Id, articleId = teamArticle.ArticleId }, Ct));

        await using var db = NewContext();
        Assert.Equal(mine, (await db.TeamArticles.SingleAsync(x => x.Id == teamArticle.Id, Ct)).ExhibitId);
    }

    [Fact]
    public async Task Update_is_forbidden_for_a_caller_holding_only_ViewExhibits()
    {
        var (exhibit, team, teamArticle) = await SeedTeamArticle();
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewExhibits).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).PutAsJsonAsync($"api/teamarticles/{teamArticle.Id}",
            new { id = teamArticle.Id, exhibitId = exhibit.Id, teamId = team.Id, articleId = teamArticle.ArticleId }, Ct));
    }

    [Fact]
    public async Task Delete_removes_the_team_article_for_a_caller_holding_ManageExhibits()
    {
        var (_, _, teamArticle) = await SeedTeamArticle();
        var actor = await Actor().WithSystemPermissions(SystemPermission.ManageExhibits).SeedAsync();

        await AssertStatus(HttpStatusCode.NoContent, await Client(actor).DeleteAsync($"api/teamarticles/{teamArticle.Id}", Ct));

        await using var db = NewContext();
        Assert.False(await db.TeamArticles.AnyAsync(x => x.Id == teamArticle.Id, Ct));
    }

    [Fact]
    public async Task Delete_is_forbidden_for_a_caller_holding_ManageExhibit_only_on_another_exhibit()
    {
        var (exhibit, _, teamArticle) = await SeedTeamArticle();
        var actor = await Actor().OnNewExhibit(exhibit.CollectionId, ExhibitPermission.ManageExhibit).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).DeleteAsync($"api/teamarticles/{teamArticle.Id}", Ct));
    }

    [Fact]
    public async Task DeleteByIds_removes_the_team_article_for_a_member_holding_ManageExhibit()
    {
        var (exhibit, team, teamArticle) = await SeedTeamArticle();
        var actor = await Actor().OnExhibit(exhibit, [ExhibitPermission.ManageExhibit]).SeedAsync();

        await AssertStatus(HttpStatusCode.NoContent, await Client(actor).DeleteAsync($"api/teams/{team.Id}/articles/{teamArticle.ArticleId}", Ct));

        await using var db = NewContext();
        Assert.False(await db.TeamArticles.AnyAsync(x => x.Id == teamArticle.Id, Ct));
    }

    [Fact]
    public async Task DeleteByIds_is_forbidden_for_a_caller_holding_only_EditExhibit()
    {
        var (exhibit, team, teamArticle) = await SeedTeamArticle();
        var actor = await Actor().OnExhibit(exhibit, [ExhibitPermission.EditExhibit]).SeedAsync();

        await AssertProblem(HttpStatusCode.Forbidden, await Client(actor).DeleteAsync($"api/teams/{team.Id}/articles/{teamArticle.ArticleId}", Ct));
    }

    private async Task<(ExhibitEntity Exhibit, TeamEntity Team, TeamArticleEntity TeamArticle)> SeedTeamArticle()
    {
        var collection = TestData.Collection();
        var exhibit = TestData.Exhibit(collection.Id);
        var team = TestData.Team(exhibit.Id);
        var article = TestData.Article(collection.Id);
        var teamArticle = TestData.TeamArticle(exhibit.Id, team.Id, article.Id);
        await Seed(collection, exhibit, team, article, teamArticle);

        return (exhibit, team, teamArticle);
    }
}
