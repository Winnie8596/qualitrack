using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QualiTrack.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNcrListIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_NonConformances_DueDate",
                table: "NonConformances",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_NonConformances_InvestigatorId",
                table: "NonConformances",
                column: "InvestigatorId");

            migrationBuilder.CreateIndex(
                name: "IX_NonConformances_Status",
                table: "NonConformances",
                column: "Status");

            // Not in the model: EF Core 10 cannot index a complex property's column (see NonConformanceConfiguration)
            migrationBuilder.CreateIndex(
                name: "IX_NonConformances_Raised_By",
                table: "NonConformances",
                column: "Raised_By");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NonConformances_Raised_By",
                table: "NonConformances");

            migrationBuilder.DropIndex(
                name: "IX_NonConformances_DueDate",
                table: "NonConformances");

            migrationBuilder.DropIndex(
                name: "IX_NonConformances_InvestigatorId",
                table: "NonConformances");

            migrationBuilder.DropIndex(
                name: "IX_NonConformances_Status",
                table: "NonConformances");
        }
    }
}
