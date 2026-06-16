using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartEventPlatform.EventService.Migrations
{
    public partial class KeysFixed : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventRegistrationTrackers");

            migrationBuilder.CreateTable(
                name: "EventRegistrationTrackers",
                columns: table => new
                {
                    EventId = table.Column<long>(type: "bigint", nullable: false),
                    RegistrationCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventRegistrationTrackers", x => x.EventId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventRegistrationTrackers");

            migrationBuilder.CreateTable(
                name: "EventRegistrationTrackers",
                columns: table => new
                {
                    EventId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RegistrationCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventRegistrationTrackers", x => x.EventId);
                });
        }
    }
}