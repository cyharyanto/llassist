using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace llassist.ApiService.Migrations
{
    /// <inheritdoc />
    public partial class InitDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EstimateRelevanceJobs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    ModelName = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TotalArticles = table.Column<int>(type: "integer", nullable: false),
                    ProjectId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstimateRelevanceJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Snapshots",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    EntityType = table.Column<string>(type: "text", nullable: false),
                    EntityId = table.Column<string>(type: "text", nullable: false),
                    SerializedEntity = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EstimateRelevanceJobId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Snapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Snapshots_EstimateRelevanceJobs_EstimateRelevanceJobId",
                        column: x => x.EstimateRelevanceJobId,
                        principalTable: "EstimateRelevanceJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Articles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Authors = table.Column<string>(type: "text", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    DOI = table.Column<string>(type: "text", nullable: false),
                    Link = table.Column<string>(type: "text", nullable: false),
                    Abstract = table.Column<string>(type: "text", nullable: false),
                    ProjectId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Articles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Articles_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectDefinitions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Definition = table.Column<string>(type: "text", nullable: false),
                    ProjectId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectDefinitions_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ResearchQuestions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    QuestionText = table.Column<string>(type: "text", nullable: false),
                    ProjectId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResearchQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResearchQuestions_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ArticleKeySemantics",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    KeySemantics = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArticleKeySemantics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArticleKeySemantics_Articles_Id",
                        column: x => x.Id,
                        principalTable: "Articles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ArticleRelevances",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MustRead = table.Column<bool>(type: "boolean", nullable: false),
                    ArticleId = table.Column<string>(type: "text", nullable: false),
                    EstimateRelevanceJobId = table.Column<string>(type: "text", nullable: false),
                    Relevances = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArticleRelevances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArticleRelevances_Articles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "Articles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuestionDefinitions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Definition = table.Column<string>(type: "text", nullable: false),
                    ResearchQuestionId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuestionDefinitions_ResearchQuestions_ResearchQuestionId",
                        column: x => x.ResearchQuestionId,
                        principalTable: "ResearchQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ArticleRelevances_ArticleId",
                table: "ArticleRelevances",
                column: "ArticleId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ArticleRelevances_ArticleId_EstimateRelevanceJobId",
                table: "ArticleRelevances",
                columns: new[] { "ArticleId", "EstimateRelevanceJobId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Articles_ProjectId",
                table: "Articles",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_EstimateRelevanceJobs_ProjectId",
                table: "EstimateRelevanceJobs",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDefinitions_ProjectId",
                table: "ProjectDefinitions",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionDefinitions_ResearchQuestionId",
                table: "QuestionDefinitions",
                column: "ResearchQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_ResearchQuestions_ProjectId",
                table: "ResearchQuestions",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Snapshots_EstimateRelevanceJobId",
                table: "Snapshots",
                column: "EstimateRelevanceJobId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArticleKeySemantics");

            migrationBuilder.DropTable(
                name: "ArticleRelevances");

            migrationBuilder.DropTable(
                name: "ProjectDefinitions");

            migrationBuilder.DropTable(
                name: "QuestionDefinitions");

            migrationBuilder.DropTable(
                name: "Snapshots");

            migrationBuilder.DropTable(
                name: "Articles");

            migrationBuilder.DropTable(
                name: "ResearchQuestions");

            migrationBuilder.DropTable(
                name: "EstimateRelevanceJobs");

            migrationBuilder.DropTable(
                name: "Projects");
        }
    }
}
