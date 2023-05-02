using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gallery.Api.Migrations.PostgreSQL.Migrations
{
    public partial class addteamemail : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "email",
                table: "teams",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "email",
                table: "teams");
        }
    }
}
