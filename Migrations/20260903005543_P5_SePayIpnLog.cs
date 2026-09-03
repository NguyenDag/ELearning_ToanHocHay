using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ELearning_ToanHocHay_Control.Migrations
{
    /// <inheritdoc />
    public partial class P5_SePayIpnLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SePayIpnLog",
                columns: table => new
                {
                    IpnLogId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ReferenceCode = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    RawPayload = table.Column<string>(type: "text", nullable: false),
                    SubscriptionId = table.Column<int>(type: "integer", nullable: true),
                    TransferAmount = table.Column<long>(type: "bigint", nullable: false),
                    TransferType = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Outcome = table.Column<string>(type: "text", nullable: false),
                    ResultMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SePayIpnLog", x => x.IpnLogId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SePayIpnLog_ReferenceCode",
                table: "SePayIpnLog",
                column: "ReferenceCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SePayIpnLog_SubscriptionId",
                table: "SePayIpnLog",
                column: "SubscriptionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SePayIpnLog");
        }
    }
}
