// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Net;
using System.Net.Http.Json;
using Gallery.Api.Hubs;
using Gallery.Api.Services;
using Gallery.Api.Tests.Support;
using Gallery.Api.ViewModels;

namespace Gallery.Api.Tests.Infrastructure.EventHandlers;

/// <summary>
/// The exhibit handlers, reached through a request. An update also sends every participant their unread
/// count on <see cref="CiteHub"/>.
/// </summary>
public class ExhibitHandlerTests(DatabaseFixture fixture, GalleryAppFactory factory) : ApiTestBase(fixture, factory)
{
    [Fact]
    public async Task Creating_an_exhibit_sends_ExhibitCreated_to_its_group()
    {
        var collection = TestData.Collection();
        await Seed(collection);

        var created = await ReadAsync<Exhibit>(await RootClient.PostAsJsonAsync("api/exhibits", new { collectionId = collection.Id }, Ct));

        var broadcast = Assert.Single(Factory.Hub<MainHub>().ToGroup(created.Id));
        Assert.Equal(MainHubMethods.ExhibitCreated, broadcast.Method);
        Assert.Null(broadcast.Arguments[1]);
    }

    [Fact]
    public async Task Moving_an_exhibit_sends_ExhibitUpdated_with_the_modified_properties()
    {
        var collection = TestData.Collection();
        var exhibit = TestData.Exhibit(collection.Id);
        await Seed(collection, exhibit);

        await AssertStatus(HttpStatusCode.OK, await RootClient.PutAsync($"api/exhibits/{exhibit.Id}/move/2/inject/1", null, Ct));

        var broadcast = Assert.Single(Factory.Hub<MainHub>().ToGroup(exhibit.Id));
        Assert.Equal(MainHubMethods.ExhibitUpdated, broadcast.Method);
        Assert.Equal(2, Assert.IsType<Exhibit>(broadcast.Arguments[0]).CurrentMove);
        Assert.Equal(["currentInject", "currentMove", "dateModified"], Assert.IsType<string[]>(broadcast.Arguments[1]).Order());
    }

    /// <summary>
    /// CITE's unread count goes to each participant's <c>-cite</c> group, as the count and a null second
    /// argument: once from the exhibit update and once from the delivery it causes.
    /// </summary>
    [Fact]
    public async Task Moving_an_exhibit_sends_each_participant_their_unread_count_on_the_cite_hub()
    {
        var collection = TestData.Collection();
        var exhibit = TestData.Exhibit(collection.Id);
        var team = TestData.Team(exhibit.Id);
        var article = TestData.Article(collection.Id, move: 1);
        var participant = TestData.User(name: "Participant");
        await Seed(collection, exhibit, team, article, participant, TestData.TeamUser(team.Id, participant.Id),
            TestData.TeamArticle(exhibit.Id, team.Id, article.Id));

        await AssertStatus(HttpStatusCode.OK, await RootClient.PutAsync($"api/exhibits/{exhibit.Id}/move/1/inject/0", null, Ct));

        var broadcasts = Factory.Hub<CiteHub>().ToGroup($"{participant.Id}{CiteHubMethods.GroupNameSuffix}");
        Assert.Equal(2, broadcasts.Count);
        Assert.All(broadcasts, x =>
        {
            Assert.Equal(CiteHubMethods.UnreadCountUpdated, x.Method);
            Assert.Equal((exhibit.Id, "1"), (Assert.IsType<UnreadArticles>(x.Arguments[0]).ExhibitId, ((UnreadArticles)x.Arguments[0]).Count));
            Assert.Null(x.Arguments[1]);
        });
    }

    [Fact]
    public async Task Deleting_an_exhibit_sends_ExhibitDeleted_with_the_bare_id()
    {
        var collection = TestData.Collection();
        var exhibit = TestData.Exhibit(collection.Id);
        await Seed(collection, exhibit);

        await AssertStatus(HttpStatusCode.NoContent, await RootClient.DeleteAsync($"api/exhibits/{exhibit.Id}", Ct));

        var broadcast = Assert.Single(Factory.Hub<MainHub>().ToGroup(exhibit.Id));
        Assert.Equal((MainHubMethods.ExhibitDeleted, (object)exhibit.Id), (broadcast.Method, broadcast.Argument));
    }
}
