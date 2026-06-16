using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartEventPlatform.DirectoryService.Migrations
{
    public partial class EventSpeakerIdFix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LocationUsageTrackers");

            migrationBuilder.DropTable(
                name: "SpeakerUsageTrackers");

            migrationBuilder.CreateTable(
                name: "LocationUsageTrackers",
                columns: table => new
                {
                    EventId = table.Column<long>(type: "bigint", nullable: false),
                    LocationId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocationUsageTrackers", x => x.EventId);
                });

            migrationBuilder.CreateTable(
                name: "SpeakerUsageTrackers",
                columns: table => new
                {
                    EventSpeakerId = table.Column<long>(type: "bigint", nullable: false),
                    SpeakerId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpeakerUsageTrackers", x => x.EventSpeakerId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LocationUsageTrackers_LocationId",
                table: "LocationUsageTrackers",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_SpeakerUsageTrackers_SpeakerId",
                table: "SpeakerUsageTrackers",
                column: "SpeakerId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LocationUsageTrackers");

            migrationBuilder.DropTable(
                name: "SpeakerUsageTrackers");

            migrationBuilder.CreateTable(
                name: "LocationUsageTrackers",
                columns: table => new
                {
                    EventId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LocationId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocationUsageTrackers", x => x.EventId);
                });

            migrationBuilder.CreateTable(
                name: "SpeakerUsageTrackers",
                columns: table => new
                {
                    EventSpeakerId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SpeakerId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpeakerUsageTrackers", x => x.EventSpeakerId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LocationUsageTrackers_LocationId",
                table: "LocationUsageTrackers",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_SpeakerUsageTrackers_SpeakerId",
                table: "SpeakerUsageTrackers",
                column: "SpeakerId");
        }
    }
}