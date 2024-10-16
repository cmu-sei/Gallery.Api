/*
Copyright 2021 Carnegie Mellon University. All Rights Reserved.
 Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.
*/

using Microsoft.EntityFrameworkCore.Migrations;

namespace Gallery.Api.Migrations.PostgreSQL.Migrations
{
    public partial class Uniqueteamcards : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "delete from team_cards where id in (select S2.id FROM team_cards S1, team_cards S2 WHERE S1.date_created < S2.date_created AND S1.team_id = S2.team_id AND S1.card_id = S2.card_id);");

            migrationBuilder.DropIndex(
                name: "IX_team_cards_team_id",
                table: "team_cards");

            migrationBuilder.CreateIndex(
                name: "IX_team_cards_team_id_card_id",
                table: "team_cards",
                columns: new[] { "team_id", "card_id" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_team_cards_team_id_card_id",
                table: "team_cards");

            migrationBuilder.CreateIndex(
                name: "IX_team_cards_team_id",
                table: "team_cards",
                column: "team_id");
        }
    }
}
