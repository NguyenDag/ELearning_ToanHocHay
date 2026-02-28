using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ELearning_ToanHocHay_Control.Migrations
{
    /// <inheritdoc />
    public partial class FixStudentAnswerRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StudentAnswer_SelectedOptionId",
                table: "StudentAnswer");

            migrationBuilder.CreateIndex(
                name: "IX_StudentAnswer_SelectedOptionId",
                table: "StudentAnswer",
                column: "SelectedOptionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StudentAnswer_SelectedOptionId",
                table: "StudentAnswer");

            migrationBuilder.CreateIndex(
                name: "IX_StudentAnswer_SelectedOptionId",
                table: "StudentAnswer",
                column: "SelectedOptionId",
                unique: true);
        }
    }
}
