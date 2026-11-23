// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Gallery.Api.Services;

namespace Gallery.Api.Tests.Services;

/// <summary><c>ArticleUrl.IsAllowed</c>, the rule every write of an article's url is held to.</summary>
public class ArticleUrlTests
{
    /// <summary>An article can show its description instead of a url, so no url at all is allowed.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsAllowed_allows_no_url(string url)
    {
        Assert.True(ArticleUrl.IsAllowed(url));
    }

    [Theory]
    [InlineData("http://news.example/outage")]
    [InlineData("https://news.example/outage?id=1#top")]
    [InlineData("HTTPS://NEWS.EXAMPLE/")]
    [InlineData("https://10.0.0.5:8443/page")]
    [InlineData(" https://news.example/padded ")]
    public void IsAllowed_allows_an_absolute_http_or_https_url(string url)
    {
        Assert.True(ArticleUrl.IsAllowed(url));
    }

    /// <summary>A url the UI would run as script, or load from somewhere other than an http(s) host, is refused.</summary>
    [Theory]
    [InlineData("javascript:alert(document.domain)")]
    [InlineData("JavaScript:alert(1)")]
    [InlineData(" javascript:alert(1)")]
    [InlineData("java\tscript:alert(1)")]
    [InlineData("data:text/html,<script>alert(1)</script>")]
    [InlineData("vbscript:msgbox(1)")]
    [InlineData("blob:https://gallery.example/0b1c")]
    [InlineData("file:///etc/passwd")]
    [InlineData("ftp://files.example/a")]
    [InlineData("/assets/page.html")]
    [InlineData("news.example/outage")]
    [InlineData("//news.example/outage")]
    [InlineData("https://")]
    [InlineData("https:news.example")]
    public void IsAllowed_refuses_any_other_url(string url)
    {
        Assert.False(ArticleUrl.IsAllowed(url));
    }
}
