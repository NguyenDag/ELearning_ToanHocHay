using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ELearning_ToanHocHay_Control.Migrations
{
    /// <inheritdoc />
    public partial class UpdateQuestionAndPackage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "EndTime",
                table: "ExerciseAttempt",
                newName: "SubmittedAt");

            migrationBuilder.AddColumn<DateTime>(
                name: "PlannedEndTime",
                table: "ExerciseAttempt",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "ExerciseAttempt",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlannedEndTime",
                table: "ExerciseAttempt");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "ExerciseAttempt");

            migrationBuilder.RenameColumn(
                name: "SubmittedAt",
                table: "ExerciseAttempt",
                newName: "EndTime");
        }
    }
}
