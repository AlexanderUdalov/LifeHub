using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeHub_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddAddictionResetJournalEntry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "JournalEntryId",
                table: "AddictionResets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AddictionResets_JournalEntryId",
                table: "AddictionResets",
                column: "JournalEntryId");

            migrationBuilder.AddForeignKey(
                name: "FK_AddictionResets_JournalEntries_JournalEntryId",
                table: "AddictionResets",
                column: "JournalEntryId",
                principalTable: "JournalEntries",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AddictionResets_JournalEntries_JournalEntryId",
                table: "AddictionResets");

            migrationBuilder.DropIndex(
                name: "IX_AddictionResets_JournalEntryId",
                table: "AddictionResets");

            migrationBuilder.DropColumn(
                name: "JournalEntryId",
                table: "AddictionResets");
        }
    }
}
