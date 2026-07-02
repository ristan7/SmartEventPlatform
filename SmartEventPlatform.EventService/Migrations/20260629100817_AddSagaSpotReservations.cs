using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartEventPlatform.EventService.Migrations
{
    /// <inheritdoc />
    public partial class AddSagaSpotReservations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SagaSpotReservations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SagaId = table.Column<long>(type: "bigint", nullable: false),
                    EventId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SagaSpotReservations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SagaSpotReservations_EventId",
                table: "SagaSpotReservations",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_SagaSpotReservations_SagaId",
                table: "SagaSpotReservations",
                column: "SagaId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SagaSpotReservations");
        }
    }
}
