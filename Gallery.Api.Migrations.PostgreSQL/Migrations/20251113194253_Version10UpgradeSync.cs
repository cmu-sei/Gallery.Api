using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gallery.Api.Migrations.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class Version10UpgradeSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "exhibit_roles",
                keyColumn: "id",
                keyValue: new Guid("f870d8ee-7332-4f7f-8ee0-63bd07cfd7e4"),
                column: "description",
                value: "Can edit the Exhibit");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "exhibit_roles",
                keyColumn: "id",
                keyValue: new Guid("f870d8ee-7332-4f7f-8ee0-63bd07cfd7e4"),
                column: "description",
                value: "Has read only access to the Exhibit");
        }
    }
}
