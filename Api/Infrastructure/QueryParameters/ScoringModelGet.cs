// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;

namespace Api.Infrastructure.QueryParameters
{
    public class ScoringModelGet
    {
        /// <summary>
        /// Whether or not to return records only for a designated user
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Whether or not to return records only for descriptions containing the designated string
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Whether or not to return archived records
        /// </summary>
        public bool? IncludeArchived { get; set; }

    }
}

