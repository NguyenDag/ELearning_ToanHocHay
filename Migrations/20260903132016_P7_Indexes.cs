using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ELearning_ToanHocHay_Control.Migrations
{
    /// <inheritdoc />
    public partial class P7_Indexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Notification_UserId",
                table: "Notification");

            migrationBuilder.CreateIndex(
                name: "IX_Notification_UserId_IsRead",
                table: "Notification",
                columns: new[] { "UserId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseAttempt_StudentId_ExerciseId_Status",
                table: "ExerciseAttempt",
                columns: new[] { "StudentId", "ExerciseId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseAttempt_StudentId_Status_SubmittedAt",
                table: "ExerciseAttempt",
                columns: new[] { "StudentId", "Status", "SubmittedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Notification_UserId_IsRead",
                table: "Notification");

            migrationBuilder.DropIndex(
                name: "IX_ExerciseAttempt_StudentId_ExerciseId_Status",
                table: "ExerciseAttempt");

            migrationBuilder.DropIndex(
                name: "IX_ExerciseAttempt_StudentId_Status_SubmittedAt",
                table: "ExerciseAttempt");

            migrationBuilder.CreateIndex(
                name: "IX_Notification_UserId",
                table: "Notification",
                column: "UserId");
        }
    }
}
