// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

namespace Api.Infrastructure.Options
{
    public class ClientOptions
    {
        public string SteamfitterApiUrl { get; set; }
        public bool IsEmailActive { get; set; }
    }
}
