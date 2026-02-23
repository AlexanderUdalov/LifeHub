using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeHub_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddLifeAreas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "LifeAreaId",
                table: "Tasks",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LifeAreaId",
                table: "Habits",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LifeAreaId",
                table: "Goals",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LifeAreaId",
                table: "Addictions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LifeAreas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Color = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LifeAreas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LifeAreas_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_LifeAreaId",
                table: "Tasks",
                column: "LifeAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_Habits_LifeAreaId",
                table: "Habits",
                column: "LifeAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_Goals_LifeAreaId",
                table: "Goals",
                column: "LifeAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_Addictions_LifeAreaId",
                table: "Addictions",
                column: "LifeAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_LifeAreas_UserId",
                table: "LifeAreas",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Addictions_LifeAreas_LifeAreaId",
                table: "Addictions",
                column: "LifeAreaId",
                principalTable: "LifeAreas",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Goals_LifeAreas_LifeAreaId",
                table: "Goals",
                column: "LifeAreaId",
                principalTable: "LifeAreas",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Habits_LifeAreas_LifeAreaId",
                table: "Habits",
                column: "LifeAreaId",
                principalTable: "LifeAreas",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_LifeAreas_LifeAreaId",
                table: "Tasks",
                column: "LifeAreaId",
                principalTable: "LifeAreas",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Addictions_LifeAreas_LifeAreaId",
                table: "Addictions");

            migrationBuilder.DropForeignKey(
                name: "FK_Goals_LifeAreas_LifeAreaId",
                table: "Goals");

            migrationBuilder.DropForeignKey(
                name: "FK_Habits_LifeAreas_LifeAreaId",
                table: "Habits");

            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_LifeAreas_LifeAreaId",
                table: "Tasks");

            migrationBuilder.DropTable(
                name: "LifeAreas");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_LifeAreaId",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_Habits_LifeAreaId",
                table: "Habits");

            migrationBuilder.DropIndex(
                name: "IX_Goals_LifeAreaId",
                table: "Goals");

            migrationBuilder.DropIndex(
                name: "IX_Addictions_LifeAreaId",
                table: "Addictions");

            migrationBuilder.DropColumn(
                name: "LifeAreaId",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "LifeAreaId",
                table: "Habits");

            migrationBuilder.DropColumn(
                name: "LifeAreaId",
                table: "Goals");

            migrationBuilder.DropColumn(
                name: "LifeAreaId",
                table: "Addictions");
        }
    }
}
