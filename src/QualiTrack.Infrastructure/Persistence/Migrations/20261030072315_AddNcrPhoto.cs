using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QualiTrack.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNcrPhoto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Photo",
                table: "NonConformances",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Photo",
                table: "NonConformances");
        }
    }
}
