using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace llassist.ApiService.Migrations
{
    /// <inheritdoc />
    public partial class AllowMultipleRelevances : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ArticleRelevances_ArticleId",
                table: "ArticleRelevances");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ArticleRelevances_ArticleId",
                table: "ArticleRelevances",
                column: "ArticleId",
                unique: true);
        }
    }
}
