using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeHub_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddJournalEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JournalEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Text = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    IsPinned = table.Column<bool>(type: "INTEGER", nullable: false),
                    PinnedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    TaskItemId = table.Column<Guid>(type: "TEXT", nullable: true),
                    HabitId = table.Column<Guid>(type: "TEXT", nullable: true),
                    AddictionId = table.Column<Guid>(type: "TEXT", nullable: true),
                    GoalId = table.Column<Guid>(type: "TEXT", nullable: true),
                    LifeAreaId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JournalEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JournalEntries_Addictions_AddictionId",
                        column: x => x.AddictionId,
                        principalTable: "Addictions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_JournalEntries_Goals_GoalId",
                        column: x => x.GoalId,
                        principalTable: "Goals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_JournalEntries_Habits_HabitId",
                        column: x => x.HabitId,
                        principalTable: "Habits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_JournalEntries_LifeAreas_LifeAreaId",
                        column: x => x.LifeAreaId,
                        principalTable: "LifeAreas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_JournalEntries_Tasks_TaskItemId",
                        column: x => x.TaskItemId,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_JournalEntries_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_AddictionId",
                table: "JournalEntries",
                column: "AddictionId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_GoalId",
                table: "JournalEntries",
                column: "GoalId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_HabitId",
                table: "JournalEntries",
                column: "HabitId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_LifeAreaId",
                table: "JournalEntries",
                column: "LifeAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_TaskItemId",
                table: "JournalEntries",
                column: "TaskItemId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_UserId_IsPinned_CreatedAt",
                table: "JournalEntries",
                columns: new[] { "UserId", "IsPinned", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JournalEntries");
        }
    }
}
