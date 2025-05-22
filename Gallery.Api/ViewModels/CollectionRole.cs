// Copyright 2024 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using Gallery.Api.Data.Enumerations;

namespace Gallery.Api.ViewModels
{
    public class CollectionRole
    {

        public Guid Id { get; set; }

        public string Name { get; set; }
        public bool AllPermissions { get; set; }

        public CollectionPermission[] Permissions { get; set; }
    }
}
