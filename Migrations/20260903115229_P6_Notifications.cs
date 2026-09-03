using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ELearning_ToanHocHay_Control.Migrations
{
    /// <inheritdoc />
    public partial class P6_Notifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NotificationPreference",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    RuleKey = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationPreference", x => new { x.UserId, x.RuleKey });
                    table.ForeignKey(
                        name: "FK_NotificationPreference_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificationPreference");
        }
    }
}
