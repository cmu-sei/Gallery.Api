// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;

namespace Gallery.Api.ViewModels
{
    public class Team : Base
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

}
