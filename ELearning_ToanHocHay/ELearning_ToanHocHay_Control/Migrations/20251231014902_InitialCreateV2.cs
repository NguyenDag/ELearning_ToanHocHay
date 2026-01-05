using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ELearning_ToanHocHay_Control.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreateV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RefreshToken",
                table: "User");

            migrationBuilder.DropColumn(
                name: "RefreshTokenExpiry",
                table: "User");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Topic",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Topic",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "QuestionBank",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "QuestionBank",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "AnswerOptions",
                table: "Question",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Question",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Package",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Lesson",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Exercise",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<double>(
                name: "PassingScore",
                table: "Exercise",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "TotalPoints",
                table: "Exercise",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "Subject",
                table: "Curriculum",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Chapter",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Chapter",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Topic");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Topic");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "QuestionBank");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "QuestionBank");

            migrationBuilder.DropColumn(
                name: "AnswerOptions",
                table: "Question");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Question");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Package");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Lesson");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Exercise");

            migrationBuilder.DropColumn(
                name: "PassingScore",
                table: "Exercise");

            migrationBuilder.DropColumn(
                name: "TotalPoints",
                table: "Exercise");

            migrationBuilder.DropColumn(
                name: "Subject",
                table: "Curriculum");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Chapter");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Chapter");

            migrationBuilder.AddColumn<string>(
                name: "RefreshToken",
                table: "User",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RefreshTokenExpiry",
                table: "User",
                type: "datetime2",
                nullable: true);
        }
    }
}
