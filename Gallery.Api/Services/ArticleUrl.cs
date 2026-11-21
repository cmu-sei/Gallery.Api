// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;

namespace Gallery.Api.Services
{
    /// <summary>
    /// The rule for an article's url, which the UI loads in an iframe and opens in a new tab. A url is optional,
    /// since an article can show its description instead, so an empty one is allowed.
    /// </summary>
    public static class ArticleUrl
    {
        /// <summary>
        /// Whether <paramref name="url"/> is empty, or an absolute http or https url with a host. Any other scheme,
        /// such as javascript: or data:, and a relative url are refused.
        /// </summary>
        public static bool IsAllowed(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return true;

            // a path such as "/a" is read as a file url on Linux, which the scheme check refuses as well
            return Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) &&
                !string.IsNullOrEmpty(uri.Host);
        }
    }
}
