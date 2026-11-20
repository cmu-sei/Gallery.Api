// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Gallery.Api.Services;
using Microsoft.Extensions.Logging;

namespace Gallery.Api.Tests.Services;

/// <summary><c>ImportBuilder.BuildCollectionContent</c>'s handling of an article url, which every copy and upload shares.</summary>
public class ImportBuilderTests
{
    /// <summary>
    /// An article whose url is not http or https is built without it, and a warning names the article but not the url,
    /// which can be a script. Every other member is copied.
    /// </summary>
    [Theory]
    [InlineData("javascript:alert(document.domain)")]
    [InlineData("data:text/html,<script>alert(1)</script>")]
    [InlineData("/assets/page.html")]
    public void BuildCollectionContent_drops_a_url_that_is_not_http_or_https_and_warns(string url)
    {
        var logger = new RecordingLogger();
        var sourceId = Guid.NewGuid();

        var (_, articles, idMap) = ImportBuilder.BuildCollectionContent(Guid.NewGuid(), [],
            [new ArticleImport { Id = sourceId, Name = "Scripted", Summary = "Kept", Url = url, OpenInNewTab = true, Move = 2 }],
            null, null, [], logger);

        var article = Assert.Single(articles);
        Assert.Equal((null, "Scripted", "Kept", true, 2), (article.Url, article.Name, article.Summary, article.OpenInNewTab, article.Move));
        Assert.Equal(article.Id, idMap[sourceId]);
        var warning = Assert.Single(logger.At(LogLevel.Warning));
        Assert.Contains(sourceId.ToString(), warning.Message);
        Assert.Contains("Scripted", warning.Message);
        Assert.Contains(article.Id.ToString(), warning.Message);
        Assert.DoesNotContain(url, warning.Message);
        Assert.Single(logger.Entries);
    }

    /// <summary>An empty, http or https url is kept as it is, and nothing is logged.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("http://news.example/a")]
    [InlineData("https://news.example/a")]
    public void BuildCollectionContent_keeps_an_empty_or_http_or_https_url(string url)
    {
        var logger = new RecordingLogger();

        var (_, articles, _) = ImportBuilder.BuildCollectionContent(Guid.NewGuid(), [],
            [new ArticleImport { Name = "Allowed", Url = url }], null, null, [], logger);

        Assert.Equal(url, Assert.Single(articles).Url);
        Assert.Empty(logger.Entries);
    }
}
