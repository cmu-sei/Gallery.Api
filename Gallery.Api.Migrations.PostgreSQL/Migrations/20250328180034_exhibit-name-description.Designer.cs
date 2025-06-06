/*
 Copyright 2025 Carnegie Mellon University. All Rights Reserved. 
 Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.
*/

﻿// <auto-generated />
using System;
using Gallery.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Gallery.Api.Migrations.PostgreSQL.Migrations
{
    [DbContext(typeof(GalleryDbContext))]
    [Migration("20250328180034_exhibit-name-description")]
    partial class exhibitnamedescription
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.8")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.HasPostgresExtension(modelBuilder, "uuid-ossp");
            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Gallery.Api.Data.Models.ArticleEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id")
                        .HasDefaultValueSql("uuid_generate_v4()");

                    b.Property<Guid?>("CardId")
                        .HasColumnType("uuid")
                        .HasColumnName("card_id");

                    b.Property<Guid>("CollectionId")
                        .HasColumnType("uuid")
                        .HasColumnName("collection_id");

                    b.Property<Guid>("CreatedBy")
                        .HasColumnType("uuid")
                        .HasColumnName("created_by");

                    b.Property<DateTime>("DateCreated")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("date_created");

                    b.Property<DateTime?>("DateModified")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("date_modified");

                    b.Property<DateTime>("DatePosted")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("date_posted");

                    b.Property<string>("Description")
                        .HasColumnType("text")
                        .HasColumnName("description");

                    b.Property<Guid?>("ExhibitId")
                        .HasColumnType("uuid")
                        .HasColumnName("exhibit_id");

                    b.Property<int>("Inject")
                        .HasColumnType("integer")
                        .HasColumnName("inject");

                    b.Property<Guid?>("ModifiedBy")
                        .HasColumnType("uuid")
                        .HasColumnName("modified_by");

                    b.Property<int>("Move")
                        .HasColumnType("integer")
                        .HasColumnName("move");

                    b.Property<string>("Name")
                        .HasColumnType("text")
                        .HasColumnName("name");

                    b.Property<bool>("OpenInNewTab")
                        .HasColumnType("boolean")
                        .HasColumnName("open_in_new_tab");

                    b.Property<string>("SourceName")
                        .HasColumnType("text")
                        .HasColumnName("source_name");

                    b.Property<int>("SourceType")
                        .HasColumnType("integer")
                        .HasColumnName("source_type");

                    b.Property<int>("Status")
                        .HasColumnType("integer")
                        .HasColumnName("status");

                    b.Property<string>("Summary")
                        .HasColumnType("text")
                        .HasColumnName("summary");

                    b.Property<string>("Url")
                        .HasColumnType("text")
                        .HasColumnName("url");

                    b.HasKey("Id");

                    b.HasIndex("CardId");

                    b.HasIndex("CollectionId");

                    b.HasIndex("ExhibitId");

                    b.ToTable("articles");
                });

            modelBuilder.Entity("Gallery.Api.Data.Models.CardEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id")
                        .HasDefaultValueSql("uuid_generate_v4()");

                    b.Property<Guid>("CollectionId")
                        .HasColumnType("uuid")
                        .HasColumnName("collection_id");

                    b.Property<Guid>("CreatedBy")
                        .HasColumnType("uuid")
                        .HasColumnName("created_by");

                    b.Property<DateTime>("DateCreated")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("date_created");

                    b.Property<DateTime?>("DateModified")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("date_modified");

                    b.Property<string>("Description")
                        .HasColumnType("text")
                        .HasColumnName("description");

                    b.Property<int>("Inject")
                        .HasColumnType("integer")
                        .HasColumnName("inject");

                    b.Property<Guid?>("ModifiedBy")
                        .HasColumnType("uuid")
                        .HasColumnName("modified_by");

                    b.Property<int>("Move")
                        .HasColumnType("integer")
                        .HasColumnName("move");

                    b.Property<string>("Name")
                        .HasColumnType("text")
                        .HasColumnName("name");

                    b.HasKey("Id");

                    b.HasIndex("CollectionId");

                    b.ToTable("cards");
                });

            modelBuilder.Entity("Gallery.Api.Data.Models.CollectionEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id")
                        .HasDefaultValueSql("uuid_generate_v4()");

                    b.Property<Guid>("CreatedBy")
                        .HasColumnType("uuid")
                        .HasColumnName("created_by");

                    b.Property<DateTime>("DateCreated")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("date_created");

                    b.Property<DateTime?>("DateModified")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("date_modified");

                    b.Property<string>("Description")
                        .HasColumnType("text")
                        .HasColumnName("description");

                    b.Property<Guid?>("ModifiedBy")
                        .HasColumnType("uuid")
                        .HasColumnName("modified_by");

                    b.Property<string>("Name")
                        .HasColumnType("text")
                        .HasColumnName("name");

                    b.HasKey("Id");

                    b.ToTable("collections");
                });

            modelBuilder.Entity("Gallery.Api.Data.Models.ExhibitEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id")
                        .HasDefaultValueSql("uuid_generate_v4()");

                    b.Property<Guid>("CollectionId")
                        .HasColumnType("uuid")
                        .HasColumnName("collection_id");

                    b.Property<Guid>("CreatedBy")
                        .HasColumnType("uuid")
                        .HasColumnName("created_by");

                    b.Property<int>("CurrentInject")
                        .HasColumnType("integer")
                        .HasColumnName("current_inject");

                    b.Property<int>("CurrentMove")
                        .HasColumnType("integer")
                        .HasColumnName("current_move");

                    b.Property<DateTime>("DateCreated")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("date_created");

                    b.Property<DateTime?>("DateModified")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("date_modified");

                    b.Property<string>("Description")
                        .HasColumnType("text")
                        .HasColumnName("description");

                    b.Property<Guid?>("ModifiedBy")
                        .HasColumnType("uuid")
                        .HasColumnName("modified_by");

                    b.Property<string>("Name")
                        .HasColumnType("text")
                        .HasColumnName("name");

                    b.Property<Guid?>("ScenarioId")
                        .HasColumnType("uuid")
                        .HasColumnName("scenario_id");

                    b.HasKey("Id");

                    b.HasIndex("CollectionId");

                    b.ToTable("exhibits");
                });

            modelBuilder.Entity("Gallery.Api.Data.Models.ExhibitTeamEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id")
                        .HasDefaultValueSql("uuid_generate_v4()");

                    b.Property<Guid>("CreatedBy")
                        .HasColumnType("uuid")
                        .HasColumnName("created_by");

                    b.Property<DateTime>("DateCreated")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("date_created");

                    b.Property<DateTime?>("DateModified")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("date_modified");

                    b.Property<Guid>("ExhibitId")
                        .HasColumnType("uuid")
                        .HasColumnName("exhibit_id");

                    b.Property<Guid?>("ModifiedBy")
                        .HasColumnType("uuid")
                        .HasColumnName("modified_by");

                    b.Property<Guid>("TeamId")
                        .HasColumnType("uuid")
                        .HasColumnName("team_id");

                    b.HasKey("Id");

                    b.HasIndex("TeamId");

                    b.HasIndex("ExhibitId", "TeamId")
                        .IsUnique();

                    b.ToTable("exhibit_teams");
                });

            modelBuilder.Entity("Gallery.Api.Data.Models.PermissionEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id")
                        .HasDefaultValueSql("uuid_generate_v4()");

                    b.Property<Guid>("CreatedBy")
                        .HasColumnType("uuid")
                        .HasColumnName("created_by");

                    b.Property<DateTime>("DateCreated")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("date_created");

                    b.Property<DateTime?>("DateModified")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("date_modified");

                    b.Property<string>("Description")
                        .HasColumnType("text")
                        .HasColumnName("description");

                    b.Property<string>("Key")
                        .HasColumnType("text")
                        .HasColumnName("key");

                    b.Property<Guid?>("ModifiedBy")
                        .HasColumnType("uuid")
                        .HasColumnName("modified_by");

                    b.Property<bool>("ReadOnly")
                        .HasColumnType("boolean")
                        .HasColumnName("read_only");

                    b.Property<string>("Value")
                        .HasColumnType("text")
                        .HasColumnName("value");

                    b.HasKey("Id");

                    b.HasIndex("Key", "Value")
                        .IsUnique();

                    b.ToTable("permissions");
                });

            modelBuilder.Entity("Gallery.Api.Data.Models.TeamArticleEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id")
                        .HasDefaultValueSql("uuid_generate_v4()");

                    b.Property<Guid>("ArticleId")
                        .HasColumnType("uuid")
                        .HasColumnName("article_id");

                    b.Property<Guid>("CreatedBy")
                        .HasColumnType("uuid")
                        .HasColumnName("created_by");

                    b.Property<DateTime>("DateCreated")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("date_created");

                    b.Property<DateTime?>("DateModified")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("date_modified");

                    b.Property<Guid>("ExhibitId")
                        .HasColumnType("uuid")
                        .HasColumnName("exhibit_id");

                    b.Property<Guid?>("ModifiedBy")
                        .HasColumnType("uuid")
                        .HasColumnName("modified_by");

                    b.Property<Guid>("TeamId")
                        .HasColumnType("uuid")
                        .HasColumnName("team_id");

                    b.HasKey("Id");

                    b.HasIndex("ArticleId");

                    b.HasIndex("TeamId");

                    b.HasIndex("ExhibitId", "TeamId", "ArticleId")
                        .IsUnique();

                    b.ToTable("team_articles");
                });

            modelBuilder.Entity("Gallery.Api.Data.Models.TeamCardEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id")
                        .HasDefaultValueSql("uuid_generate_v4()");

                    b.Property<bool>("CanPostArticles")
                        .HasColumnType("boolean")
                        .HasColumnName("can_post_articles");

                    b.Property<Guid>("CardId")
                        .HasColumnType("uuid")
                        .HasColumnName("card_id");

                    b.Property<Guid>("CreatedBy")
                        .HasColumnType("uuid")
                        .HasColumnName("created_by");

                    b.Property<DateTime>("DateCreated")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("date_created");

                    b.Property<DateTime?>("DateModified")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("date_modified");

                    b.Property<int>("Inject")
                        .HasColumnType("integer")
                        .HasColumnName("inject");

                    b.Property<bool>("IsShownOnWall")
                        .HasColumnType("boolean")
                        .HasColumnName("is_shown_on_wall");

                    b.Property<Guid?>("ModifiedBy")
                        .HasColumnType("uuid")
                        .HasColumnName("modified_by");

                    b.Property<int>("Move")
                        .HasColumnType("integer")
                        .HasColumnName("move");

                    b.Property<Guid>("TeamId")
                        .HasColumnType("uuid")
                        .HasColumnName("team_id");

                    b.HasKey("Id");

                    b.HasIndex("CardId");

                    b.HasIndex("TeamId", "CardId")
                        .IsUnique();

                    b.ToTable("team_cards");
                });

            modelBuilder.Entity("Gallery.Api.Data.Models.TeamEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id")
                        .HasDefaultValueSql("uuid_generate_v4()");

                    b.Property<Guid>("CreatedBy")
                        .HasColumnType("uuid")
                        .HasColumnName("created_by");

                    b.Property<DateTime>("DateCreated")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("date_created");

                    b.Property<DateTime?>("DateModified")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("date_modified");

                    b.Property<string>("Email")
                        .HasColumnType("text")
                        .HasColumnName("email");

                    b.Property<Guid?>("ExhibitId")
                        .HasColumnType("uuid")
                        .HasColumnName("exhibit_id");

                    b.Property<Guid?>("ModifiedBy")
                        .HasColumnType("uuid")
                        .HasColumnName("modified_by");

                    b.Property<string>("Name")
                        .HasColumnType("text")
                        .HasColumnName("name");

                    b.Property<string>("ShortName")
                        .HasColumnType("text")
                        .HasColumnName("short_name");

                    b.HasKey("Id");

                    b.HasIndex("ExhibitId");

                    b.HasIndex("Id")
                        .IsUnique();

                    b.ToTable("teams");
                });

            modelBuilder.Entity("Gallery.Api.Data.Models.TeamUserEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id")
                        .HasDefaultValueSql("uuid_generate_v4()");

                    b.Property<bool>("IsObserver")
                        .HasColumnType("boolean")
                        .HasColumnName("is_observer");

                    b.Property<Guid>("TeamId")
                        .HasColumnType("uuid")
                        .HasColumnName("team_id");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid")
                        .HasColumnName("user_id");

                    b.HasKey("Id");

                    b.HasIndex("TeamId");

                    b.HasIndex("UserId", "TeamId")
                        .IsUnique();

                    b.ToTable("team_users");
                });

            modelBuilder.Entity("Gallery.Api.Data.Models.UserArticleEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id")
                        .HasDefaultValueSql("uuid_generate_v4()");

                    b.Property<DateTime>("ActualDatePosted")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("actual_date_posted");

                    b.Property<Guid>("ArticleId")
                        .HasColumnType("uuid")
                        .HasColumnName("article_id");

                    b.Property<Guid>("CreatedBy")
                        .HasColumnType("uuid")
                        .HasColumnName("created_by");

                    b.Property<DateTime>("DateCreated")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("date_created");

                    b.Property<DateTime?>("DateModified")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("date_modified");

                    b.Property<Guid>("ExhibitId")
                        .HasColumnType("uuid")
                        .HasColumnName("exhibit_id");

                    b.Property<bool>("IsRead")
                        .HasColumnType("boolean")
                        .HasColumnName("is_read");

                    b.Property<Guid?>("ModifiedBy")
                        .HasColumnType("uuid")
                        .HasColumnName("modified_by");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid")
                        .HasColumnName("user_id");

                    b.HasKey("Id");

                    b.HasIndex("ArticleId");

                    b.HasIndex("UserId");

                    b.HasIndex("ExhibitId", "UserId", "ArticleId")
                        .IsUnique();

                    b.ToTable("user_articles");
                });

            modelBuilder.Entity("Gallery.Api.Data.Models.UserEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id")
                        .HasDefaultValueSql("uuid_generate_v4()");

                    b.Property<Guid>("CreatedBy")
                        .HasColumnType("uuid")
                        .HasColumnName("created_by");

                    b.Property<DateTime>("DateCreated")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("date_created");

                    b.Property<DateTime?>("DateModified")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("date_modified");

                    b.Property<string>("Email")
                        .HasColumnType("text")
                        .HasColumnName("email");

                    b.Property<Guid?>("ModifiedBy")
                        .HasColumnType("uuid")
                        .HasColumnName("modified_by");

                    b.Property<string>("Name")
                        .HasColumnType("text")
                        .HasColumnName("name");

                    b.HasKey("Id");

                    b.HasIndex("Id")
                        .IsUnique();

                    b.ToTable("users");
                });

            modelBuilder.Entity("Gallery.Api.Data.Models.UserPermissionEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id")
                        .HasDefaultValueSql("uuid_generate_v4()");

                    b.Property<Guid>("PermissionId")
                        .HasColumnType("uuid")
                        .HasColumnName("permission_id");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid")
                        .HasColumnName("user_id");

                    b.HasKey("Id");

                    b.HasIndex("PermissionId");

                    b.HasIndex("UserId", "PermissionId")
                        .IsUnique();

                    b.ToTable("user_permissions");
                });

            modelBuilder.Entity("Gallery.Api.Data.Models.ArticleEntity", b =>
                {
                    b.HasOne("Gallery.Api.Data.Models.CardEntity", "Card")
                        .WithMany()
                        .HasForeignKey("CardId");

                    b.HasOne("Gallery.Api.Data.Models.CollectionEntity", "Collection")
                        .WithMany()
                        .HasForeignKey("CollectionId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Gallery.Api.Data.Models.CardEntity", "Exhibit")
                        .WithMany()
                        .HasForeignKey("ExhibitId");

                    b.Navigation("Card");

                    b.Navigation("Collection");

                    b.Navigation("Exhibit");
                });

            modelBuilder.Entity("Gallery.Api.Data.Models.CardEntity", b =>
                {
                    b.HasOne("Gallery.Api.Data.Models.CollectionEntity", "Collection")
                        .WithMany()
                        .HasForeignKey("CollectionId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Collection");
                });

            modelBuilder.Entity("Gallery.Api.Data.Models.ExhibitEntity", b =>
                {
                    b.HasOne("Gallery.Api.Data.Models.CollectionEntity", "Collection")
                        .WithMany()
                        .HasForeignKey("CollectionId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Collection");
                });

            modelBuilder.Entity("Gallery.Api.Data.Models.ExhibitTeamEntity", b =>
                {
                    b.HasOne("Gallery.Api.Data.Models.ExhibitEntity", "Exhibit")
                        .WithMany()
                        .HasForeignKey("ExhibitId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Gallery.Api.Data.Models.TeamEntity", "Team")
                        .WithMany()
                        .HasForeignKey("TeamId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Exhibit");

                    b.Navigation("Team");
                });

            modelBuilder.Entity("Gallery.Api.Data.Models.TeamArticleEntity", b =>
                {
                    b.HasOne("Gallery.Api.Data.Models.ArticleEntity", "Article")
                        .WithMany("TeamArticles")
                        .HasForeignKey("ArticleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Gallery.Api.Data.Models.ExhibitEntity", "Exhibit")
                        .WithMany()
                        .HasForeignKey("ExhibitId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Gallery.Api.Data.Models.TeamEntity", "Team")
                        .WithMany("TeamArticles")
                        .HasForeignKey("TeamId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Article");

                    b.Navigation("Exhibit");

                    b.Navigation("Team");
                });

            modelBuilder.Entity("Gallery.Api.Data.Models.TeamCardEntity", b =>
                {
                    b.HasOne("Gallery.Api.Data.Models.CardEntity", "Card")
                        .WithMany()
                        .HasForeignKey("CardId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Gallery.Api.Data.Models.TeamEntity", "Team")
                        .WithMany()
                        .HasForeignKey("TeamId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Card");

                    b.Navigation("Team");
                });

            modelBuilder.Entity("Gallery.Api.Data.Models.TeamEntity", b =>
                {
                    b.HasOne("Gallery.Api.Data.Models.ExhibitEntity", "Exhibit")
                        .WithMany("Teams")
                        .HasForeignKey("ExhibitId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.Navigation("Exhibit");
                });

            modelBuilder.Entity("Gallery.Api.Data.Models.TeamUserEntity", b =>
                {
                    b.HasOne("Gallery.Api.Data.Models.TeamEntity", "Team")
                        .WithMany("TeamUsers")
                        .HasForeignKey("TeamId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Gallery.Api.Data.Models.UserEntity", "User")
                        .WithMany("TeamUsers")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Team");

                    b.Navigation("User");
                });

            modelBuilder.Entity("Gallery.Api.Data.Models.UserArticleEntity", b =>
                {
                    b.HasOne("Gallery.Api.Data.Models.ArticleEntity", "Article")
                        .WithMany()
                        .HasForeignKey("ArticleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Gallery.Api.Data.Models.ExhibitEntity", "Exhibit")
                        .WithMany()
                        .HasForeignKey("ExhibitId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Gallery.Api.Data.Models.UserEntity", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Article");

                    b.Navigation("Exhibit");

                    b.Navigation("User");
                });

            modelBuilder.Entity("Gallery.Api.Data.Models.UserPermissionEntity", b =>
                {
                    b.HasOne("Gallery.Api.Data.Models.PermissionEntity", "Permission")
                        .WithMany("UserPermissions")
                        .HasForeignKey("PermissionId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Gallery.Api.Data.Models.UserEntity", "User")
                        .WithMany("UserPermissions")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Permission");

                    b.Navigation("User");
                });

            modelBuilder.Entity("Gallery.Api.Data.Models.ArticleEntity", b =>
                {
                    b.Navigation("TeamArticles");
                });

            modelBuilder.Entity("Gallery.Api.Data.Models.ExhibitEntity", b =>
                {
                    b.Navigation("Teams");
                });

            modelBuilder.Entity("Gallery.Api.Data.Models.PermissionEntity", b =>
                {
                    b.Navigation("UserPermissions");
                });

            modelBuilder.Entity("Gallery.Api.Data.Models.TeamEntity", b =>
                {
                    b.Navigation("TeamArticles");

                    b.Navigation("TeamUsers");
                });

            modelBuilder.Entity("Gallery.Api.Data.Models.UserEntity", b =>
                {
                    b.Navigation("TeamUsers");

                    b.Navigation("UserPermissions");
                });
#pragma warning restore 612, 618
        }
    }
}
