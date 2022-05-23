// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.ViewModels
{
    public class TeamCard : Base
    {
        public TeamCard() {
            Move = 0;
            Inject = 0;
            IsShownOnWall = true;
        }

        public TeamCard(Guid cardId, Guid teamId)
        {
            CardId = cardId;
            TeamId = teamId;
            Move = 0;
            Inject = 0;
            IsShownOnWall = true;
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }
        public int Move { get; set; }
        public int Inject { get; set; }
        public bool IsShownOnWall { get; set; }
        public Guid TeamId { get; set; }
        public Guid CardId { get; set; }
    }

}

