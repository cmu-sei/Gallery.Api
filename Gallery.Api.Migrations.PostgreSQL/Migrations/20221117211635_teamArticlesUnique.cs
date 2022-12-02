/*
 Copyright 2022 Carnegie Mellon University. All Rights Reserved. 
 Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.
*/

﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gallery.Api.Migrations.PostgreSQL.Migrations
{
    public partial class teamArticlesUnique : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_team_articles_exhibit_id",
                table: "team_articles");

            migrationBuilder.CreateIndex(
                name: "IX_team_articles_exhibit_id_team_id_article_id",
                table: "team_articles",
                columns: new[] { "exhibit_id", "team_id", "article_id" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_team_articles_exhibit_id_team_id_article_id",
                table: "team_articles");

            migrationBuilder.CreateIndex(
                name: "IX_team_articles_exhibit_id",
                table: "team_articles",
                column: "exhibit_id");
        }
    }
}
