using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MicroclimateIotSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Add_TelemetryReadingsIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_TelemetryReadings_HardwareId_Timestamp",
                table: "TelemetryReadings",
                columns: new[] { "HardwareId", "Timestamp" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TelemetryReadings_HardwareId_Timestamp",
                table: "TelemetryReadings");
        }
    }
}
