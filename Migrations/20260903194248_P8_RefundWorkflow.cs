using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ELearning_ToanHocHay_Control.Migrations
{
    /// <inheritdoc />
    public partial class P8_RefundWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DataProtectionKeys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FriendlyName = table.Column<string>(type: "text", nullable: true),
                    Xml = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataProtectionKeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RefundBatch",
                columns: table => new
                {
                    RefundBatchId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExportedByUserId = table.Column<int>(type: "integer", nullable: true),
                    ExportedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DisbursedByUserId = table.Column<int>(type: "integer", nullable: true),
                    DisbursedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DisbursementNote = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ItemCount = table.Column<int>(type: "integer", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefundBatch", x => x.RefundBatchId);
                });

            migrationBuilder.CreateTable(
                name: "RefundRequest",
                columns: table => new
                {
                    RefundRequestId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentId = table.Column<int>(type: "integer", nullable: false),
                    RequestedByUserId = table.Column<int>(type: "integer", nullable: false),
                    OnBehalf = table.Column<bool>(type: "boolean", nullable: false),
                    BeneficiaryUserId = table.Column<int>(type: "integer", nullable: false),
                    ReasonCode = table.Column<string>(type: "text", nullable: false),
                    ReasonNote = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    BankBin = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    BankAccountNumberProtected = table.Column<string>(type: "text", nullable: false),
                    BankAccountNumberLast4 = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    BankAccountHolderName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    FirstApprovedByUserId = table.Column<int>(type: "integer", nullable: true),
                    FirstApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovedByUserId = table.Column<int>(type: "integer", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RejectedByUserId = table.Column<int>(type: "integer", nullable: true),
                    RejectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RefundBatchId = table.Column<int>(type: "integer", nullable: true),
                    BankTransactionRef = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefundRequest", x => x.RefundRequestId);
                    table.ForeignKey(
                        name: "FK_RefundRequest_Payment_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "Payment",
                        principalColumn: "PaymentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RefundRequest_RefundBatch_RefundBatchId",
                        column: x => x.RefundBatchId,
                        principalTable: "RefundBatch",
                        principalColumn: "RefundBatchId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "RefundEvent",
                columns: table => new
                {
                    RefundEventId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RefundRequestId = table.Column<int>(type: "integer", nullable: true),
                    RefundBatchId = table.Column<int>(type: "integer", nullable: true),
                    EventType = table.Column<string>(type: "text", nullable: false),
                    FromStatus = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    ToStatus = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    ActorUserId = table.Column<int>(type: "integer", nullable: true),
                    ActorUserType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    AmountSnapshot = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefundEvent", x => x.RefundEventId);
                    table.ForeignKey(
                        name: "FK_RefundEvent_RefundBatch_RefundBatchId",
                        column: x => x.RefundBatchId,
                        principalTable: "RefundBatch",
                        principalColumn: "RefundBatchId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RefundEvent_RefundRequest_RefundRequestId",
                        column: x => x.RefundRequestId,
                        principalTable: "RefundRequest",
                        principalColumn: "RefundRequestId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "SystemConfig",
                columns: new[] { "ConfigId", "ConfigGroup", "ConfigKey", "ConfigType", "ConfigValue", "Description", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { 24, "refund", "refund.dailyCapVnd", "Decimal", "20000000", "Trần tổng tiền hoàn được duyệt trong 1 ngày (giờ VN)", new DateTime(2026, 8, 31, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 25, "refund", "refund.maxRequestsPerUserPer30d", "Int", "3", "Giới hạn số yêu cầu hoàn / người thụ hưởng / 30 ngày trượt", new DateTime(2026, 8, 31, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 26, "refund", "refund.maxPaymentAgeDays", "Int", "180", "Không hoàn payment cũ hơn X ngày", new DateTime(2026, 8, 31, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 27, "refund", "refund.dualControlThresholdVnd", "Decimal", "0", ">= ngưỡng cần 2 người Finance duyệt; 0 = tắt", new DateTime(2026, 8, 31, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 28, "refund", "refund.timezoneOffsetHours", "Int", "7", "Mốc 'ngày' (Asia/Ho_Chi_Minh) để tính trần", new DateTime(2026, 8, 31, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 29, "refund", "refund.staleDisbursedDays", "Int", "3", "Disbursed quá X ngày chưa Completed -> cảnh báo Finance", new DateTime(2026, 8, 31, 0, 0, 0, 0, DateTimeKind.Utc), null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_RefundBatch_PublicId",
                table: "RefundBatch",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefundBatch_Status",
                table: "RefundBatch",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_RefundEvent_CreatedAt",
                table: "RefundEvent",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RefundEvent_RefundBatchId",
                table: "RefundEvent",
                column: "RefundBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_RefundEvent_RefundRequestId",
                table: "RefundEvent",
                column: "RefundRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_RefundRequest_BeneficiaryUserId",
                table: "RefundRequest",
                column: "BeneficiaryUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RefundRequest_PaymentId",
                table: "RefundRequest",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_RefundRequest_PublicId",
                table: "RefundRequest",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefundRequest_RefundBatchId",
                table: "RefundRequest",
                column: "RefundBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_RefundRequest_Status",
                table: "RefundRequest",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DataProtectionKeys");

            migrationBuilder.DropTable(
                name: "RefundEvent");

            migrationBuilder.DropTable(
                name: "RefundRequest");

            migrationBuilder.DropTable(
                name: "RefundBatch");

            migrationBuilder.DeleteData(
                table: "SystemConfig",
                keyColumn: "ConfigId",
                keyValue: 24);

            migrationBuilder.DeleteData(
                table: "SystemConfig",
                keyColumn: "ConfigId",
                keyValue: 25);

            migrationBuilder.DeleteData(
                table: "SystemConfig",
                keyColumn: "ConfigId",
                keyValue: 26);

            migrationBuilder.DeleteData(
                table: "SystemConfig",
                keyColumn: "ConfigId",
                keyValue: 27);

            migrationBuilder.DeleteData(
                table: "SystemConfig",
                keyColumn: "ConfigId",
                keyValue: 28);

            migrationBuilder.DeleteData(
                table: "SystemConfig",
                keyColumn: "ConfigId",
                keyValue: 29);
        }
    }
}
