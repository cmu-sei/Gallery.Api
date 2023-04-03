﻿using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gallery.Api.Migrations.PostgreSQL.Migrations
{
    public partial class articleexhibitId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "exhibit_id",
                table: "articles",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_articles_exhibit_id",
                table: "articles",
                column: "exhibit_id");

            migrationBuilder.AddForeignKey(
                name: "FK_articles_exhibits_exhibit_id",
                table: "articles",
                column: "exhibit_id",
                principalTable: "exhibits",
                principalColumn: "id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_articles_exhibits_exhibit_id",
                table: "articles");

            migrationBuilder.DropIndex(
                name: "IX_articles_exhibit_id",
                table: "articles");

            migrationBuilder.DropColumn(
                name: "exhibit_id",
                table: "articles");
        }
    }
}
