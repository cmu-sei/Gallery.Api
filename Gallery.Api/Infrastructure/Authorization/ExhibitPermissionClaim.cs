// Copyright 2024 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Text.Json;
using Gallery.Api.Data.Enumerations;

namespace Gallery.Api.Infrastructure.Authorization;

public class ExhibitPermissionClaim
{
    public Guid ExhibitId { get; set; }
    public ExhibitPermission[] Permissions { get; set; } = [];

    public ExhibitPermissionClaim() { }

    public static ExhibitPermissionClaim FromString(string json)
    {
        return JsonSerializer.Deserialize<ExhibitPermissionClaim>(json);
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this);
    }
}
