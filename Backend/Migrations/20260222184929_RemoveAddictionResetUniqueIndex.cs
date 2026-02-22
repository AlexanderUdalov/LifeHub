using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeHub_Backend.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAddictionResetUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AddictionResets_AddictionId_Date",
                table: "AddictionResets");

            migrationBuilder.CreateIndex(
                name: "IX_AddictionResets_AddictionId",
                table: "AddictionResets",
                column: "AddictionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AddictionResets_AddictionId",
                table: "AddictionResets");

            migrationBuilder.CreateIndex(
                name: "IX_AddictionResets_AddictionId_Date",
                table: "AddictionResets",
                columns: new[] { "AddictionId", "Date" });
        }
    }
}
