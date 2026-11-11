using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeHub_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddHabitLifecycleAndSchedules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "StartDate",
                table: "Habits",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<int>(
                name: "State",
                table: "Habits",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "HabitInactivePeriods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    HabitId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HabitInactivePeriods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HabitInactivePeriods_Habits_HabitId",
                        column: x => x.HabitId,
                        principalTable: "Habits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HabitSchedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    HabitId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    RecurrenceRule = table.Column<string>(type: "TEXT", nullable: false),
                    TimesPerWeekGoal = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HabitSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HabitSchedules_Habits_HabitId",
                        column: x => x.HabitId,
                        principalTable: "Habits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Preserve every existing mark. Older habits have no creation timestamp, so
            // use their first recorded day (or migration day when there is no history).
            migrationBuilder.Sql("""
                UPDATE Habits SET StartDate = MIN(
                    COALESCE((SELECT MIN(Date) FROM HabitDays WHERE HabitId = Habits.Id), date('now', 'localtime')),
                    date('now', 'localtime'));
                INSERT INTO HabitSchedules (Id, HabitId, EffectiveFrom, RecurrenceRule, TimesPerWeekGoal)
                SELECT Id, Id, StartDate, RecurrenceRule, TimesPerWeekGoal FROM Habits;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_HabitInactivePeriods_HabitId",
                table: "HabitInactivePeriods",
                column: "HabitId",
                unique: true,
                filter: "\"EndDate\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_HabitInactivePeriods_HabitId_StartDate",
                table: "HabitInactivePeriods",
                columns: new[] { "HabitId", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "IX_HabitSchedules_HabitId_EffectiveFrom",
                table: "HabitSchedules",
                columns: new[] { "HabitId", "EffectiveFrom" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HabitInactivePeriods");

            migrationBuilder.DropTable(
                name: "HabitSchedules");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "Habits");

            migrationBuilder.DropColumn(
                name: "State",
                table: "Habits");
        }
    }
}
