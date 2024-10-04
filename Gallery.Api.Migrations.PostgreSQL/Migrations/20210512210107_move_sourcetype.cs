/*
Copyright 2021 Carnegie Mellon University. All Rights Reserved.
 Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.
*/

using Microsoft.EntityFrameworkCore.Migrations;

namespace Gallery.Api.Migrations.PostgreSQL.Migrations
{
    public partial class Move_sourcetype : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "order",
                table: "team_cards");

            migrationBuilder.DropColumn(
                name: "current_order",
                table: "exhibits");

            migrationBuilder.DropColumn(
                name: "order",
                table: "cards");

            migrationBuilder.DropColumn(
                name: "order",
                table: "articles");

            migrationBuilder.DropColumn(
                name: "source",
                table: "articles");

            migrationBuilder.DropColumn(
                name: "order",
                table: "article_tags");

            migrationBuilder.AddColumn<int>(
                name: "inject",
                table: "team_cards",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "move",
                table: "team_cards",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "current_move",
                table: "exhibits",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "inject",
                table: "cards",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "move",
                table: "cards",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "move",
                table: "articles",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "source_name",
                table: "articles",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "source_type",
                table: "articles",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "inject",
                table: "article_tags",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "move",
                table: "article_tags",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "inject",
                table: "team_cards");

            migrationBuilder.DropColumn(
                name: "move",
                table: "team_cards");

            migrationBuilder.DropColumn(
                name: "current_move",
                table: "exhibits");

            migrationBuilder.DropColumn(
                name: "inject",
                table: "cards");

            migrationBuilder.DropColumn(
                name: "move",
                table: "cards");

            migrationBuilder.DropColumn(
                name: "move",
                table: "articles");

            migrationBuilder.DropColumn(
                name: "source_name",
                table: "articles");

            migrationBuilder.DropColumn(
                name: "source_type",
                table: "articles");

            migrationBuilder.DropColumn(
                name: "inject",
                table: "article_tags");

            migrationBuilder.DropColumn(
                name: "move",
                table: "article_tags");

            migrationBuilder.AddColumn<int>(
                name: "order",
                table: "team_cards",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "current_order",
                table: "exhibits",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "order",
                table: "cards",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "order",
                table: "articles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "source",
                table: "articles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "order",
                table: "article_tags",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
