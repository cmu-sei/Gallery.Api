// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Net;
using System.Net.Http.Json;
using Gallery.Api.Hubs;
using Gallery.Api.Services;
using Gallery.Api.Tests.Support;
using Gallery.Api.ViewModels;

namespace Gallery.Api.Tests.Infrastructure.EventHandlers;

/// <summary>The user article handlers, reached through a request: the reader's archive and CITE's unread count.</summary>
public class UserArticleHandlerTests(DatabaseFixture fixture, GalleryAppFactory factory) : ApiTestBase(fixture, factory)
{
    [Fact]
    public async Task Marking_an_article_read_sends_UserArticleUpdated_to_the_reader()
    {
        var (exhibit, team, article) = await SeedExhibit();
        var reader = await Actor().OnTeam(team).SeedAsync();
        var userArticle = TestData.UserArticle(exhibit.Id, reader.Id, article.Id);
        await Seed(userArticle);

        await AssertStatus(HttpStatusCode.OK, await Client(reader).PutAsJsonAsync($"api/userarticles/{userArticle.Id}/isread", true, Ct));

        var broadcast = Assert.Single(Factory.Hub<MainHub>().ToGroup(reader.Id));
        Assert.Equal(MainHubMethods.UserArticleUpdated, broadcast.Method);
        Assert.True(Assert.IsType<UserArticle>(broadcast.Arguments[0]).IsRead);
        Assert.Contains("isRead", Assert.IsType<string[]>(broadcast.Arguments[1]));
    }

    [Fact]
    public async Task Marking_an_article_read_sends_the_reader_s_new_unread_count_on_the_cite_hub()
    {
        var (exhibit, team, article) = await SeedExhibit();
        var reader = await Actor().OnTeam(team).SeedAsync();
        var userArticle = TestData.UserArticle(exhibit.Id, reader.Id, article.Id);
        await Seed(userArticle);

        await AssertStatus(HttpStatusCode.OK, await Client(reader).PutAsJsonAsync($"api/userarticles/{userArticle.Id}/isread", true, Ct));

        var broadcast = Assert.Single(Factory.Hub<CiteHub>().ToGroup($"{reader.Id}{CiteHubMethods.GroupNameSuffix}"));
        Assert.Equal(CiteHubMethods.UnreadCountUpdated, broadcast.Method);
        Assert.Equal("0", Assert.IsType<UnreadArticles>(broadcast.Arguments[0]).Count);
    }

    /// <summary>An article delivered by a team article reaches the reader as UserArticleCreated.</summary>
    [Fact]
    public async Task Delivering_an_article_sends_UserArticleCreated_to_the_reader()
    {
        var (exhibit, team, article) = await SeedExhibit();
        var reader = TestData.User(name: "Reader");
        await Seed(reader, TestData.TeamUser(team.Id, reader.Id));

        await AssertStatus(HttpStatusCode.Created, await RootClient.PostAsJsonAsync("api/teamarticles", new { exhibitId = exhibit.Id, teamId = team.Id, articleId = article.Id }, Ct));

        var broadcast = Assert.Single(Factory.Hub<MainHub>().ToGroup(reader.Id));
        Assert.Equal(MainHubMethods.UserArticleCreated, broadcast.Method);
        Assert.Equal(article.Id, Assert.IsType<UserArticle>(broadcast.Arguments[0]).ArticleId);
        Assert.Null(broadcast.Arguments[1]);
    }

    [Fact]
    public async Task Deleting_a_user_article_sends_UserArticleDeleted_with_the_bare_id_to_the_reader()
    {
        var (exhibit, _, article) = await SeedExhibit();
        var reader = TestData.User(name: "Reader");
        var userArticle = TestData.UserArticle(exhibit.Id, reader.Id, article.Id);
        await Seed(reader, userArticle);

        await AssertStatus(HttpStatusCode.NoContent, await RootClient.DeleteAsync($"api/userarticles/{userArticle.Id}", Ct));

        var broadcast = Assert.Single(Factory.Hub<MainHub>().ToGroup(reader.Id));
        Assert.Equal((MainHubMethods.UserArticleDeleted, (object)userArticle.Id), (broadcast.Method, broadcast.Argument));
    }

    private async Task<(Gallery.Api.Data.Models.ExhibitEntity, Gallery.Api.Data.Models.TeamEntity, Gallery.Api.Data.Models.ArticleEntity)> SeedExhibit()
    {
        var collection = TestData.Collection();
        var exhibit = TestData.Exhibit(collection.Id);
        var team = TestData.Team(exhibit.Id);
        var article = TestData.Article(collection.Id);
        await Seed(collection, exhibit, team, article);

        return (exhibit, team, article);
    }
}
