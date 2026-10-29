using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QualiTrack.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NcrSequences",
                columns: table => new
                {
                    Year = table.Column<int>(type: "integer", nullable: false),
                    LastSequence = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NcrSequences", x => x.Year);
                });

            migrationBuilder.CreateTable(
                name: "NonConformances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    InvestigatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    DueDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RootCause = table.Column<string>(type: "text", nullable: true),
                    Details_Description = table.Column<string>(type: "text", nullable: false),
                    Details_Location = table.Column<string>(type: "text", nullable: false),
                    Details_Severity = table.Column<string>(type: "text", nullable: false),
                    Details_Title = table.Column<string>(type: "text", nullable: false),
                    Raised_At = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Raised_By = table.Column<Guid>(type: "uuid", nullable: false),
                    Actions = table.Column<string>(type: "jsonb", nullable: false),
                    History = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NonConformances", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NonConformances_Number",
                table: "NonConformances",
                column: "Number",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NcrSequences");

            migrationBuilder.DropTable(
                name: "NonConformances");
        }
    }
}
