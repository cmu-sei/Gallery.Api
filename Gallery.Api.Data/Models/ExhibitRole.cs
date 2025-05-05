// Copyright 2024 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Gallery.Api.Data.Enumerations;

namespace Gallery.Api.Data.Models;

public class ExhibitRoleEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    public string Name { get; set; }

    public string Description { get; set; }

    public bool AllPermissions { get; set; }

    public List<ExhibitPermission> Permissions { get; set; }
}

public static class ExhibitRoleDefaults
{
    public static Guid ExhibitCreatorRoleId = new("1a3f26cd-9d99-4b98-b914-12931e786198");
    public static Guid ExhibitReadOnlyRoleId = new("39aa296e-05ba-4fb0-8d74-c92cf3354c6f");
    public static Guid ExhibitMemberRoleId = new("f870d8ee-7332-4f7f-8ee0-63bd07cfd7e4");
}

public class ExhibitRoleConfiguration : IEntityTypeConfiguration<ExhibitRoleEntity>
{
    public void Configure(EntityTypeBuilder<ExhibitRoleEntity> builder)
    {
        builder.HasData(
            new ExhibitRoleEntity
            {
                Id = ExhibitRoleDefaults.ExhibitCreatorRoleId,
                Name = "Manager",
                AllPermissions = true,
                Permissions = [],
                Description = "Can perform all actions on the Exhibit"
            },
            new ExhibitRoleEntity
            {
                Id = ExhibitRoleDefaults.ExhibitReadOnlyRoleId,
                Name = "Observer",
                AllPermissions = false,
                Permissions = [ExhibitPermission.ViewExhibit],
                Description = "Has read only access to the Exhibit"
            },
            new ExhibitRoleEntity
            {
                Id = ExhibitRoleDefaults.ExhibitMemberRoleId,
                Name = "Member",
                AllPermissions = false,
                Permissions = [
                    ExhibitPermission.ViewExhibit,
                    ExhibitPermission.EditExhibit
                ],
                Description = "Has read only access to the Exhibit"
            }
        );
    }
}
