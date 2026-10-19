// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;

namespace Gallery.Api.Infrastructure.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        /// Converts a string to CamelCase, assuming it is already in TitleCase
        /// </summary>
        public static string TitleCaseToCamelCase(this string str)
        {
            var camelCaseStr = str;

            if (!string.IsNullOrEmpty(str) && str.Length > 1)
            {
                camelCaseStr = Char.ToLowerInvariant(camelCaseStr[0]) + camelCaseStr.Substring(1);
            }

            return camelCaseStr;
        }

        /// <summary>
        /// Removes line breaks, so a user-supplied value cannot forge extra log lines. Null gives an empty string.
        /// </summary>
        public static string SanitizeForLog(this string str) =>
            (str ?? string.Empty).Replace("\r", string.Empty).Replace("\n", string.Empty);
    }
}
