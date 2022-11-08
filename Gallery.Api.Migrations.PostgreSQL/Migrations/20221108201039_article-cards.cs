using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gallery.Api.Migrations.PostgreSQL.Migrations
{
    public partial class articlecards : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "article_cards",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    card_id = table.Column<Guid>(type: "uuid", nullable: false),
                    article_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date_created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    date_modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    modified_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_article_cards", x => x.id);
                    table.ForeignKey(
                        name: "FK_article_cards_articles_article_id",
                        column: x => x.article_id,
                        principalTable: "articles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_article_cards_cards_card_id",
                        column: x => x.card_id,
                        principalTable: "cards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_article_cards_article_id",
                table: "article_cards",
                column: "article_id");

            migrationBuilder.CreateIndex(
                name: "IX_article_cards_card_id_article_id",
                table: "article_cards",
                columns: new[] { "card_id", "article_id" },
                unique: true);

            migrationBuilder.Sql("INSERT INTO article_cards (card_id, article_id, date_created, created_by) SELECT card_id, id, date_created, created_by FROM articles WHERE card_id is not null");

        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "article_cards");
        }
    }
}
