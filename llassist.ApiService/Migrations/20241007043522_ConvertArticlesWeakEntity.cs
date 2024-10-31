using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace llassist.ApiService.Migrations
{
    /// <inheritdoc />
    public partial class ConvertArticlesWeakEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ArticleKeySemantics_Articles_Id",
                table: "ArticleKeySemantics");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ArticleRelevances",
                table: "ArticleRelevances");

            migrationBuilder.DropIndex(
                name: "IX_ArticleRelevances_ArticleId_EstimateRelevanceJobId",
                table: "ArticleRelevances");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "ArticleRelevances");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "ArticleKeySemantics",
                newName: "ArticleId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ArticleRelevances",
                table: "ArticleRelevances",
                columns: new[] { "ArticleId", "EstimateRelevanceJobId" });

            migrationBuilder.AddForeignKey(
                name: "FK_ArticleKeySemantics_Articles_ArticleId",
                table: "ArticleKeySemantics",
                column: "ArticleId",
                principalTable: "Articles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ArticleKeySemantics_Articles_ArticleId",
                table: "ArticleKeySemantics");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ArticleRelevances",
                table: "ArticleRelevances");

            migrationBuilder.RenameColumn(
                name: "ArticleId",
                table: "ArticleKeySemantics",
                newName: "Id");

            migrationBuilder.AddColumn<string>(
                name: "Id",
                table: "ArticleRelevances",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ArticleRelevances",
                table: "ArticleRelevances",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_ArticleRelevances_ArticleId_EstimateRelevanceJobId",
                table: "ArticleRelevances",
                columns: new[] { "ArticleId", "EstimateRelevanceJobId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ArticleKeySemantics_Articles_Id",
                table: "ArticleKeySemantics",
                column: "Id",
                principalTable: "Articles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
