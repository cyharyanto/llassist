using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace llassist.ApiService.Migrations
{
    /// <inheritdoc />
    public partial class FixArticleKeySemanticsAndRelevances : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ArticleRelevances",
                table: "ArticleRelevances");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ArticleKeySemantics",
                table: "ArticleKeySemantics");

            migrationBuilder.DropColumn(
                name: "Relevances",
                table: "ArticleRelevances");

            migrationBuilder.DropColumn(
                name: "KeySemantics",
                table: "ArticleKeySemantics");

            migrationBuilder.RenameColumn(
                name: "MustRead",
                table: "ArticleRelevances",
                newName: "IsRelevant");

            migrationBuilder.AddColumn<bool>(
                name: "MustRead",
                table: "Articles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "RelevanceIndex",
                table: "ArticleRelevances",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ContributionReason",
                table: "ArticleRelevances",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "ContributionScore",
                table: "ArticleRelevances",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<bool>(
                name: "IsContributing",
                table: "ArticleRelevances",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Question",
                table: "ArticleRelevances",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RelevanceReason",
                table: "ArticleRelevances",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "RelevanceScore",
                table: "ArticleRelevances",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "KeySemanticIndex",
                table: "ArticleKeySemantics",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "ArticleKeySemantics",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Value",
                table: "ArticleKeySemantics",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ArticleRelevances",
                table: "ArticleRelevances",
                columns: new[] { "ArticleId", "EstimateRelevanceJobId", "RelevanceIndex" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_ArticleKeySemantics",
                table: "ArticleKeySemantics",
                columns: new[] { "ArticleId", "KeySemanticIndex" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ArticleRelevances",
                table: "ArticleRelevances");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ArticleKeySemantics",
                table: "ArticleKeySemantics");

            migrationBuilder.DropColumn(
                name: "MustRead",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "RelevanceIndex",
                table: "ArticleRelevances");

            migrationBuilder.DropColumn(
                name: "ContributionReason",
                table: "ArticleRelevances");

            migrationBuilder.DropColumn(
                name: "ContributionScore",
                table: "ArticleRelevances");

            migrationBuilder.DropColumn(
                name: "IsContributing",
                table: "ArticleRelevances");

            migrationBuilder.DropColumn(
                name: "Question",
                table: "ArticleRelevances");

            migrationBuilder.DropColumn(
                name: "RelevanceReason",
                table: "ArticleRelevances");

            migrationBuilder.DropColumn(
                name: "RelevanceScore",
                table: "ArticleRelevances");

            migrationBuilder.DropColumn(
                name: "KeySemanticIndex",
                table: "ArticleKeySemantics");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "ArticleKeySemantics");

            migrationBuilder.DropColumn(
                name: "Value",
                table: "ArticleKeySemantics");

            migrationBuilder.RenameColumn(
                name: "IsRelevant",
                table: "ArticleRelevances",
                newName: "MustRead");

            migrationBuilder.AddColumn<string>(
                name: "Relevances",
                table: "ArticleRelevances",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KeySemantics",
                table: "ArticleKeySemantics",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ArticleRelevances",
                table: "ArticleRelevances",
                columns: new[] { "ArticleId", "EstimateRelevanceJobId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_ArticleKeySemantics",
                table: "ArticleKeySemantics",
                column: "ArticleId");
        }
    }
}
