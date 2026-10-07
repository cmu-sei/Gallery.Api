// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Net;
using System.Net.Http.Json;
using Gallery.Api.Hubs;
using Gallery.Api.Tests.Support;
using Gallery.Api.ViewModels;

namespace Gallery.Api.Tests.Infrastructure.EventHandlers;

/// <summary>
/// The article handlers, reached the way production reaches them: a request saves, the entity event
/// interceptor publishes, MediatR runs the handler, and the handler broadcasts on <see cref="MainHub"/>.
/// Each test reads the group named by an id it created.
/// </summary>
public class ArticleHandlerTests(DatabaseFixture fixture, GalleryAppFactory factory) : ApiTestBase(fixture, factory)
{
    [Fact]
    public async Task Creating_an_article_sends_ArticleCreated_with_the_article_and_no_modified_properties()
    {
        var collection = TestData.Collection();
        await Seed(collection);

        var created = await ReadAsync<Article>(await RootClient.PostAsJsonAsync("api/articles", NewArticle(collection.Id, "Broadcast"), Ct));

        var broadcast = Assert.Single(Factory.Hub<MainHub>().ToGroup(created.Id));
        Assert.Equal(MainHubMethods.ArticleCreated, broadcast.Method);
        Assert.Equal("Broadcast", Assert.IsType<Article>(broadcast.Arguments[0]).Name);
        Assert.Null(broadcast.Arguments[1]);
    }

    [Fact]
    public async Task Updating_an_article_sends_ArticleUpdated_with_the_camel_cased_modified_properties()
    {
        var collection = TestData.Collection();
        var article = TestData.Article(collection.Id);
        await Seed(collection, article);

        await AssertStatus(HttpStatusCode.OK, await RootClient.PutAsJsonAsync($"api/articles/{article.Id}", NewArticle(collection.Id, "Renamed", article.Id), Ct));

        var broadcast = Assert.Single(Factory.Hub<MainHub>().ToGroup(article.Id), x => x.Method == MainHubMethods.ArticleUpdated);
        Assert.Equal("Renamed", Assert.IsType<Article>(broadcast.Arguments[0]).Name);
        Assert.Contains("name", Assert.IsType<string[]>(broadcast.Arguments[1]));
    }

    [Fact]
    public async Task Deleting_an_article_sends_ArticleDeleted_with_the_bare_id()
    {
        var collection = TestData.Collection();
        var article = TestData.Article(collection.Id);
        await Seed(collection, article);

        await AssertStatus(HttpStatusCode.NoContent, await RootClient.DeleteAsync($"api/articles/{article.Id}", Ct));

        var broadcast = Assert.Single(Factory.Hub<MainHub>().ToGroup(article.Id));
        Assert.Equal(MainHubMethods.ArticleDeleted, broadcast.Method);
        Assert.Equal(article.Id, broadcast.Argument);
    }

    /// <summary>A collection article is sent to the exhibit admin group and not the collection admin group.</summary>
    [Fact]
    public async Task Creating_a_collection_article_sends_it_to_the_exhibit_admin_group()
    {
        var collection = TestData.Collection();
        await Seed(collection);

        var created = await ReadAsync<Article>(await RootClient.PostAsJsonAsync("api/articles", NewArticle(collection.Id, "Admin Broadcast"), Ct));

        var hub = Factory.Hub<MainHub>();
        Assert.Contains(hub.ToGroup(MainHub.EXHIBIT_GROUP), x => x.Arguments[0] is Article a && a.Id == created.Id);
        Assert.DoesNotContain(hub.ToGroup(MainHub.COLLECTION_GROUP), x => x.Arguments[0] is Article a && a.Id == created.Id);
    }

    /// <summary>An article delivered to a user is also sent to that user's group.</summary>
    [Fact]
    public async Task Updating_a_delivered_article_sends_it_to_the_reader_s_group()
    {
        var collection = TestData.Collection();
        var exhibit = TestData.Exhibit(collection.Id);
        var article = TestData.Article(collection.Id);
        var reader = TestData.User(name: "Reader");
        await Seed(collection, exhibit, article, reader, TestData.UserArticle(exhibit.Id, reader.Id, article.Id));

        await AssertStatus(HttpStatusCode.OK, await RootClient.PutAsJsonAsync($"api/articles/{article.Id}", NewArticle(collection.Id, "Revised", article.Id), Ct));

        Assert.Contains(Factory.Hub<MainHub>().ToGroup(reader.Id), x => x.Method == MainHubMethods.ArticleUpdated);
    }

    private static object NewArticle(Guid collectionId, string name, Guid? id = null) =>
        new { id = id ?? Guid.Empty, name, collectionId, sourceType = "News", status = "Open", datePosted = TestData.DefaultDatePosted };
}
