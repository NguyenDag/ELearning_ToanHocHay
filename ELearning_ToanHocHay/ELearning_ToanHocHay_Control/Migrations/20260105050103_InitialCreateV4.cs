using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ELearning_ToanHocHay_Control.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreateV4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnswerOptions",
                table: "Question");

            migrationBuilder.AlterColumn<double>(
                name: "PointsEarned",
                table: "StudentAnswer",
                type: "float",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<bool>(
                name: "IsCorrect",
                table: "StudentAnswer",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SelectedOptionId",
                table: "StudentAnswer",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "Points",
                table: "Question",
                type: "float",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "CorrectAnswer",
                table: "Question",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Question",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "QuestionOption",
                columns: table => new
                {
                    OptionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuestionId = table.Column<int>(type: "int", nullable: false),
                    OptionText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsCorrect = table.Column<bool>(type: "bit", nullable: false),
                    OrderIndex = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionOption", x => x.OptionId);
                    table.ForeignKey(
                        name: "FK_QuestionOption_Question_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Question",
                        principalColumn: "QuestionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StudentAnswer_SelectedOptionId",
                table: "StudentAnswer",
                column: "SelectedOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionOption_QuestionId",
                table: "QuestionOption",
                column: "QuestionId");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentAnswer_QuestionOption_SelectedOptionId",
                table: "StudentAnswer",
                column: "SelectedOptionId",
                principalTable: "QuestionOption",
                principalColumn: "OptionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudentAnswer_QuestionOption_SelectedOptionId",
                table: "StudentAnswer");

            migrationBuilder.DropTable(
                name: "QuestionOption");

            migrationBuilder.DropIndex(
                name: "IX_StudentAnswer_SelectedOptionId",
                table: "StudentAnswer");

            migrationBuilder.DropColumn(
                name: "SelectedOptionId",
                table: "StudentAnswer");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Question");

            migrationBuilder.AlterColumn<int>(
                name: "PointsEarned",
                table: "StudentAnswer",
                type: "int",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<bool>(
                name: "IsCorrect",
                table: "StudentAnswer",
                type: "bit",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<int>(
                name: "Points",
                table: "Question",
                type: "int",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<string>(
                name: "CorrectAnswer",
                table: "Question",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AnswerOptions",
                table: "Question",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
