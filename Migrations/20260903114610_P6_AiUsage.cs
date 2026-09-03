using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ELearning_ToanHocHay_Control.Migrations
{
    /// <inheritdoc />
    public partial class P6_AiUsage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiUsageDaily",
                columns: table => new
                {
                    StudentId = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    HintCount = table.Column<int>(type: "integer", nullable: false),
                    FeedbackCount = table.Column<int>(type: "integer", nullable: false),
                    ChatCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiUsageDaily", x => new { x.StudentId, x.Date });
                    table.ForeignKey(
                        name: "FK_AiUsageDaily_Student_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Student",
                        principalColumn: "StudentId",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiUsageDaily");
        }
    }
}
