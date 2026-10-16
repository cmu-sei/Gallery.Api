// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gallery.Api.ViewModels
{
    public class TeamCard : Base
    {
        public TeamCard() {
            Move = 0;
            Inject = 0;
            IsShownOnWall = true;
            CanPostArticles = false;
        }

        public TeamCard(Guid cardId, Guid teamId)
        {
            CardId = cardId;
            TeamId = teamId;
            Move = 0;
            Inject = 0;
            IsShownOnWall = true;
            CanPostArticles = false;
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }
        public int Move { get; set; }
        public int Inject { get; set; }
        public bool IsShownOnWall { get; set; }
        public bool CanPostArticles { get; set; }
        public Guid TeamId { get; set; }
        public Guid CardId { get; set; }
    }

    /// <summary>
    /// The fields a TeamCard's create and edit requests share. On an edit, the Team must be in the stored
    /// TeamCard's Exhibit and the Card in that Exhibit's Collection.
    /// </summary>
    public abstract class TeamCardFields
    {
        public Guid TeamId { get; set; }
        public Guid CardId { get; set; }
        public int Move { get; set; }
        public int Inject { get; set; }
        public bool IsShownOnWall { get; set; } = true;
        public bool CanPostArticles { get; set; }
    }

    public class TeamCardCreate : TeamCardFields
    {
        /// <summary>The Id to create the TeamCard with. A new one is made when it is empty.</summary>
        public Guid Id { get; set; }
    }

    public class TeamCardEdit : TeamCardFields
    {
    }
}
