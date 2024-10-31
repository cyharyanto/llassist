﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using llassist.ApiService.Repositories;

#nullable disable

namespace llassist.ApiService.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20241007043522_ConvertArticlesWeakEntity")]
    partial class ConvertArticlesWeakEntity
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.8")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("llassist.Common.Models.Article", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("text");

                    b.Property<string>("Abstract")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Authors")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("DOI")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Link")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("ProjectId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("Year")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("ProjectId");

                    b.ToTable("Articles", (string)null);
                });

            modelBuilder.Entity("llassist.Common.Models.ArticleKeySemantic", b =>
                {
                    b.Property<string>("ArticleId")
                        .HasColumnType("text");

                    b.HasKey("ArticleId");

                    b.ToTable("ArticleKeySemantics", (string)null);
                });

            modelBuilder.Entity("llassist.Common.Models.ArticleRelevance", b =>
                {
                    b.Property<string>("ArticleId")
                        .HasColumnType("text");

                    b.Property<string>("EstimateRelevanceJobId")
                        .HasColumnType("text");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<bool>("MustRead")
                        .HasColumnType("boolean");

                    b.HasKey("ArticleId", "EstimateRelevanceJobId");

                    b.ToTable("ArticleRelevances", (string)null);
                });

            modelBuilder.Entity("llassist.Common.Models.EstimateRelevanceJob", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("text");

                    b.Property<int>("CompletedArticles")
                        .HasColumnType("integer");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("ModelName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("ProjectId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("TotalArticles")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("ProjectId");

                    b.ToTable("EstimateRelevanceJobs", (string)null);
                });

            modelBuilder.Entity("llassist.Common.Models.Project", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("text");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("Projects", (string)null);
                });

            modelBuilder.Entity("llassist.Common.Models.ProjectDefinition", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("text");

                    b.Property<string>("Definition")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("ProjectId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("ProjectId");

                    b.ToTable("ProjectDefinitions", (string)null);
                });

            modelBuilder.Entity("llassist.Common.Models.QuestionDefinition", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("text");

                    b.Property<string>("Definition")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("ResearchQuestionId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("ResearchQuestionId");

                    b.ToTable("QuestionDefinitions", (string)null);
                });

            modelBuilder.Entity("llassist.Common.Models.ResearchQuestion", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("text");

                    b.Property<string>("ProjectId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("QuestionText")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("ProjectId");

                    b.ToTable("ResearchQuestions", (string)null);
                });

            modelBuilder.Entity("llassist.Common.Models.Snapshot", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("text");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("EntityId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("EntityType")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("EstimateRelevanceJobId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("SerializedEntity")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("EstimateRelevanceJobId");

                    b.ToTable("Snapshots", (string)null);
                });

            modelBuilder.Entity("llassist.Common.Models.Article", b =>
                {
                    b.HasOne("llassist.Common.Models.Project", "Project")
                        .WithMany("Articles")
                        .HasForeignKey("ProjectId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Project");
                });

            modelBuilder.Entity("llassist.Common.Models.ArticleKeySemantic", b =>
                {
                    b.HasOne("llassist.Common.Models.Article", "Article")
                        .WithOne("ArticleKeySemantic")
                        .HasForeignKey("llassist.Common.Models.ArticleKeySemantic", "ArticleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.OwnsOne("llassist.Common.Models.KeySemantics", "KeySemantics", b1 =>
                        {
                            b1.Property<string>("ArticleKeySemanticArticleId")
                                .HasColumnType("text");

                            b1.Property<string[]>("Entities")
                                .IsRequired()
                                .HasColumnType("text[]");

                            b1.Property<string[]>("Keywords")
                                .IsRequired()
                                .HasColumnType("text[]");

                            b1.Property<string[]>("Topics")
                                .IsRequired()
                                .HasColumnType("text[]");

                            b1.HasKey("ArticleKeySemanticArticleId");

                            b1.ToTable("ArticleKeySemantics");

                            b1.ToJson("KeySemantics");

                            b1.WithOwner()
                                .HasForeignKey("ArticleKeySemanticArticleId");
                        });

                    b.Navigation("Article");

                    b.Navigation("KeySemantics")
                        .IsRequired();
                });

            modelBuilder.Entity("llassist.Common.Models.ArticleRelevance", b =>
                {
                    b.HasOne("llassist.Common.Models.Article", "Article")
                        .WithMany("ArticleRelevances")
                        .HasForeignKey("ArticleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.OwnsMany("llassist.Common.Models.Relevance", "Relevances", b1 =>
                        {
                            b1.Property<string>("ArticleRelevanceArticleId")
                                .HasColumnType("text");

                            b1.Property<string>("ArticleRelevanceEstimateRelevanceJobId")
                                .HasColumnType("text");

                            b1.Property<int>("Id")
                                .ValueGeneratedOnAdd()
                                .HasColumnType("integer");

                            b1.Property<string>("ContributionReason")
                                .IsRequired()
                                .HasColumnType("text");

                            b1.Property<double>("ContributionScore")
                                .HasColumnType("double precision");

                            b1.Property<bool>("IsContributing")
                                .HasColumnType("boolean");

                            b1.Property<bool>("IsRelevant")
                                .HasColumnType("boolean");

                            b1.Property<string>("Question")
                                .IsRequired()
                                .HasColumnType("text");

                            b1.Property<string>("RelevanceReason")
                                .IsRequired()
                                .HasColumnType("text");

                            b1.Property<double>("RelevanceScore")
                                .HasColumnType("double precision");

                            b1.HasKey("ArticleRelevanceArticleId", "ArticleRelevanceEstimateRelevanceJobId", "Id");

                            b1.ToTable("ArticleRelevances");

                            b1.ToJson("Relevances");

                            b1.WithOwner()
                                .HasForeignKey("ArticleRelevanceArticleId", "ArticleRelevanceEstimateRelevanceJobId");
                        });

                    b.Navigation("Article");

                    b.Navigation("Relevances");
                });

            modelBuilder.Entity("llassist.Common.Models.ProjectDefinition", b =>
                {
                    b.HasOne("llassist.Common.Models.Project", "Project")
                        .WithMany("ProjectDefinitions")
                        .HasForeignKey("ProjectId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Project");
                });

            modelBuilder.Entity("llassist.Common.Models.QuestionDefinition", b =>
                {
                    b.HasOne("llassist.Common.Models.ResearchQuestion", "ResearchQuestion")
                        .WithMany("QuestionDefinitions")
                        .HasForeignKey("ResearchQuestionId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("ResearchQuestion");
                });

            modelBuilder.Entity("llassist.Common.Models.ResearchQuestion", b =>
                {
                    b.HasOne("llassist.Common.Models.Project", "Project")
                        .WithMany("ResearchQuestions")
                        .HasForeignKey("ProjectId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Project");
                });

            modelBuilder.Entity("llassist.Common.Models.Snapshot", b =>
                {
                    b.HasOne("llassist.Common.Models.EstimateRelevanceJob", "EstimateRelevanceJob")
                        .WithMany("Snapshots")
                        .HasForeignKey("EstimateRelevanceJobId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("EstimateRelevanceJob");
                });

            modelBuilder.Entity("llassist.Common.Models.Article", b =>
                {
                    b.Navigation("ArticleKeySemantic");

                    b.Navigation("ArticleRelevances");
                });

            modelBuilder.Entity("llassist.Common.Models.EstimateRelevanceJob", b =>
                {
                    b.Navigation("Snapshots");
                });

            modelBuilder.Entity("llassist.Common.Models.Project", b =>
                {
                    b.Navigation("Articles");

                    b.Navigation("ProjectDefinitions");

                    b.Navigation("ResearchQuestions");
                });

            modelBuilder.Entity("llassist.Common.Models.ResearchQuestion", b =>
                {
                    b.Navigation("QuestionDefinitions");
                });
#pragma warning restore 612, 618
        }
    }
}
