using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartEventPlatformWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueRegistrationConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Registrations_EventId",
                table: "Registrations");

            migrationBuilder.CreateIndex(
                name: "IX_Registrations_EventId_ParticipantId",
                table: "Registrations",
                columns: new[] { "EventId", "ParticipantId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Registrations_EventId_ParticipantId",
                table: "Registrations");

            migrationBuilder.CreateIndex(
                name: "IX_Registrations_EventId",
                table: "Registrations",
                column: "EventId");
        }
    }
}
