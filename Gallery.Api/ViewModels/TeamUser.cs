// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gallery.Api.ViewModels
{
    public class TeamUser : Base
    {
        public TeamUser() { }

        public TeamUser(Guid userId, Guid teamId)
        {
            UserId = userId;
            TeamId = teamId;
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        public Guid UserId { get; set; }
        public User User { get; set; }

        public Guid TeamId { get; set; }
        public Team Team { get; set; }
        public Boolean IsObserver { get; set; }
    }

    /// <summary>
    /// A TeamUser create request. It names the User and the Team by Id only, so it cannot create or change either.
    /// </summary>
    public class TeamUserCreate
    {
        /// <summary>The Id to create the TeamUser with. A new one is made when it is empty.</summary>
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid TeamId { get; set; }
        public bool IsObserver { get; set; }
    }
}
