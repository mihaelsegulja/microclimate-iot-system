using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MicroclimateIotSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Add_Device_HardwareId_UniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Devices_HardwareId",
                table: "Devices",
                column: "HardwareId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Devices_HardwareId",
                table: "Devices");
        }
    }
}
