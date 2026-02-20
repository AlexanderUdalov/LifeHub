using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeHub_Backend.Migrations
{
    /// <inheritdoc />
    public partial class MakeHabitRecurrenceRequired : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Goals_LifeFocuses_LifeFocusId",
                table: "Goals");

            migrationBuilder.DropForeignKey(
                name: "FK_Habits_Goals_GoalId",
                table: "Habits");

            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_Goals_GoalId",
                table: "Tasks");

            migrationBuilder.AlterColumn<string>(
                name: "RecurrenceRule",
                table: "Habits",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Goals_LifeFocuses_LifeFocusId",
                table: "Goals",
                column: "LifeFocusId",
                principalTable: "LifeFocuses",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Habits_Goals_GoalId",
                table: "Habits",
                column: "GoalId",
                principalTable: "Goals",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_Goals_GoalId",
                table: "Tasks",
                column: "GoalId",
                principalTable: "Goals",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Goals_LifeFocuses_LifeFocusId",
                table: "Goals");

            migrationBuilder.DropForeignKey(
                name: "FK_Habits_Goals_GoalId",
                table: "Habits");

            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_Goals_GoalId",
                table: "Tasks");

            migrationBuilder.AlterColumn<string>(
                name: "RecurrenceRule",
                table: "Habits",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddForeignKey(
                name: "FK_Goals_LifeFocuses_LifeFocusId",
                table: "Goals",
                column: "LifeFocusId",
                principalTable: "LifeFocuses",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Habits_Goals_GoalId",
                table: "Habits",
                column: "GoalId",
                principalTable: "Goals",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_Goals_GoalId",
                table: "Tasks",
                column: "GoalId",
                principalTable: "Goals",
                principalColumn: "Id");
        }
    }
}
