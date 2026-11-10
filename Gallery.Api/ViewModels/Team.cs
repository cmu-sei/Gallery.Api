// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;

namespace Gallery.Api.ViewModels
{
    public class Team : Base, IAuthorizationType
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string ShortName { get; set; }
        public string Email { get; set; }
        public Guid? ExhibitId { get; set; }
        public virtual Exhibit Exhibit { get; set; }
        public ICollection<User> Users { get; set; } = new List<User>();
        public ICollection<Article> Articles { get; set; }
    }

    /// <summary>
    /// The fields a Team's create and edit requests share. The Exhibit is not one of them: only
    /// <see cref="TeamCreate"/> names it, so an edit cannot move a Team to another Exhibit.
    /// </summary>
    public abstract class TeamFields
    {
        public string Name { get; set; }
        public string ShortName { get; set; }
        public string Email { get; set; }
    }

    public class TeamCreate : TeamFields
    {
        /// <summary>The Id to create the Team with. A new one is made when it is empty.</summary>
        public Guid Id { get; set; }
        public Guid? ExhibitId { get; set; }
    }

    public class TeamEdit : TeamFields
    {
    }
}
