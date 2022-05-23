using Microsoft.EntityFrameworkCore.Migrations;

namespace Gallery.Api.Migrations.PostgreSQL.Migrations
{
    public partial class addUserEmail : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "email",
                table: "users",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "email",
                table: "users");
        }
    }
}
