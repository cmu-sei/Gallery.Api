/*
Copyright 2021 Carnegie Mellon University. All Rights Reserved.
 Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.
*/

using Microsoft.EntityFrameworkCore.Migrations;

namespace Gallery.Api.Migrations.PostgreSQL.Migrations
{
    public partial class Article_fix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_articles_collections_card_id",
                table: "articles");

            migrationBuilder.AddForeignKey(
                name: "FK_articles_cards_card_id",
                table: "articles",
                column: "card_id",
                principalTable: "cards",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_articles_cards_card_id",
                table: "articles");

            migrationBuilder.AddForeignKey(
                name: "FK_articles_collections_card_id",
                table: "articles",
                column: "card_id",
                principalTable: "collections",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
