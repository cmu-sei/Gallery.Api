// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Gallery.Api.Infrastructure.Exceptions;
using Gallery.Api.Services;

namespace Gallery.Api.Tests.Services;

/// <summary><c>ImportBuilder.BuildCollectionContent</c>'s handling of an article url, which every copy and upload shares.</summary>
public class ImportBuilderTests
{
    /// <summary>
    /// An article whose url is not http or https refuses the import with a reason that names the article and says how
    /// to fix it, but leaves out the url, which can be a script.
    /// </summary>
    [Theory]
    [InlineData("javascript:alert(document.domain)")]
    [InlineData("data:text/html,<script>alert(1)</script>")]
    [InlineData("/assets/page.html")]
    public void BuildCollectionContent_refuses_a_url_that_is_not_http_or_https(string url)
    {
        var sourceId = Guid.NewGuid();

        var refused = Assert.Throws<BadRequestException>(() => ImportBuilder.BuildCollectionContent(Guid.NewGuid(), [],
            [new ArticleImport { Name = "Allowed", Url = "https://news.example/a" }, new ArticleImport { Id = sourceId, Name = "Scripted", Url = url }],
            null, null, []));

        Assert.Equal(
            $"Article \"Scripted\" ({sourceId}) has a url that is not an http or https url. Edit the article to fix or clear its url, then try again.",
            refused.Message);
        Assert.DoesNotContain(url, refused.Message);
    }

    /// <summary>An article of another exhibit is left out before its url is looked at, so it cannot refuse the import.</summary>
    [Fact]
    public void BuildCollectionContent_leaves_out_another_exhibit_s_article_whatever_its_url()
    {
        var (_, articles, _) = ImportBuilder.BuildCollectionContent(Guid.NewGuid(), [],
            [new ArticleImport { Name = "Sibling's", ExhibitId = Guid.NewGuid(), Url = "javascript:alert(1)" }],
            Guid.NewGuid(), Guid.NewGuid(), []);

        Assert.Empty(articles);
    }

    /// <summary>An empty, http or https url is kept as it is.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("http://news.example/a")]
    [InlineData("https://news.example/a")]
    public void BuildCollectionContent_keeps_an_empty_or_http_or_https_url(string url)
    {
        var (_, articles, _) = ImportBuilder.BuildCollectionContent(Guid.NewGuid(), [],
            [new ArticleImport { Name = "Allowed", Url = url }], null, null, []);

        Assert.Equal(url, Assert.Single(articles).Url);
    }
}
