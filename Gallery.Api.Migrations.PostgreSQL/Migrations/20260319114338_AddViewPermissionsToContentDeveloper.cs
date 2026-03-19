using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gallery.Api.Migrations.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class AddViewPermissionsToContentDeveloper : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "system_roles",
                keyColumn: "id",
                keyValue: new Guid("d80b73c3-95d7-4468-8650-c62bbd082507"),
                column: "permissions",
                value: new[] { 1, 5, 10, 12, 0, 4, 7 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "system_roles",
                keyColumn: "id",
                keyValue: new Guid("d80b73c3-95d7-4468-8650-c62bbd082507"),
                column: "permissions",
                value: new[] { 0, 4, 7 });
        }
    }
}
