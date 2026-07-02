using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartEventPlatform.DirectoryService.Migrations
{
    /// <inheritdoc />
    public partial class AddLocationRegistrationTracker : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LocationRegistrationTrackers",
                columns: table => new
                {
                    LocationId = table.Column<long>(type: "bigint", nullable: false),
                    RegistrationCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocationRegistrationTrackers", x => x.LocationId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LocationRegistrationTrackers");
        }
    }
}
