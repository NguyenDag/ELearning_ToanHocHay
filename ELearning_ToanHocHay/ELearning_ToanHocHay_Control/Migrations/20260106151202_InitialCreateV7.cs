using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ELearning_ToanHocHay_Control.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreateV7 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StudentAnswer_AttemptId",
                table: "StudentAnswer");

            migrationBuilder.CreateIndex(
                name: "IX_StudentAnswer_AttemptId_QuestionId",
                table: "StudentAnswer",
                columns: new[] { "AttemptId", "QuestionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StudentAnswer_AttemptId_QuestionId",
                table: "StudentAnswer");

            migrationBuilder.CreateIndex(
                name: "IX_StudentAnswer_AttemptId",
                table: "StudentAnswer",
                column: "AttemptId");
        }
    }
}
