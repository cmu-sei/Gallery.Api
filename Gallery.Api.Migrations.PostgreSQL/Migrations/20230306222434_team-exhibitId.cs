/*
 Copyright 2023 Carnegie Mellon University. All Rights Reserved.
 Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.
*/

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gallery.Api.Migrations.PostgreSQL.Migrations
{
    public partial class TeamexhibitId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_teams_exhibits_exhibit_entity_id",
                table: "teams");

            migrationBuilder.RenameColumn(
                name: "exhibit_entity_id",
                table: "teams",
                newName: "exhibit_id");

            migrationBuilder.RenameIndex(
                name: "IX_teams_exhibit_entity_id",
                table: "teams",
                newName: "IX_teams_exhibit_id");

            migrationBuilder.CreateIndex(
                name: "IX_teams_id",
                table: "teams",
                column: "id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_teams_exhibits_exhibit_id",
                table: "teams",
                column: "exhibit_id",
                principalTable: "exhibits",
                principalColumn: "id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_teams_exhibits_exhibit_id",
                table: "teams");

            migrationBuilder.DropIndex(
                name: "IX_teams_id",
                table: "teams");

            migrationBuilder.RenameColumn(
                name: "exhibit_id",
                table: "teams",
                newName: "exhibit_entity_id");

            migrationBuilder.RenameIndex(
                name: "IX_teams_exhibit_id",
                table: "teams",
                newName: "IX_teams_exhibit_entity_id");

            migrationBuilder.AddForeignKey(
                name: "FK_teams_exhibits_exhibit_entity_id",
                table: "teams",
                column: "exhibit_entity_id",
                principalTable: "exhibits",
                principalColumn: "id");
        }
    }
}
