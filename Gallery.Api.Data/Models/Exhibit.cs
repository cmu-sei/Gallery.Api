// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace Gallery.Api.Data.Models
{
    public class ExhibitEntity : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }
        public int CurrentMove { get; set; }
        public int CurrentInject { get; set; }
        public Guid CollectionId { get; set; }
        public Guid? ScenarioId { get; set; }
        public CollectionEntity Collection { get; set; }
        public ICollection<TeamEntity> Teams { get; set; } = new List<TeamEntity>();
    }
}
