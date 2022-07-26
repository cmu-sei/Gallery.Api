/*
Copyright 2021 Carnegie Mellon University. All Rights Reserved. 
 Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.
*/

﻿using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Gallery.Api.Migrations.PostgreSQL.Migrations
{
    public partial class addUserArticles : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_teams_exhibits_exhibit_id",
                table: "teams");

            migrationBuilder.DropIndex(
                name: "IX_teams_exhibit_id",
                table: "teams");

            migrationBuilder.DropColumn(
                name: "exhibit_id",
                table: "teams");

            migrationBuilder.AddColumn<Guid>(
                name: "exhibit_entity_id",
                table: "teams",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "exhibit_teams",
                columns: table => new
                {
                    id = table.Column<Guid>(nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    date_created = table.Column<DateTime>(nullable: false),
                    date_modified = table.Column<DateTime>(nullable: true),
                    created_by = table.Column<Guid>(nullable: false),
                    modified_by = table.Column<Guid>(nullable: true),
                    exhibit_id = table.Column<Guid>(nullable: false),
                    team_id = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exhibit_teams", x => x.id);
                    table.ForeignKey(
                        name: "FK_exhibit_teams_exhibits_exhibit_id",
                        column: x => x.exhibit_id,
                        principalTable: "exhibits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_exhibit_teams_teams_team_id",
                        column: x => x.team_id,
                        principalTable: "teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "team_article_entity",
                columns: table => new
                {
                    id = table.Column<Guid>(nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    date_created = table.Column<DateTime>(nullable: false),
                    date_modified = table.Column<DateTime>(nullable: true),
                    created_by = table.Column<Guid>(nullable: false),
                    modified_by = table.Column<Guid>(nullable: true),
                    team_id = table.Column<Guid>(nullable: false),
                    article_id = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_team_article_entity", x => x.id);
                    table.ForeignKey(
                        name: "FK_team_article_entity_articles_article_id",
                        column: x => x.article_id,
                        principalTable: "articles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_team_article_entity_teams_team_id",
                        column: x => x.team_id,
                        principalTable: "teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_articles",
                columns: table => new
                {
                    id = table.Column<Guid>(nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    date_created = table.Column<DateTime>(nullable: false),
                    date_modified = table.Column<DateTime>(nullable: true),
                    created_by = table.Column<Guid>(nullable: false),
                    modified_by = table.Column<Guid>(nullable: true),
                    exhibit_id = table.Column<Guid>(nullable: false),
                    user_id = table.Column<Guid>(nullable: false),
                    article_id = table.Column<Guid>(nullable: false),
                    actual_date_posted = table.Column<DateTime>(nullable: false),
                    is_read = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_articles", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_articles_articles_article_id",
                        column: x => x.article_id,
                        principalTable: "articles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_articles_exhibits_exhibit_id",
                        column: x => x.exhibit_id,
                        principalTable: "exhibits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_articles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_teams_exhibit_entity_id",
                table: "teams",
                column: "exhibit_entity_id");

            migrationBuilder.CreateIndex(
                name: "IX_exhibit_teams_team_id",
                table: "exhibit_teams",
                column: "team_id");

            migrationBuilder.CreateIndex(
                name: "IX_exhibit_teams_exhibit_id_team_id",
                table: "exhibit_teams",
                columns: new[] { "exhibit_id", "team_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_team_article_entity_article_id",
                table: "team_article_entity",
                column: "article_id");

            migrationBuilder.CreateIndex(
                name: "IX_team_article_entity_team_id_article_id",
                table: "team_article_entity",
                columns: new[] { "team_id", "article_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_articles_article_id",
                table: "user_articles",
                column: "article_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_articles_user_id",
                table: "user_articles",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_articles_exhibit_id_user_id_article_id",
                table: "user_articles",
                columns: new[] { "exhibit_id", "user_id", "article_id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_teams_exhibits_exhibit_entity_id",
                table: "teams",
                column: "exhibit_entity_id",
                principalTable: "exhibits",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_teams_exhibits_exhibit_entity_id",
                table: "teams");

            migrationBuilder.DropTable(
                name: "exhibit_teams");

            migrationBuilder.DropTable(
                name: "team_article_entity");

            migrationBuilder.DropTable(
                name: "user_articles");

            migrationBuilder.DropIndex(
                name: "IX_teams_exhibit_entity_id",
                table: "teams");

            migrationBuilder.DropColumn(
                name: "exhibit_entity_id",
                table: "teams");

            migrationBuilder.AddColumn<Guid>(
                name: "exhibit_id",
                table: "teams",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_teams_exhibit_id",
                table: "teams",
                column: "exhibit_id");

            migrationBuilder.AddForeignKey(
                name: "FK_teams_exhibits_exhibit_id",
                table: "teams",
                column: "exhibit_id",
                principalTable: "exhibits",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
