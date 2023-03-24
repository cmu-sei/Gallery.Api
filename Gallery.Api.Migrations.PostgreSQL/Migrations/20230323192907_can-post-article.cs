using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gallery.Api.Migrations.PostgreSQL.Migrations
{
    public partial class canpostarticle : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "can_post_articles",
                table: "team_cards",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "can_post_articles",
                table: "team_cards");
        }
    }
}
