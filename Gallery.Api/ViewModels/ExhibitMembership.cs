// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;

namespace Gallery.Api.ViewModels
{
    public class ExhibitMembership : IAuthorizationType
    {
        public Guid Id { get; set; }

        public Guid ExhibitId { get; set; }

        public Guid? UserId { get; set; }

        public Guid? GroupId { get; set; }

        public Guid RoleId { get; set; }
    }
}
