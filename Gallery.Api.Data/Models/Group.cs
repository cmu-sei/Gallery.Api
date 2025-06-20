// Copyright 2024 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gallery.Api.Data.Models;

public class GroupEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    public string Name { get; set; }
    public string Description { get; set; }

    public virtual ICollection<GroupMembershipEntity> Memberships { get; set; } = new List<GroupMembershipEntity>();
    public virtual ICollection<CollectionMembershipEntity> CollectionMemberships { get; set; } = new List<CollectionMembershipEntity>();
    public virtual ICollection<ExhibitMembershipEntity> ExhibitMemberships { get; set; } = new List<ExhibitMembershipEntity>();
}
