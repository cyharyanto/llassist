using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace llassist.ApiService.Migrations
{
    /// <inheritdoc />
    public partial class AddCompletedArticleCountToJob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CompletedArticles",
                table: "EstimateRelevanceJobs",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletedArticles",
                table: "EstimateRelevanceJobs");
        }
    }
}
