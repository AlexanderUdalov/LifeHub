using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeHub_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddAddictionDescriptionAndTriggerEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Addictions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AddictionTriggerEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AddictionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EventAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Outcome = table.Column<int>(type: "INTEGER", nullable: false),
                    Note = table.Column<string>(type: "TEXT", nullable: true),
                    JournalEntryId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AddictionTriggerEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AddictionTriggerEvents_Addictions_AddictionId",
                        column: x => x.AddictionId,
                        principalTable: "Addictions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AddictionTriggerEvents_JournalEntries_JournalEntryId",
                        column: x => x.JournalEntryId,
                        principalTable: "JournalEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AddictionTriggerEvents_AddictionId",
                table: "AddictionTriggerEvents",
                column: "AddictionId");

            migrationBuilder.CreateIndex(
                name: "IX_AddictionTriggerEvents_JournalEntryId",
                table: "AddictionTriggerEvents",
                column: "JournalEntryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AddictionTriggerEvents");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Addictions");
        }
    }
}
