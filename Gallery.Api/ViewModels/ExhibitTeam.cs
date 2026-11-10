// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gallery.Api.ViewModels
{
    public class ExhibitTeam : Base
    {
        public ExhibitTeam() { }

        public ExhibitTeam(Guid teamId, Guid exhibitId)
        {
            TeamId = teamId;
            ExhibitId = exhibitId;
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        public Guid TeamId { get; set; }
        public Team Team { get; set; }

        public Guid ExhibitId { get; set; }
        public Exhibit Exhibit { get; set; }
    }

    /// <summary>
    /// An ExhibitTeam create request. It names the Team and the Exhibit by Id only, so it cannot create or change either.
    /// </summary>
    public class ExhibitTeamCreate
    {
        /// <summary>The Id to create the ExhibitTeam with. A new one is made when it is empty.</summary>
        public Guid Id { get; set; }
        public Guid TeamId { get; set; }
        public Guid ExhibitId { get; set; }
    }
}
