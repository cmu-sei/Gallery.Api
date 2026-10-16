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

    /// <summary>
    /// The one field an Exhibit Membership's edit request carries. The Exhibit, User and Group are not among
    /// them, so an edit cannot move a membership or hand it to someone else.
    /// </summary>
    public class ExhibitMembershipEdit
    {
        public Guid RoleId { get; set; }
    }
}
