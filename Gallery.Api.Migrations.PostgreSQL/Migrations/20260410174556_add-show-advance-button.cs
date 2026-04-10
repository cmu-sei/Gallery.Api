using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gallery.Api.Migrations.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class addshowadvancebutton : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "show_advance_button",
                table: "exhibits",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Set existing exhibits to show the advance button
            migrationBuilder.Sql("UPDATE exhibits SET show_advance_button = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "show_advance_button",
                table: "exhibits");
        }
    }
}
