using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartEventPlatform.RegistrationService.Migrations
{
    /// <inheritdoc />
    public partial class AddSagaTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Registrations",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Confirmed");

            migrationBuilder.CreateTable(
                name: "SagaStates",
                columns: table => new
                {
                    SagaId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RegistrationId = table.Column<long>(type: "bigint", nullable: true),
                    EventId = table.Column<long>(type: "bigint", nullable: false),
                    ParticipantId = table.Column<long>(type: "bigint", nullable: false),
                    LocationId = table.Column<long>(type: "bigint", nullable: false),
                    FailureReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SagaStates", x => x.SagaId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SagaStates_RegistrationId",
                table: "SagaStates",
                column: "RegistrationId");

            migrationBuilder.CreateIndex(
                name: "IX_SagaStates_Status",
                table: "SagaStates",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SagaStates");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Registrations");
        }
    }
}
