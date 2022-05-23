using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations.PostgreSQL.Migrations
{
    public partial class openInNewTab : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "open_in_new_tab",
                table: "articles",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "open_in_new_tab",
                table: "articles");
        }
    }
}
