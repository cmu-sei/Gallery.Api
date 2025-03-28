/*
 Copyright 2025 Carnegie Mellon University. All Rights Reserved. 
 Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.
*/

﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gallery.Api.Migrations.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class exhibitnamedescription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "exhibits",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "exhibits",
                type: "text",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE exhibits
                SET name = to_char(exhibits.date_created,'MM/DD/YYYY HH24:MI') || ' - ' || users.name
                from users where users.id = exhibits.created_by
            ");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "description",
                table: "exhibits");

            migrationBuilder.DropColumn(
                name: "name",
                table: "exhibits");
        }
    }
}
