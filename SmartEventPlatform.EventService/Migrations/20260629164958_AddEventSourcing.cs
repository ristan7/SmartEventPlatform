using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartEventPlatform.EventService.Migrations
{
    /// <inheritdoc />
    public partial class AddEventSourcing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EventSnapshotEntries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AggregateId = table.Column<long>(type: "bigint", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    SnapshotData = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventSnapshotEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EventStoreEntries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AggregateId = table.Column<long>(type: "bigint", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventStoreEntries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventSnapshotEntries_AggregateId",
                table: "EventSnapshotEntries",
                column: "AggregateId");

            migrationBuilder.CreateIndex(
                name: "IX_EventStoreEntries_AggregateId",
                table: "EventStoreEntries",
                column: "AggregateId");

            migrationBuilder.CreateIndex(
                name: "IX_EventStoreEntries_AggregateId_Version",
                table: "EventStoreEntries",
                columns: new[] { "AggregateId", "Version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventSnapshotEntries");

            migrationBuilder.DropTable(
                name: "EventStoreEntries");
        }
    }
}
