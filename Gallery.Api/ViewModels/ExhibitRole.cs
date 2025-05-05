// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using Gallery.Api.Data;

namespace Gallery.Api.ViewModels
{
    public class ExhibitRole
    {

        public Guid Id { get; set; }

        public string Name { get; set; }
        public bool AllPermissions { get; set; }

        public ExhibitPermission[] Permissions { get; set; }
    }
}
