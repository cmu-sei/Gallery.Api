// Copyright 2024 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Text.Json;
using Gallery.Api.Data.Enumerations;

namespace Gallery.Api.Infrastructure.Authorization;

public class CollectionPermissionClaim
{
    public Guid CollectionId { get; set; }
    public CollectionPermission[] Permissions { get; set; } = [];

    public CollectionPermissionClaim() { }

    public static CollectionPermissionClaim FromString(string json)
    {
        return JsonSerializer.Deserialize<CollectionPermissionClaim>(json);
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this);
    }
}
