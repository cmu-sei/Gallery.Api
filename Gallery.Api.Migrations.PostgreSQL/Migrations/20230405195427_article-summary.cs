using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gallery.Api.Migrations.PostgreSQL.Migrations
{
    public partial class articlesummary : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "summary",
                table: "articles",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "summary",
                table: "articles");
        }
    }
}
