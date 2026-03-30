using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SmartEventPlatformWeb.Migrations
{
    /// <inheritdoc />
    public partial class SeedEventRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "EventRoles",
                columns: new[] { "EventRoleId", "Name" },
                values: new object[,]
                {
                    { 1L, "Main Speaker" },
                    { 2L, "Guest Speaker" },
                    { 3L, "Moderator" },
                    { 4L, "Panelist" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "EventRoles",
                keyColumn: "EventRoleId",
                keyValue: 1L);

            migrationBuilder.DeleteData(
                table: "EventRoles",
                keyColumn: "EventRoleId",
                keyValue: 2L);

            migrationBuilder.DeleteData(
                table: "EventRoles",
                keyColumn: "EventRoleId",
                keyValue: 3L);

            migrationBuilder.DeleteData(
                table: "EventRoles",
                keyColumn: "EventRoleId",
                keyValue: 4L);
        }
    }
}
