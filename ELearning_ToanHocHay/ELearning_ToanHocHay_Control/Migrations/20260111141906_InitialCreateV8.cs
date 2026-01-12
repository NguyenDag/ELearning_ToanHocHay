using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ELearning_ToanHocHay_Control.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreateV8 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Points",
                table: "Question");

            migrationBuilder.RenameColumn(
                name: "TotalPoints",
                table: "Exercise",
                newName: "TotalScores");

            migrationBuilder.AddColumn<double>(
                name: "Score",
                table: "ExerciseQuestion",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AlterColumn<int>(
                name: "ExerciseId",
                table: "ExerciseAttempt",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HintLevel",
                table: "AIFeedback",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Score",
                table: "ExerciseQuestion");

            migrationBuilder.DropColumn(
                name: "HintLevel",
                table: "AIFeedback");

            migrationBuilder.RenameColumn(
                name: "TotalScores",
                table: "Exercise",
                newName: "TotalPoints");

            migrationBuilder.AddColumn<double>(
                name: "Points",
                table: "Question",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AlterColumn<int>(
                name: "ExerciseId",
                table: "ExerciseAttempt",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");
        }
    }
}
