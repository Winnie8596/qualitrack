using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace QualiTrack.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { new Guid("8e23cde2-7b5b-4535-adfd-3d44b37eeebf"), "0cc6b840-a183-4c95-936e-45d0834a01ac", "QaManager", "QAMANAGER" },
                    { new Guid("c33ee7d7-1443-4b5d-a9e3-76c95376fb7c"), "cdbbc068-582f-4cd7-9258-f4d4cbad7211", "Inspector", "INSPECTOR" },
                    { new Guid("c89f3d68-15b6-440d-9fe7-c98b91bfa962"), "ec4145f4-63bd-4f13-89f8-1fa6601ff792", "Investigator", "INVESTIGATOR" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("8e23cde2-7b5b-4535-adfd-3d44b37eeebf"));

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("c33ee7d7-1443-4b5d-a9e3-76c95376fb7c"));

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("c89f3d68-15b6-440d-9fe7-c98b91bfa962"));
        }
    }
}
