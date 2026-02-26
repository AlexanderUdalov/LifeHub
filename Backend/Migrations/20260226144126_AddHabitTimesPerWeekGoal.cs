using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeHub_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddHabitTimesPerWeekGoal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TimesPerWeekGoal",
                table: "Habits",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TimesPerWeekGoal",
                table: "Habits");
        }
    }
}
