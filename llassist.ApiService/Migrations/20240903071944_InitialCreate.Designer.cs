﻿// <auto-generated />
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
    [Migration("20240903071944_InitialCreate")]
    partial class InitialCreate
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

                    b.Property<bool>("MustRead")
                        .HasColumnType("boolean");

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

            modelBuilder.Entity("llassist.Common.Models.Article", b =>
                {
                    b.HasOne("llassist.Common.Models.Project", "Project")
                        .WithMany("Articles")
                        .HasForeignKey("ProjectId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.OwnsOne("llassist.Common.Models.KeySemantics", "KeySemantics", b1 =>
                        {
                            b1.Property<string>("ArticleId")
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

                            b1.HasKey("ArticleId");

                            b1.ToTable("Articles");

                            b1.ToJson("KeySemantics");

                            b1.WithOwner()
                                .HasForeignKey("ArticleId");
                        });

                    b.OwnsMany("llassist.Common.Models.Relevance", "Relevances", b1 =>
                        {
                            b1.Property<string>("ArticleId")
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

                            b1.HasKey("ArticleId", "Id");

                            b1.ToTable("Articles");

                            b1.ToJson("Relevances");

                            b1.WithOwner()
                                .HasForeignKey("ArticleId");
                        });

                    b.Navigation("KeySemantics")
                        .IsRequired();

                    b.Navigation("Project");

                    b.Navigation("Relevances");
                });

            modelBuilder.Entity("llassist.Common.Models.Project", b =>
                {
                    b.OwnsOne("llassist.Common.Models.ResearchQuestions", "ResearchQuestions", b1 =>
                        {
                            b1.Property<string>("ProjectId")
                                .HasColumnType("text");

                            b1.Property<string[]>("Definitions")
                                .IsRequired()
                                .HasColumnType("text[]");

                            b1.HasKey("ProjectId");

                            b1.ToTable("Projects");

                            b1.ToJson("ResearchQuestions");

                            b1.WithOwner()
                                .HasForeignKey("ProjectId");

                            b1.OwnsMany("llassist.Common.Models.Question", "Questions", b2 =>
                                {
                                    b2.Property<string>("ResearchQuestionsProjectId")
                                        .HasColumnType("text");

                                    b2.Property<int>("Id")
                                        .ValueGeneratedOnAdd()
                                        .HasColumnType("integer");

                                    b2.Property<string[]>("Definitions")
                                        .IsRequired()
                                        .HasColumnType("text[]");

                                    b2.Property<string>("Text")
                                        .IsRequired()
                                        .HasColumnType("text");

                                    b2.HasKey("ResearchQuestionsProjectId", "Id");

                                    b2.ToTable("Projects");

                                    b2.WithOwner()
                                        .HasForeignKey("ResearchQuestionsProjectId");
                                });

                            b1.Navigation("Questions");
                        });

                    b.Navigation("ResearchQuestions")
                        .IsRequired();
                });

            modelBuilder.Entity("llassist.Common.Models.Project", b =>
                {
                    b.Navigation("Articles");
                });
#pragma warning restore 612, 618
        }
    }
}
