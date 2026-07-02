using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartEventPlatform.RegistrationService.Migrations
{
    /// <inheritdoc />
    public partial class KeysFixed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MessageId",
                table: "OutboxMessages",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_MessageId",
                table: "OutboxMessages",
                column: "MessageId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_MessageId",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "MessageId",
                table: "OutboxMessages");
        }
    }
}
