using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartEventPlatform.RegistrationService.Migrations
{
    /// <inheritdoc />
    public partial class AddSagaChoreographyState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SagaChoreographyStates",
                columns: table => new
                {
                    SagaId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RegistrationId = table.Column<long>(type: "bigint", nullable: true),
                    EventId = table.Column<long>(type: "bigint", nullable: false),
                    ParticipantId = table.Column<long>(type: "bigint", nullable: false),
                    LocationId = table.Column<long>(type: "bigint", nullable: false),
                    ParticipantFirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ParticipantLastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ParticipantEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EventName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RegistrationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FailureReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SagaChoreographyStates", x => x.SagaId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SagaChoreographyStates_CorrelationId",
                table: "SagaChoreographyStates",
                column: "CorrelationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SagaChoreographyStates_Status",
                table: "SagaChoreographyStates",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SagaChoreographyStates");
        }
    }
}
