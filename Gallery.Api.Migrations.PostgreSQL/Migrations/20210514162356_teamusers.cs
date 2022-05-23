using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Gallery.Api.Migrations.PostgreSQL.Migrations
{
    public partial class teamusers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "exhibit_id",
                table: "teams",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<bool>(
                name: "is_shown_on_wall",
                table: "team_cards",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "team_users",
                columns: table => new
                {
                    id = table.Column<Guid>(nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    user_id = table.Column<Guid>(nullable: false),
                    team_id = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_team_users", x => x.id);
                    table.ForeignKey(
                        name: "FK_team_users_teams_team_id",
                        column: x => x.team_id,
                        principalTable: "teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_team_users_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_teams_exhibit_id",
                table: "teams",
                column: "exhibit_id");

            migrationBuilder.CreateIndex(
                name: "IX_team_users_team_id",
                table: "team_users",
                column: "team_id");

            migrationBuilder.CreateIndex(
                name: "IX_team_users_user_id_team_id",
                table: "team_users",
                columns: new[] { "user_id", "team_id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_teams_exhibits_exhibit_id",
                table: "teams",
                column: "exhibit_id",
                principalTable: "exhibits",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_teams_exhibits_exhibit_id",
                table: "teams");

            migrationBuilder.DropTable(
                name: "team_users");

            migrationBuilder.DropIndex(
                name: "IX_teams_exhibit_id",
                table: "teams");

            migrationBuilder.DropColumn(
                name: "exhibit_id",
                table: "teams");

            migrationBuilder.DropColumn(
                name: "is_shown_on_wall",
                table: "team_cards");
        }
    }
}
