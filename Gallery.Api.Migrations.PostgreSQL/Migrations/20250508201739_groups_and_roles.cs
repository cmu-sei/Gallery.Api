/*
 Copyright 2025 Carnegie Mellon University. All Rights Reserved. 
 Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.
*/

﻿using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Gallery.Api.Migrations.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class groups_and_roles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "role_id",
                table: "users",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "collection_roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    name = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    all_permissions = table.Column<bool>(type: "boolean", nullable: false),
                    permissions = table.Column<int[]>(type: "integer[]", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_collection_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "exhibit_roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    name = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    all_permissions = table.Column<bool>(type: "boolean", nullable: false),
                    permissions = table.Column<int[]>(type: "integer[]", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exhibit_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "groups",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    name = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_groups", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "system_roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    name = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    all_permissions = table.Column<bool>(type: "boolean", nullable: false),
                    immutable = table.Column<bool>(type: "boolean", nullable: false),
                    permissions = table.Column<int[]>(type: "integer[]", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_system_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "collection_memberships",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    collection_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValue: new Guid("f870d8ee-7332-4f7f-8ee0-63bd07cfd7e4"))
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_collection_memberships", x => x.id);
                    table.ForeignKey(
                        name: "FK_collection_memberships_collection_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "collection_roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_collection_memberships_collections_collection_id",
                        column: x => x.collection_id,
                        principalTable: "collections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_collection_memberships_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_collection_memberships_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "exhibit_memberships",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    exhibit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValue: new Guid("f870d8ee-7332-4f7f-8ee0-63bd07cfd7e4"))
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exhibit_memberships", x => x.id);
                    table.ForeignKey(
                        name: "FK_exhibit_memberships_exhibit_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "exhibit_roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_exhibit_memberships_exhibits_exhibit_id",
                        column: x => x.exhibit_id,
                        principalTable: "exhibits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_exhibit_memberships_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_exhibit_memberships_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "group_memberships",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_group_memberships", x => x.id);
                    table.ForeignKey(
                        name: "FK_group_memberships_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_group_memberships_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "collection_roles",
                columns: new[] { "id", "all_permissions", "description", "name", "permissions" },
                values: new object[,]
                {
                    { new Guid("1a3f26cd-9d99-4b98-b914-12931e786198"), true, "Can perform all actions on the Collection", "Manager", new int[0] },
                    { new Guid("39aa296e-05ba-4fb0-8d74-c92cf3354c6f"), false, "Has read only access to the Collection", "Observer", new[] { 0 } },
                    { new Guid("f870d8ee-7332-4f7f-8ee0-63bd07cfd7e4"), false, "Has read only access to the Collection", "Member", new[] { 0, 1 } }
                });

            migrationBuilder.InsertData(
                table: "exhibit_roles",
                columns: new[] { "id", "all_permissions", "description", "name", "permissions" },
                values: new object[,]
                {
                    { new Guid("1a3f26cd-9d99-4b98-b914-12931e786198"), true, "Can perform all actions on the Exhibit", "Manager", new int[0] },
                    { new Guid("39aa296e-05ba-4fb0-8d74-c92cf3354c6f"), false, "Has read only access to the Exhibit", "Observer", new[] { 0 } },
                    { new Guid("f870d8ee-7332-4f7f-8ee0-63bd07cfd7e4"), false, "Has read only access to the Exhibit", "Member", new[] { 0, 1 } }
                });

            migrationBuilder.InsertData(
                table: "system_roles",
                columns: new[] { "id", "all_permissions", "description", "immutable", "name", "permissions" },
                values: new object[,]
                {
                    { new Guid("1da3027e-725d-4753-9455-a836ed9bdb1e"), false, "Can View all Exhibit Templates and Exhibits, but cannot make any changes.", false, "Observer", new[] { 1, 5, 8, 10, 12 } },
                    { new Guid("d80b73c3-95d7-4468-8650-c62bbd082507"), false, "Can create and manage their own Exhibit Templates and Exhibits.", false, "Content Developer", new[] { 0, 4, 7 } },
                    { new Guid("f35e8fff-f996-4cba-b303-3ba515ad8d2f"), true, "Can perform all actions", true, "Administrator", new int[0] }
                });

            migrationBuilder.CreateIndex(
                name: "IX_users_role_id",
                table: "users",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_collection_memberships_collection_id_user_id_group_id",
                table: "collection_memberships",
                columns: new[] { "collection_id", "user_id", "group_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_collection_memberships_group_id",
                table: "collection_memberships",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "IX_collection_memberships_role_id",
                table: "collection_memberships",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_collection_memberships_user_id",
                table: "collection_memberships",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_exhibit_memberships_exhibit_id_user_id_group_id",
                table: "exhibit_memberships",
                columns: new[] { "exhibit_id", "user_id", "group_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_exhibit_memberships_group_id",
                table: "exhibit_memberships",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "IX_exhibit_memberships_role_id",
                table: "exhibit_memberships",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_exhibit_memberships_user_id",
                table: "exhibit_memberships",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_group_memberships_group_id_user_id",
                table: "group_memberships",
                columns: new[] { "group_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_group_memberships_user_id",
                table: "group_memberships",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_system_roles_name",
                table: "system_roles",
                column: "name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_users_system_roles_role_id",
                table: "users",
                column: "role_id",
                principalTable: "system_roles",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_users_system_roles_role_id",
                table: "users");

            migrationBuilder.DropTable(
                name: "collection_memberships");

            migrationBuilder.DropTable(
                name: "exhibit_memberships");

            migrationBuilder.DropTable(
                name: "group_memberships");

            migrationBuilder.DropTable(
                name: "system_roles");

            migrationBuilder.DropTable(
                name: "collection_roles");

            migrationBuilder.DropTable(
                name: "exhibit_roles");

            migrationBuilder.DropTable(
                name: "groups");

            migrationBuilder.DropIndex(
                name: "IX_users_role_id",
                table: "users");

            migrationBuilder.DropColumn(
                name: "role_id",
                table: "users");
        }
    }
}
