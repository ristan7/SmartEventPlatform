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

            migrationBuilder.AddColumn<string>(
                name: "MessageId",
                table: "OutboxMessages",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValueSql: "REPLACE(CONVERT(nvarchar(36), NEWID()), '-', '')");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_MessageId",
                table: "OutboxMessages",
                column: "MessageId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_MessageId",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "MessageId",
                table: "OutboxMessages");

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