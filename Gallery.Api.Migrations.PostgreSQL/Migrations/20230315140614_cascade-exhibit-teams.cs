using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gallery.Api.Migrations.PostgreSQL.Migrations
{
    public partial class cascadeexhibitteams : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_teams_exhibits_exhibit_id",
                table: "teams");

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

            migrationBuilder.AddForeignKey(
                name: "FK_teams_exhibits_exhibit_id",
                table: "teams",
                column: "exhibit_id",
                principalTable: "exhibits",
                principalColumn: "id");
        }
    }
}
