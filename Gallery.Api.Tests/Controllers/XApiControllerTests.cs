// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Net;
using Gallery.Api.Data.Models;
using Gallery.Api.Tests.Support;

namespace Gallery.Api.Tests.Controllers;

/// <summary>
/// <c>XApiController</c> with the shipped configuration, where no learning record store is configured, so
/// every statement is skipped and the routes answer 200 once the entities they name exist.
/// </summary>
/// <remarks>
/// The routes check no permission beyond an identity, so the callers here are participants: the actor the
/// UI sends these on behalf of.
/// </remarks>
public class XApiControllerTests(DatabaseFixture fixture, GalleryAppFactory factory) : ApiTestBase(fixture, factory)
{
    /// <summary>A request with no identity is answered with a 401; the anonymous client is the case under test.</summary>
    [Fact]
    public async Task ViewedExhibitWall_without_an_identity_is_unauthorized()
    {
        await AssertStatus(HttpStatusCode.Unauthorized, await Client().GetAsync($"api/xapi/viewed/exhibit/{Guid.NewGuid()}/wall", Ct));
    }

    [Fact]
    public async Task ViewedExhibitWall_is_answered_for_a_participant()
    {
        var (exhibit, team, _, _) = await SeedExhibit();
        var actor = await Actor().OnTeam(team).SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).GetAsync($"api/xapi/viewed/exhibit/{exhibit.Id}/wall", Ct));
    }

    [Fact]
    public async Task ViewedExhibitArchive_is_answered_for_a_participant()
    {
        var (exhibit, team, _, _) = await SeedExhibit();
        var actor = await Actor().OnTeam(team).SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).GetAsync($"api/xapi/viewed/exhibit/{exhibit.Id}/archive", Ct));
    }

    /// <summary>A participant of another exhibit records a view of this exhibit's wall.</summary>
    [Fact]
    public async Task ViewedExhibitWall_is_answered_for_a_participant_only_of_another_exhibit()
    {
        var (exhibit, _, _, _) = await SeedExhibit();
        var actor = await Actor().OnNewTeam((await SeedExhibit()).Exhibit.Id).SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).GetAsync($"api/xapi/viewed/exhibit/{exhibit.Id}/wall", Ct));
    }

    [Fact]
    public async Task ViewedExhibitWall_reports_an_unknown_exhibit_as_not_found()
    {
        await AssertProblem(HttpStatusCode.NotFound, await RootClient.GetAsync($"api/xapi/viewed/exhibit/{Guid.NewGuid()}/wall", Ct));
    }

    [Fact]
    public async Task ViewedCard_is_answered_for_a_participant()
    {
        var (exhibit, team, card, _) = await SeedExhibit();
        var actor = await Actor().OnTeam(team).SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).GetAsync($"api/xapi/viewed/exhibit/{exhibit.Id}/card/{card.Id}", Ct));
    }

    [Fact]
    public async Task ViewedCard_reports_an_unknown_card_as_not_found()
    {
        var (exhibit, _, _, _) = await SeedExhibit();

        await AssertProblem(HttpStatusCode.NotFound, await RootClient.GetAsync($"api/xapi/viewed/exhibit/{exhibit.Id}/card/{Guid.NewGuid()}", Ct));
    }

    [Fact]
    public async Task ViewedArticle_is_answered_for_a_participant()
    {
        var (exhibit, team, _, article) = await SeedExhibit();
        var actor = await Actor().OnTeam(team).SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).GetAsync($"api/xapi/viewed/exhibit/{exhibit.Id}/article/{article.Id}", Ct));
    }

    /// <summary>A caller on no team of the exhibit is answered with a 500 for a viewed or previewed article.</summary>
    [Theory]
    [InlineData("viewed")]
    [InlineData("previewed")]
    public async Task An_article_view_answers_a_caller_on_no_team_of_the_exhibit_with_a_server_error(string verb)
    {
        var (exhibit, _, _, article) = await SeedExhibit();

        var problem = await AssertProblem(HttpStatusCode.InternalServerError,
            await RootClient.GetAsync($"api/xapi/{verb}/exhibit/{exhibit.Id}/article/{article.Id}", Ct));

        Assert.Equal("Object reference not set to an instance of an object.", problem.Detail);
    }

    /// <summary>An article with no card is answered with a 500 for a viewed or previewed article.</summary>
    [Theory]
    [InlineData("viewed")]
    [InlineData("previewed")]
    public async Task An_article_view_of_an_article_without_a_card_answers_a_server_error(string verb)
    {
        var (exhibit, team, _, _) = await SeedExhibit();
        var cardless = TestData.Article(exhibit.CollectionId, name: "Cardless");
        await Seed(cardless);
        var actor = await Actor().OnTeam(team).SeedAsync();

        var problem = await AssertProblem(HttpStatusCode.InternalServerError,
            await Client(actor).GetAsync($"api/xapi/{verb}/exhibit/{exhibit.Id}/article/{cardless.Id}", Ct));

        Assert.Equal("Nullable object must have a value.", problem.Detail);
    }

    [Fact]
    public async Task PreviewedArticle_is_answered_for_a_participant()
    {
        var (exhibit, team, _, article) = await SeedExhibit();
        var actor = await Actor().OnTeam(team).SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).GetAsync($"api/xapi/previewed/exhibit/{exhibit.Id}/article/{article.Id}", Ct));
    }

    [Fact]
    public async Task ObservedExhibitWall_is_answered_for_an_observer()
    {
        var (exhibit, team, _, _) = await SeedExhibit();
        var actor = await Actor().OnTeam(team, observer: true).SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).GetAsync($"api/xapi/observed/exhibit/{exhibit.Id}/team/{team.Id}/wall", Ct));
    }

    [Fact]
    public async Task ObservedExhibitArchive_reports_an_unknown_team_as_not_found()
    {
        var (exhibit, _, _, _) = await SeedExhibit();

        await AssertProblem(HttpStatusCode.NotFound,
            await RootClient.GetAsync($"api/xapi/observed/exhibit/{exhibit.Id}/team/{Guid.NewGuid()}/archive", Ct));
    }

    private async Task<(ExhibitEntity Exhibit, TeamEntity Team, CardEntity Card, ArticleEntity Article)> SeedExhibit()
    {
        var collection = TestData.Collection();
        var exhibit = TestData.Exhibit(collection.Id);
        var team = TestData.Team(exhibit.Id);
        var card = TestData.Card(collection.Id);
        var article = TestData.Article(collection.Id, card.Id);
        await Seed(collection, exhibit, team, card, article);

        return (exhibit, team, card, article);
    }
}
