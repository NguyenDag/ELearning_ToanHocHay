using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ELearning_ToanHocHay_Control.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CourseBundle",
                columns: table => new
                {
                    CourseBundleId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Slug = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    ListPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    SalePrice = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseBundle", x => x.CourseBundleId);
                });

            migrationBuilder.CreateTable(
                name: "CurriculumFramework",
                columns: table => new
                {
                    FrameworkId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Publisher = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurriculumFramework", x => x.FrameworkId);
                });

            migrationBuilder.CreateTable(
                name: "GradeLevel",
                columns: table => new
                {
                    GradeLevelId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Stage = table.Column<string>(type: "text", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GradeLevel", x => x.GradeLevelId);
                });

            migrationBuilder.CreateTable(
                name: "GuestIpUsage",
                columns: table => new
                {
                    IpHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    LessonViewCount = table.Column<int>(type: "integer", nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuestIpUsage", x => new { x.IpHash, x.Date });
                });

            migrationBuilder.CreateTable(
                name: "Promotion",
                columns: table => new
                {
                    PromotionId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    PromotionType = table.Column<string>(type: "text", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    DiscountKind = table.Column<string>(type: "text", nullable: false),
                    DiscountValue = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    MaxDiscountAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    StartAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    Stackable = table.Column<bool>(type: "boolean", nullable: false),
                    TotalUsageLimit = table.Column<int>(type: "integer", nullable: true),
                    PerUserLimit = table.Column<int>(type: "integer", nullable: false),
                    MinOrderAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    FirstPurchaseOnly = table.Column<bool>(type: "boolean", nullable: false),
                    ReservedCount = table.Column<int>(type: "integer", nullable: false),
                    ConfirmedCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedBy = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Promotion", x => x.PromotionId);
                });

            migrationBuilder.CreateTable(
                name: "StaticPage",
                columns: table => new
                {
                    StaticPageId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Slug = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    BodyHtml = table.Column<string>(type: "text", nullable: true),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaticPage", x => x.StaticPageId);
                });

            migrationBuilder.CreateTable(
                name: "Subject",
                columns: table => new
                {
                    SubjectId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IconUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ColorHex = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subject", x => x.SubjectId);
                });

            migrationBuilder.CreateTable(
                name: "Tag",
                columns: table => new
                {
                    TagId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TagName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TagType = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tag", x => x.TagId);
                });

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FullName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Dob = table.Column<DateOnly>(type: "date", nullable: true),
                    AvatarUrl = table.Column<string>(type: "text", nullable: true),
                    UserType = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsEmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    EmailConfirmedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastLogin = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LockedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LockedReason = table.Column<string>(type: "text", nullable: true),
                    LockedByUserId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "PromotionScope",
                columns: table => new
                {
                    PromotionScopeId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PromotionId = table.Column<int>(type: "integer", nullable: false),
                    ScopeType = table.Column<string>(type: "text", nullable: false),
                    SubjectId = table.Column<int>(type: "integer", nullable: true),
                    GradeLevelId = table.Column<int>(type: "integer", nullable: true),
                    CourseId = table.Column<int>(type: "integer", nullable: true),
                    PackageId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromotionScope", x => x.PromotionScopeId);
                    table.ForeignKey(
                        name: "FK_PromotionScope_Promotion_PromotionId",
                        column: x => x.PromotionId,
                        principalTable: "Promotion",
                        principalColumn: "PromotionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NodeTypeRule",
                columns: table => new
                {
                    NodeTypeRuleId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SubjectId = table.Column<int>(type: "integer", nullable: true),
                    ParentType = table.Column<string>(type: "text", nullable: true),
                    ChildType = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NodeTypeRule", x => x.NodeTypeRuleId);
                    table.ForeignKey(
                        name: "FK_NodeTypeRule_Subject_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subject",
                        principalColumn: "SubjectId");
                });

            migrationBuilder.CreateTable(
                name: "Skill",
                columns: table => new
                {
                    SkillId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SubjectId = table.Column<int>(type: "integer", nullable: false),
                    ParentSkillId = table.Column<int>(type: "integer", nullable: true),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Skill", x => x.SkillId);
                    table.ForeignKey(
                        name: "FK_Skill_Skill_ParentSkillId",
                        column: x => x.ParentSkillId,
                        principalTable: "Skill",
                        principalColumn: "SkillId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Skill_Subject_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subject",
                        principalColumn: "SubjectId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AuditLog",
                columns: table => new
                {
                    LogId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: true),
                    Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<int>(type: "integer", nullable: true),
                    OldValueJson = table.Column<string>(type: "text", nullable: true),
                    NewValueJson = table.Column<string>(type: "text", nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLog", x => x.LogId);
                    table.ForeignKey(
                        name: "FK_AuditLog_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Course",
                columns: table => new
                {
                    CourseId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SubjectId = table.Column<int>(type: "integer", nullable: false),
                    GradeLevelId = table.Column<int>(type: "integer", nullable: false),
                    FrameworkId = table.Column<int>(type: "integer", nullable: true),
                    Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Slug = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    ThumbnailUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ListPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    SalePrice = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    IsPurchasable = table.Column<bool>(type: "boolean", nullable: false),
                    AccessDurationDays = table.Column<int>(type: "integer", nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedBy = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Course", x => x.CourseId);
                    table.ForeignKey(
                        name: "FK_Course_CurriculumFramework_FrameworkId",
                        column: x => x.FrameworkId,
                        principalTable: "CurriculumFramework",
                        principalColumn: "FrameworkId");
                    table.ForeignKey(
                        name: "FK_Course_GradeLevel_GradeLevelId",
                        column: x => x.GradeLevelId,
                        principalTable: "GradeLevel",
                        principalColumn: "GradeLevelId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Course_Subject_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subject",
                        principalColumn: "SubjectId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Course_User_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmailVerificationToken",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Token = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ExpiredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsUsed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailVerificationToken", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailVerificationToken_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MediaAsset",
                columns: table => new
                {
                    MediaAssetId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StorageKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    MimeType = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    UploadedBy = table.Column<int>(type: "integer", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaAsset", x => x.MediaAssetId);
                    table.ForeignKey(
                        name: "FK_MediaAsset_User_UploadedBy",
                        column: x => x.UploadedBy,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Order",
                columns: table => new
                {
                    OrderId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BuyerUserId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    SubtotalAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Order", x => x.OrderId);
                    table.ForeignKey(
                        name: "FK_Order_User_BuyerUserId",
                        column: x => x.BuyerUserId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Package",
                columns: table => new
                {
                    PackageId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    PackageName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Tier = table.Column<string>(type: "text", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DurationDays = table.Column<int>(type: "integer", nullable: false),
                    MaxMembers = table.Column<int>(type: "integer", nullable: true),
                    AiHintLimitDaily = table.Column<int>(type: "integer", nullable: true),
                    UnlimitedAiHint = table.Column<bool>(type: "boolean", nullable: false),
                    PersonalizedPath = table.Column<bool>(type: "boolean", nullable: false),
                    MistakeRetry = table.Column<bool>(type: "boolean", nullable: false),
                    SmartReminder = table.Column<bool>(type: "boolean", nullable: false),
                    PrioritySupport = table.Column<bool>(type: "boolean", nullable: false),
                    FeaturesJson = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Package", x => x.PackageId);
                    table.ForeignKey(
                        name: "FK_Package_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Parent",
                columns: table => new
                {
                    ParentId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Job = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ConnectionCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parent", x => x.ParentId);
                    table.ForeignKey(
                        name: "FK_Parent_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Student",
                columns: table => new
                {
                    StudentId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    CurrentGradeLevelId = table.Column<int>(type: "integer", nullable: true),
                    SchoolName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AiDataSharingLevel = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Student", x => x.StudentId);
                    table.ForeignKey(
                        name: "FK_Student_GradeLevel_CurrentGradeLevelId",
                        column: x => x.CurrentGradeLevelId,
                        principalTable: "GradeLevel",
                        principalColumn: "GradeLevelId");
                    table.ForeignKey(
                        name: "FK_Student_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SystemConfig",
                columns: table => new
                {
                    ConfigId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ConfigKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ConfigValue = table.Column<string>(type: "text", nullable: true),
                    ConfigType = table.Column<string>(type: "text", nullable: false),
                    ConfigGroup = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemConfig", x => x.ConfigId);
                    table.ForeignKey(
                        name: "FK_SystemConfig_User_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "User",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "CourseBundleItem",
                columns: table => new
                {
                    CourseBundleId = table.Column<int>(type: "integer", nullable: false),
                    CourseId = table.Column<int>(type: "integer", nullable: false),
                    OrderIndex = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseBundleItem", x => new { x.CourseBundleId, x.CourseId });
                    table.ForeignKey(
                        name: "FK_CourseBundleItem_CourseBundle_CourseBundleId",
                        column: x => x.CourseBundleId,
                        principalTable: "CourseBundle",
                        principalColumn: "CourseBundleId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CourseBundleItem_Course_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Course",
                        principalColumn: "CourseId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CourseVersion",
                columns: table => new
                {
                    CourseVersionId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CourseId = table.Column<int>(type: "integer", nullable: false),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    Label = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    State = table.Column<string>(type: "text", nullable: false),
                    SubmittedBy = table.Column<int>(type: "integer", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PublishedBy = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseVersion", x => x.CourseVersionId);
                    table.ForeignKey(
                        name: "FK_CourseVersion_Course_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Course",
                        principalColumn: "CourseId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CourseVersion_User_PublishedBy",
                        column: x => x.PublishedBy,
                        principalTable: "User",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_CourseVersion_User_SubmittedBy",
                        column: x => x.SubmittedBy,
                        principalTable: "User",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "PromotionRedemption",
                columns: table => new
                {
                    RedemptionId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PromotionId = table.Column<int>(type: "integer", nullable: false),
                    OrderId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    RedeemedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConfirmedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VoidedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromotionRedemption", x => x.RedemptionId);
                    table.ForeignKey(
                        name: "FK_PromotionRedemption_Order_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Order",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PromotionRedemption_Promotion_PromotionId",
                        column: x => x.PromotionId,
                        principalTable: "Promotion",
                        principalColumn: "PromotionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PromotionRedemption_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PackageEntitlement",
                columns: table => new
                {
                    PackageEntitlementId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PackageId = table.Column<int>(type: "integer", nullable: false),
                    ScopeType = table.Column<string>(type: "text", nullable: false),
                    SubjectId = table.Column<int>(type: "integer", nullable: true),
                    GradeLevelId = table.Column<int>(type: "integer", nullable: true),
                    CourseId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackageEntitlement", x => x.PackageEntitlementId);
                    table.ForeignKey(
                        name: "FK_PackageEntitlement_Course_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Course",
                        principalColumn: "CourseId");
                    table.ForeignKey(
                        name: "FK_PackageEntitlement_GradeLevel_GradeLevelId",
                        column: x => x.GradeLevelId,
                        principalTable: "GradeLevel",
                        principalColumn: "GradeLevelId");
                    table.ForeignKey(
                        name: "FK_PackageEntitlement_Package_PackageId",
                        column: x => x.PackageId,
                        principalTable: "Package",
                        principalColumn: "PackageId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PackageEntitlement_Subject_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subject",
                        principalColumn: "SubjectId");
                });

            migrationBuilder.CreateTable(
                name: "ChatConversation",
                columns: table => new
                {
                    ConversationId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InitiatorUserId = table.Column<int>(type: "integer", nullable: false),
                    StudentId = table.Column<int>(type: "integer", nullable: true),
                    Topic = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    AssignedStaffId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatConversation", x => x.ConversationId);
                    table.ForeignKey(
                        name: "FK_ChatConversation_Student_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Student",
                        principalColumn: "StudentId");
                    table.ForeignKey(
                        name: "FK_ChatConversation_User_AssignedStaffId",
                        column: x => x.AssignedStaffId,
                        principalTable: "User",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_ChatConversation_User_InitiatorUserId",
                        column: x => x.InitiatorUserId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DailyActivitySnapshot",
                columns: table => new
                {
                    StudentId = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    MinutesStudied = table.Column<int>(type: "integer", nullable: false),
                    ExercisesDone = table.Column<int>(type: "integer", nullable: false),
                    LessonsDone = table.Column<int>(type: "integer", nullable: false),
                    QuestionsAnswered = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyActivitySnapshot", x => new { x.StudentId, x.Date });
                    table.ForeignKey(
                        name: "FK_DailyActivitySnapshot_Student_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Student",
                        principalColumn: "StudentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GuestSession",
                columns: table => new
                {
                    GuestSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    GradeLevelId = table.Column<int>(type: "integer", nullable: true),
                    LessonViewCount = table.Column<int>(type: "integer", nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    ConvertedToStudentId = table.Column<int>(type: "integer", nullable: true),
                    ConvertedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuestSession", x => x.GuestSessionId);
                    table.ForeignKey(
                        name: "FK_GuestSession_GradeLevel_GradeLevelId",
                        column: x => x.GradeLevelId,
                        principalTable: "GradeLevel",
                        principalColumn: "GradeLevelId");
                    table.ForeignKey(
                        name: "FK_GuestSession_Student_ConvertedToStudentId",
                        column: x => x.ConvertedToStudentId,
                        principalTable: "Student",
                        principalColumn: "StudentId");
                });

            migrationBuilder.CreateTable(
                name: "LearningPath",
                columns: table => new
                {
                    PathId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StudentId = table.Column<int>(type: "integer", nullable: false),
                    RecommendedTopicsJson = table.Column<string>(type: "text", nullable: true),
                    WeakAreasJson = table.Column<string>(type: "text", nullable: true),
                    StrongAreasJson = table.Column<string>(type: "text", nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsPersonalized = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningPath", x => x.PathId);
                    table.ForeignKey(
                        name: "FK_LearningPath_Student_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Student",
                        principalColumn: "StudentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notification",
                columns: table => new
                {
                    NotificationId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    StudentId = table.Column<int>(type: "integer", nullable: true),
                    Audience = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    NotificationType = table.Column<string>(type: "text", nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notification", x => x.NotificationId);
                    table.ForeignKey(
                        name: "FK_Notification_Student_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Student",
                        principalColumn: "StudentId");
                    table.ForeignKey(
                        name: "FK_Notification_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderItem",
                columns: table => new
                {
                    OrderItemId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderId = table.Column<int>(type: "integer", nullable: false),
                    ItemType = table.Column<string>(type: "text", nullable: false),
                    CourseId = table.Column<int>(type: "integer", nullable: true),
                    PackageId = table.Column<int>(type: "integer", nullable: true),
                    CourseBundleId = table.Column<int>(type: "integer", nullable: true),
                    BeneficiaryStudentId = table.Column<int>(type: "integer", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItem", x => x.OrderItemId);
                    table.ForeignKey(
                        name: "FK_OrderItem_CourseBundle_CourseBundleId",
                        column: x => x.CourseBundleId,
                        principalTable: "CourseBundle",
                        principalColumn: "CourseBundleId");
                    table.ForeignKey(
                        name: "FK_OrderItem_Course_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Course",
                        principalColumn: "CourseId");
                    table.ForeignKey(
                        name: "FK_OrderItem_Order_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Order",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderItem_Package_PackageId",
                        column: x => x.PackageId,
                        principalTable: "Package",
                        principalColumn: "PackageId");
                    table.ForeignKey(
                        name: "FK_OrderItem_Student_BeneficiaryStudentId",
                        column: x => x.BeneficiaryStudentId,
                        principalTable: "Student",
                        principalColumn: "StudentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ParentInvite",
                columns: table => new
                {
                    ParentInviteId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ParentId = table.Column<int>(type: "integer", nullable: false),
                    InviteeEmail = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AcceptedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AcceptedByStudentId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParentInvite", x => x.ParentInviteId);
                    table.ForeignKey(
                        name: "FK_ParentInvite_Parent_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Parent",
                        principalColumn: "ParentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ParentInvite_Student_AcceptedByStudentId",
                        column: x => x.AcceptedByStudentId,
                        principalTable: "Student",
                        principalColumn: "StudentId");
                });

            migrationBuilder.CreateTable(
                name: "ParentLink",
                columns: table => new
                {
                    ParentLinkId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ParentId = table.Column<int>(type: "integer", nullable: false),
                    StudentId = table.Column<int>(type: "integer", nullable: false),
                    Relationship = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    IsPrimaryGuardian = table.Column<bool>(type: "boolean", nullable: false),
                    LinkedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParentLink", x => x.ParentLinkId);
                    table.ForeignKey(
                        name: "FK_ParentLink_Parent_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Parent",
                        principalColumn: "ParentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ParentLink_Student_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Student",
                        principalColumn: "StudentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Payment",
                columns: table => new
                {
                    PaymentId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderId = table.Column<int>(type: "integer", nullable: true),
                    PaidByUserId = table.Column<int>(type: "integer", nullable: false),
                    StudentId = table.Column<int>(type: "integer", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PaymentMethod = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TransactionId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    RefundedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RefundAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payment", x => x.PaymentId);
                    table.ForeignKey(
                        name: "FK_Payment_Order_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Order",
                        principalColumn: "OrderId");
                    table.ForeignKey(
                        name: "FK_Payment_Student_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Student",
                        principalColumn: "StudentId");
                    table.ForeignKey(
                        name: "FK_Payment_User_PaidByUserId",
                        column: x => x.PaidByUserId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SkillProgress",
                columns: table => new
                {
                    SkillProgressId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StudentId = table.Column<int>(type: "integer", nullable: false),
                    SkillId = table.Column<int>(type: "integer", nullable: false),
                    MasteryScore = table.Column<decimal>(type: "numeric(4,3)", nullable: false),
                    LastAssessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillProgress", x => x.SkillProgressId);
                    table.ForeignKey(
                        name: "FK_SkillProgress_Skill_SkillId",
                        column: x => x.SkillId,
                        principalTable: "Skill",
                        principalColumn: "SkillId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SkillProgress_Student_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Student",
                        principalColumn: "StudentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContentImportJob",
                columns: table => new
                {
                    ImportJobId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UploadedBy = table.Column<int>(type: "integer", nullable: false),
                    FileUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    TargetType = table.Column<string>(type: "text", nullable: false),
                    CourseVersionId = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    TotalRows = table.Column<int>(type: "integer", nullable: false),
                    SuccessRows = table.Column<int>(type: "integer", nullable: false),
                    ErrorReport = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentImportJob", x => x.ImportJobId);
                    table.ForeignKey(
                        name: "FK_ContentImportJob_CourseVersion_CourseVersionId",
                        column: x => x.CourseVersionId,
                        principalTable: "CourseVersion",
                        principalColumn: "CourseVersionId");
                    table.ForeignKey(
                        name: "FK_ContentImportJob_User_UploadedBy",
                        column: x => x.UploadedBy,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContentNode",
                columns: table => new
                {
                    NodeId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CourseVersionId = table.Column<int>(type: "integer", nullable: false),
                    ParentNodeId = table.Column<int>(type: "integer", nullable: true),
                    NodeType = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Slug = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    OrderIndex = table.Column<int>(type: "integer", nullable: false),
                    Depth = table.Column<int>(type: "integer", nullable: false),
                    MaterializedPath = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    IsFree = table.Column<bool>(type: "boolean", nullable: false),
                    IsHidden = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<int>(type: "integer", nullable: false),
                    UpdatedBy = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentNode", x => x.NodeId);
                    table.CheckConstraint("CK_ContentNode_Depth", "\"Depth\" >= 0");
                    table.ForeignKey(
                        name: "FK_ContentNode_ContentNode_ParentNodeId",
                        column: x => x.ParentNodeId,
                        principalTable: "ContentNode",
                        principalColumn: "NodeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContentNode_CourseVersion_CourseVersionId",
                        column: x => x.CourseVersionId,
                        principalTable: "CourseVersion",
                        principalColumn: "CourseVersionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContentReview",
                columns: table => new
                {
                    ReviewId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CourseVersionId = table.Column<int>(type: "integer", nullable: false),
                    ReviewerId = table.Column<int>(type: "integer", nullable: false),
                    Decision = table.Column<string>(type: "text", nullable: false),
                    Summary = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentReview", x => x.ReviewId);
                    table.ForeignKey(
                        name: "FK_ContentReview_CourseVersion_CourseVersionId",
                        column: x => x.CourseVersionId,
                        principalTable: "CourseVersion",
                        principalColumn: "CourseVersionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContentReview_User_ReviewerId",
                        column: x => x.ReviewerId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudentCourse",
                columns: table => new
                {
                    StudentCourseId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StudentId = table.Column<int>(type: "integer", nullable: false),
                    CourseId = table.Column<int>(type: "integer", nullable: false),
                    CourseVersionId = table.Column<int>(type: "integer", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ProgressPercent = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    EnrolledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AccessExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentCourse", x => x.StudentCourseId);
                    table.ForeignKey(
                        name: "FK_StudentCourse_CourseVersion_CourseVersionId",
                        column: x => x.CourseVersionId,
                        principalTable: "CourseVersion",
                        principalColumn: "CourseVersionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudentCourse_Course_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Course",
                        principalColumn: "CourseId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudentCourse_Student_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Student",
                        principalColumn: "StudentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChatMessage",
                columns: table => new
                {
                    MessageId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ConversationId = table.Column<int>(type: "integer", nullable: false),
                    SenderType = table.Column<string>(type: "text", nullable: false),
                    SenderUserId = table.Column<int>(type: "integer", nullable: true),
                    Body = table.Column<string>(type: "text", nullable: false),
                    MetadataJson = table.Column<string>(type: "text", nullable: true),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessage", x => x.MessageId);
                    table.ForeignKey(
                        name: "FK_ChatMessage_ChatConversation_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "ChatConversation",
                        principalColumn: "ConversationId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChatMessage_User_SenderUserId",
                        column: x => x.SenderUserId,
                        principalTable: "User",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "SupportTicket",
                columns: table => new
                {
                    TicketId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CreatedByUserId = table.Column<int>(type: "integer", nullable: false),
                    AssignedToStaffId = table.Column<int>(type: "integer", nullable: true),
                    ConversationId = table.Column<int>(type: "integer", nullable: true),
                    Subject = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Priority = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportTicket", x => x.TicketId);
                    table.ForeignKey(
                        name: "FK_SupportTicket_ChatConversation_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "ChatConversation",
                        principalColumn: "ConversationId");
                    table.ForeignKey(
                        name: "FK_SupportTicket_User_AssignedToStaffId",
                        column: x => x.AssignedToStaffId,
                        principalTable: "User",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_SupportTicket_User_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Subscription",
                columns: table => new
                {
                    SubscriptionId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StudentId = table.Column<int>(type: "integer", nullable: true),
                    PackageId = table.Column<int>(type: "integer", nullable: false),
                    PaymentId = table.Column<int>(type: "integer", nullable: true),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    AmountPaid = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subscription", x => x.SubscriptionId);
                    table.ForeignKey(
                        name: "FK_Subscription_Package_PackageId",
                        column: x => x.PackageId,
                        principalTable: "Package",
                        principalColumn: "PackageId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Subscription_Payment_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "Payment",
                        principalColumn: "PaymentId");
                    table.ForeignKey(
                        name: "FK_Subscription_Student_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Student",
                        principalColumn: "StudentId");
                });

            migrationBuilder.CreateTable(
                name: "ContentBlock",
                columns: table => new
                {
                    BlockId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NodeId = table.Column<int>(type: "integer", nullable: false),
                    BlockType = table.Column<string>(type: "text", nullable: false),
                    ContentText = table.Column<string>(type: "text", nullable: true),
                    ContentUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    MetadataJson = table.Column<string>(type: "text", nullable: true),
                    OrderIndex = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentBlock", x => x.BlockId);
                    table.ForeignKey(
                        name: "FK_ContentBlock_ContentNode_NodeId",
                        column: x => x.NodeId,
                        principalTable: "ContentNode",
                        principalColumn: "NodeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Exercise",
                columns: table => new
                {
                    ExerciseId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NodeId = table.Column<int>(type: "integer", nullable: true),
                    ExerciseName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ExerciseType = table.Column<string>(type: "text", nullable: false),
                    TotalQuestions = table.Column<int>(type: "integer", nullable: false),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: true),
                    MaxAttempts = table.Column<int>(type: "integer", nullable: true),
                    IsFree = table.Column<bool>(type: "boolean", nullable: false),
                    RequiredTier = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    TotalScores = table.Column<double>(type: "double precision", nullable: false),
                    PassingScore = table.Column<double>(type: "double precision", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedBy = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Exercise", x => x.ExerciseId);
                    table.ForeignKey(
                        name: "FK_Exercise_ContentNode_NodeId",
                        column: x => x.NodeId,
                        principalTable: "ContentNode",
                        principalColumn: "NodeId");
                    table.ForeignKey(
                        name: "FK_Exercise_User_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FlashcardDeck",
                columns: table => new
                {
                    DeckId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NodeId = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlashcardDeck", x => x.DeckId);
                    table.ForeignKey(
                        name: "FK_FlashcardDeck_ContentNode_NodeId",
                        column: x => x.NodeId,
                        principalTable: "ContentNode",
                        principalColumn: "NodeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LessonDetail",
                columns: table => new
                {
                    NodeId = table.Column<int>(type: "integer", nullable: false),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LessonDetail", x => x.NodeId);
                    table.ForeignKey(
                        name: "FK_LessonDetail_ContentNode_NodeId",
                        column: x => x.NodeId,
                        principalTable: "ContentNode",
                        principalColumn: "NodeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LessonResource",
                columns: table => new
                {
                    ResourceId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NodeId = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ResourceType = table.Column<string>(type: "text", nullable: false),
                    MediaAssetId = table.Column<int>(type: "integer", nullable: true),
                    ExternalUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsDownloadable = table.Column<bool>(type: "boolean", nullable: false),
                    OrderIndex = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LessonResource", x => x.ResourceId);
                    table.ForeignKey(
                        name: "FK_LessonResource_ContentNode_NodeId",
                        column: x => x.NodeId,
                        principalTable: "ContentNode",
                        principalColumn: "NodeId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LessonResource_MediaAsset_MediaAssetId",
                        column: x => x.MediaAssetId,
                        principalTable: "MediaAsset",
                        principalColumn: "MediaAssetId");
                });

            migrationBuilder.CreateTable(
                name: "NodeProgress",
                columns: table => new
                {
                    NodeProgressId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StudentId = table.Column<int>(type: "integer", nullable: false),
                    NodeId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    MasteryLevel = table.Column<string>(type: "text", nullable: false),
                    CompletionPercent = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    TimeSpentSeconds = table.Column<int>(type: "integer", nullable: false),
                    TotalAttempts = table.Column<int>(type: "integer", nullable: false),
                    CorrectCount = table.Column<int>(type: "integer", nullable: false),
                    WrongCount = table.Column<int>(type: "integer", nullable: false),
                    CommonMistakesJson = table.Column<string>(type: "text", nullable: true),
                    LastAccessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NodeProgress", x => x.NodeProgressId);
                    table.ForeignKey(
                        name: "FK_NodeProgress_ContentNode_NodeId",
                        column: x => x.NodeId,
                        principalTable: "ContentNode",
                        principalColumn: "NodeId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NodeProgress_Student_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Student",
                        principalColumn: "StudentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NodeRevision",
                columns: table => new
                {
                    RevisionId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NodeId = table.Column<int>(type: "integer", nullable: false),
                    RevisionNumber = table.Column<int>(type: "integer", nullable: false),
                    Snapshot = table.Column<string>(type: "text", nullable: true),
                    EditedBy = table.Column<int>(type: "integer", nullable: false),
                    EditedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NodeRevision", x => x.RevisionId);
                    table.ForeignKey(
                        name: "FK_NodeRevision_ContentNode_NodeId",
                        column: x => x.NodeId,
                        principalTable: "ContentNode",
                        principalColumn: "NodeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NodeSkill",
                columns: table => new
                {
                    NodeId = table.Column<int>(type: "integer", nullable: false),
                    SkillId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NodeSkill", x => new { x.NodeId, x.SkillId });
                    table.ForeignKey(
                        name: "FK_NodeSkill_ContentNode_NodeId",
                        column: x => x.NodeId,
                        principalTable: "ContentNode",
                        principalColumn: "NodeId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NodeSkill_Skill_SkillId",
                        column: x => x.SkillId,
                        principalTable: "Skill",
                        principalColumn: "SkillId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuestionBank",
                columns: table => new
                {
                    BankId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BankName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    SubjectId = table.Column<int>(type: "integer", nullable: false),
                    GradeLevelId = table.Column<int>(type: "integer", nullable: false),
                    CourseId = table.Column<int>(type: "integer", nullable: true),
                    PrimaryNodeId = table.Column<int>(type: "integer", nullable: true),
                    CreatedBy = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionBank", x => x.BankId);
                    table.ForeignKey(
                        name: "FK_QuestionBank_ContentNode_PrimaryNodeId",
                        column: x => x.PrimaryNodeId,
                        principalTable: "ContentNode",
                        principalColumn: "NodeId");
                    table.ForeignKey(
                        name: "FK_QuestionBank_Course_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Course",
                        principalColumn: "CourseId");
                    table.ForeignKey(
                        name: "FK_QuestionBank_GradeLevel_GradeLevelId",
                        column: x => x.GradeLevelId,
                        principalTable: "GradeLevel",
                        principalColumn: "GradeLevelId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QuestionBank_Subject_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subject",
                        principalColumn: "SubjectId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QuestionBank_User_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "User",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "SupportMessage",
                columns: table => new
                {
                    MessageId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TicketId = table.Column<int>(type: "integer", nullable: false),
                    SenderUserId = table.Column<int>(type: "integer", nullable: false),
                    MessageText = table.Column<string>(type: "text", nullable: false),
                    IsInternalNote = table.Column<bool>(type: "boolean", nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportMessage", x => x.MessageId);
                    table.ForeignKey(
                        name: "FK_SupportMessage_SupportTicket_TicketId",
                        column: x => x.TicketId,
                        principalTable: "SupportTicket",
                        principalColumn: "TicketId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SupportMessage_User_SenderUserId",
                        column: x => x.SenderUserId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionMember",
                columns: table => new
                {
                    SubscriptionMemberId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SubscriptionId = table.Column<int>(type: "integer", nullable: false),
                    StudentId = table.Column<int>(type: "integer", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RemovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionMember", x => x.SubscriptionMemberId);
                    table.ForeignKey(
                        name: "FK_SubscriptionMember_Student_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Student",
                        principalColumn: "StudentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SubscriptionMember_Subscription_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalTable: "Subscription",
                        principalColumn: "SubscriptionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReviewComment",
                columns: table => new
                {
                    CommentId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ReviewId = table.Column<int>(type: "integer", nullable: false),
                    NodeId = table.Column<int>(type: "integer", nullable: true),
                    BlockId = table.Column<int>(type: "integer", nullable: true),
                    Body = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ResolvedBy = table.Column<int>(type: "integer", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReviewComment", x => x.CommentId);
                    table.ForeignKey(
                        name: "FK_ReviewComment_ContentBlock_BlockId",
                        column: x => x.BlockId,
                        principalTable: "ContentBlock",
                        principalColumn: "BlockId");
                    table.ForeignKey(
                        name: "FK_ReviewComment_ContentNode_NodeId",
                        column: x => x.NodeId,
                        principalTable: "ContentNode",
                        principalColumn: "NodeId");
                    table.ForeignKey(
                        name: "FK_ReviewComment_ContentReview_ReviewId",
                        column: x => x.ReviewId,
                        principalTable: "ContentReview",
                        principalColumn: "ReviewId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExerciseAttempt",
                columns: table => new
                {
                    AttemptId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StudentId = table.Column<int>(type: "integer", nullable: true),
                    GuestSessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExerciseId = table.Column<int>(type: "integer", nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PlannedEndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    TotalScore = table.Column<double>(type: "double precision", nullable: false),
                    MaxScore = table.Column<double>(type: "double precision", nullable: false),
                    CompletionPercentage = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CorrectAnswers = table.Column<int>(type: "integer", nullable: false),
                    WrongAnswers = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExerciseAttempt", x => x.AttemptId);
                    table.CheckConstraint("CK_ExerciseAttempt_Owner", "(\"StudentId\" IS NOT NULL AND \"GuestSessionId\" IS NULL) OR (\"StudentId\" IS NULL AND \"GuestSessionId\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_ExerciseAttempt_Exercise_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "Exercise",
                        principalColumn: "ExerciseId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExerciseAttempt_GuestSession_GuestSessionId",
                        column: x => x.GuestSessionId,
                        principalTable: "GuestSession",
                        principalColumn: "GuestSessionId");
                    table.ForeignKey(
                        name: "FK_ExerciseAttempt_Student_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Student",
                        principalColumn: "StudentId");
                });

            migrationBuilder.CreateTable(
                name: "Flashcard",
                columns: table => new
                {
                    CardId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DeckId = table.Column<int>(type: "integer", nullable: false),
                    FrontText = table.Column<string>(type: "text", nullable: false),
                    BackText = table.Column<string>(type: "text", nullable: false),
                    FrontImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BackImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Hint = table.Column<string>(type: "text", nullable: true),
                    OrderIndex = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Flashcard", x => x.CardId);
                    table.ForeignKey(
                        name: "FK_Flashcard_FlashcardDeck_DeckId",
                        column: x => x.DeckId,
                        principalTable: "FlashcardDeck",
                        principalColumn: "DeckId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Question",
                columns: table => new
                {
                    QuestionId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BankId = table.Column<int>(type: "integer", nullable: false),
                    SubjectId = table.Column<int>(type: "integer", nullable: false),
                    QuestionText = table.Column<string>(type: "text", nullable: false),
                    QuestionImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    QuestionType = table.Column<string>(type: "text", nullable: false),
                    DifficultyLevel = table.Column<string>(type: "text", nullable: false),
                    CorrectAnswer = table.Column<string>(type: "text", nullable: true),
                    Explanation = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<int>(type: "integer", nullable: false),
                    ReviewedBy = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RejectReason = table.Column<string>(type: "text", nullable: true),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Question", x => x.QuestionId);
                    table.ForeignKey(
                        name: "FK_Question_QuestionBank_BankId",
                        column: x => x.BankId,
                        principalTable: "QuestionBank",
                        principalColumn: "BankId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Question_Subject_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subject",
                        principalColumn: "SubjectId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Question_User_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Question_User_ReviewedBy",
                        column: x => x.ReviewedBy,
                        principalTable: "User",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "TabSwitchLog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AttemptId = table.Column<int>(type: "integer", nullable: false),
                    SwitchedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TabSwitchLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TabSwitchLog_ExerciseAttempt_AttemptId",
                        column: x => x.AttemptId,
                        principalTable: "ExerciseAttempt",
                        principalColumn: "AttemptId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AIFeedback",
                columns: table => new
                {
                    FeedbackId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AttemptId = table.Column<int>(type: "integer", nullable: false),
                    QuestionId = table.Column<int>(type: "integer", nullable: false),
                    FullSolution = table.Column<string>(type: "text", nullable: true),
                    MistakeAnalysis = table.Column<string>(type: "text", nullable: true),
                    ImprovementAdvice = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIFeedback", x => x.FeedbackId);
                    table.ForeignKey(
                        name: "FK_AIFeedback_ExerciseAttempt_AttemptId",
                        column: x => x.AttemptId,
                        principalTable: "ExerciseAttempt",
                        principalColumn: "AttemptId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AIFeedback_Question_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Question",
                        principalColumn: "QuestionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AIHint",
                columns: table => new
                {
                    HintId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AttemptId = table.Column<int>(type: "integer", nullable: false),
                    QuestionId = table.Column<int>(type: "integer", nullable: false),
                    HintText = table.Column<string>(type: "text", nullable: true),
                    HintLevel = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIHint", x => x.HintId);
                    table.ForeignKey(
                        name: "FK_AIHint_ExerciseAttempt_AttemptId",
                        column: x => x.AttemptId,
                        principalTable: "ExerciseAttempt",
                        principalColumn: "AttemptId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AIHint_Question_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Question",
                        principalColumn: "QuestionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExerciseQuestion",
                columns: table => new
                {
                    ExerciseId = table.Column<int>(type: "integer", nullable: false),
                    QuestionId = table.Column<int>(type: "integer", nullable: false),
                    Score = table.Column<double>(type: "double precision", nullable: false),
                    OrderIndex = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExerciseQuestion", x => new { x.ExerciseId, x.QuestionId });
                    table.ForeignKey(
                        name: "FK_ExerciseQuestion_Exercise_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "Exercise",
                        principalColumn: "ExerciseId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExerciseQuestion_Question_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Question",
                        principalColumn: "QuestionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuestionNode",
                columns: table => new
                {
                    QuestionId = table.Column<int>(type: "integer", nullable: false),
                    NodeId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionNode", x => new { x.QuestionId, x.NodeId });
                    table.ForeignKey(
                        name: "FK_QuestionNode_ContentNode_NodeId",
                        column: x => x.NodeId,
                        principalTable: "ContentNode",
                        principalColumn: "NodeId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QuestionNode_Question_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Question",
                        principalColumn: "QuestionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuestionOption",
                columns: table => new
                {
                    OptionId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    QuestionId = table.Column<int>(type: "integer", nullable: false),
                    OptionText = table.Column<string>(type: "text", nullable: false),
                    ImageUrl = table.Column<string>(type: "text", nullable: true),
                    IsCorrect = table.Column<bool>(type: "boolean", nullable: false),
                    OrderIndex = table.Column<int>(type: "integer", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "QuestionSkill",
                columns: table => new
                {
                    QuestionId = table.Column<int>(type: "integer", nullable: false),
                    SkillId = table.Column<int>(type: "integer", nullable: false),
                    Weight = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionSkill", x => new { x.QuestionId, x.SkillId });
                    table.ForeignKey(
                        name: "FK_QuestionSkill_Question_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Question",
                        principalColumn: "QuestionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QuestionSkill_Skill_SkillId",
                        column: x => x.SkillId,
                        principalTable: "Skill",
                        principalColumn: "SkillId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuestionTag",
                columns: table => new
                {
                    QuestionId = table.Column<int>(type: "integer", nullable: false),
                    TagId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionTag", x => new { x.QuestionId, x.TagId });
                    table.ForeignKey(
                        name: "FK_QuestionTag_Question_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Question",
                        principalColumn: "QuestionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QuestionTag_Tag_TagId",
                        column: x => x.TagId,
                        principalTable: "Tag",
                        principalColumn: "TagId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudentAnswer",
                columns: table => new
                {
                    AnswerId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AttemptId = table.Column<int>(type: "integer", nullable: false),
                    QuestionId = table.Column<int>(type: "integer", nullable: false),
                    AnswerText = table.Column<string>(type: "text", nullable: true),
                    SelectedOptionId = table.Column<int>(type: "integer", nullable: true),
                    IsCorrect = table.Column<bool>(type: "boolean", nullable: false),
                    PointsEarned = table.Column<double>(type: "double precision", nullable: false),
                    AnsweredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentAnswer", x => x.AnswerId);
                    table.ForeignKey(
                        name: "FK_StudentAnswer_ExerciseAttempt_AttemptId",
                        column: x => x.AttemptId,
                        principalTable: "ExerciseAttempt",
                        principalColumn: "AttemptId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudentAnswer_QuestionOption_SelectedOptionId",
                        column: x => x.SelectedOptionId,
                        principalTable: "QuestionOption",
                        principalColumn: "OptionId");
                    table.ForeignKey(
                        name: "FK_StudentAnswer_Question_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Question",
                        principalColumn: "QuestionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "CurriculumFramework",
                columns: new[] { "FrameworkId", "Code", "IsActive", "Name", "Publisher" },
                values: new object[,]
                {
                    { 1, "KNTT", true, "Kết nối tri thức với cuộc sống", "NXB Giáo dục Việt Nam" },
                    { 2, "CTST", true, "Chân trời sáng tạo", "NXB Giáo dục Việt Nam" },
                    { 3, "CD", true, "Cánh Diều", "Liên danh ĐHSP / VEPIC" }
                });

            migrationBuilder.InsertData(
                table: "GradeLevel",
                columns: new[] { "GradeLevelId", "Code", "DisplayOrder", "IsActive", "Name", "Stage" },
                values: new object[,]
                {
                    { 1, "G6", 6, true, "Lớp 6", "LowerSecondary" },
                    { 2, "G7", 7, false, "Lớp 7", "LowerSecondary" },
                    { 3, "G8", 8, false, "Lớp 8", "LowerSecondary" },
                    { 4, "G9", 9, false, "Lớp 9", "LowerSecondary" },
                    { 5, "EXAM10", 10, false, "Ôn thi vào 10", "ExamPrep" }
                });

            migrationBuilder.InsertData(
                table: "NodeTypeRule",
                columns: new[] { "NodeTypeRuleId", "ChildType", "ParentType", "SubjectId" },
                values: new object[,]
                {
                    { 1, "Chapter", null, null },
                    { 2, "Topic", "Chapter", null },
                    { 3, "Lesson", "Chapter", null },
                    { 4, "SubTopic", "Topic", null },
                    { 5, "Lesson", "Topic", null },
                    { 6, "Lesson", "SubTopic", null }
                });

            migrationBuilder.InsertData(
                table: "Subject",
                columns: new[] { "SubjectId", "Code", "ColorHex", "Description", "DisplayOrder", "IconUrl", "IsActive", "Name", "Slug" },
                values: new object[] { 1, "MATH", "#1f5fae", null, 1, null, true, "Toán", "toan" });

            migrationBuilder.InsertData(
                table: "SystemConfig",
                columns: new[] { "ConfigId", "ConfigGroup", "ConfigKey", "ConfigType", "ConfigValue", "Description", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { 1, "guest", "guest.maxFreeLessons", "Int", "5", "Xem N bài IsFree → tường mềm đăng ký", new DateTime(2026, 8, 31, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 2, "guest", "guest.maxAttempts", "Int", "5", "Lượt làm bài / session", new DateTime(2026, 8, 31, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 3, "guest", "guest.maxAttemptsPerIpPerDay", "Int", "50", "Rộng hơn để không khoá nhầm lớp NAT", new DateTime(2026, 8, 31, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 4, "guest", "guest.session.retentionDays", "Int", "90", "Dọn GuestSession chưa convert", new DateTime(2026, 8, 31, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 5, "guest", "guest.ipUsage.retentionDays", "Int", "60", "Dọn GuestIpUsage", new DateTime(2026, 8, 31, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 6, "content", "content.maxTreeDepth", "Int", "4", "Chapter → Topic → SubTopic → Lesson", new DateTime(2026, 8, 31, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 7, "content", "content.import.maxRowsPerJob", "Int", "2000", "Cap file import", new DateTime(2026, 8, 31, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 8, "content", "version.clone.timeoutSeconds", "Int", "120", "Timeout job clone version", new DateTime(2026, 8, 31, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 9, "exercise", "exercise.defaultMaxAttempts", "Int", "3", "Khi Exercise không tự đặt", new DateTime(2026, 8, 31, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 10, "exercise", "exercise.attempt.abandonTimeoutMinutes", "Int", "30", "InProgress quá hạn → Timeout", new DateTime(2026, 8, 31, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 11, "promo", "promo.reservation.ttlMinutes", "Int", "20", "Nhả PromotionRedemption Reserved quá hạn", new DateTime(2026, 8, 31, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 12, "notify", "notify.inactivity.days", "Int", "3", "Con nghỉ N ngày → báo phụ huynh", new DateTime(2026, 8, 31, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 13, "notify", "notify.lowScore.threshold", "Decimal", "5.0", "Điểm dưới X (thang 10)", new DateTime(2026, 8, 31, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 14, "notify", "notify.parentDigest.dayOfWeek", "String", "Monday", "Bản tổng hợp tuần", new DateTime(2026, 8, 31, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 15, "support", "support.phone", "String", "", "BẮT BUỘC đặt trước launch — số escalate", new DateTime(2026, 8, 31, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 16, "support", "support.chat.aiHandoffAfterTurns", "Int", "3", "AI thử N lượt → mời điện thoại/nhân viên", new DateTime(2026, 8, 31, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 17, "support", "support.ticket.slaFirstResponseHours", "Int", "24", null, new DateTime(2026, 8, 31, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 18, "ai", "ai.chat.parentContextMaxTier", "Int", "2", "Phụ huynh-trong-chat đọc tới tầng dữ liệu nào", new DateTime(2026, 8, 31, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 19, "ai", "ai.hint.dailyLimitFreeTier", "Int", "3", "Gói free", new DateTime(2026, 8, 31, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 20, "security", "ipHash.secretVersion", "Int", "1", "Con trỏ version; secret thật ở env var", new DateTime(2026, 8, 31, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 21, "security", "ipHash.rotationDays", "Int", "90", null, new DateTime(2026, 8, 31, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 22, "referral", "referral.qualifyingOrderMinAmount", "Int", "99000", "đồng", new DateTime(2026, 8, 31, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 23, "referral", "referral.maxQualifiedPerReferrerPer30Days", "Int", "10", null, new DateTime(2026, 8, 31, 0, 0, 0, 0, DateTimeKind.Utc), null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AIFeedback_AttemptId",
                table: "AIFeedback",
                column: "AttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_AIFeedback_QuestionId",
                table: "AIFeedback",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_AIHint_AttemptId",
                table: "AIHint",
                column: "AttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_AIHint_QuestionId",
                table: "AIHint",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_EntityType_EntityId_CreatedAt",
                table: "AuditLog",
                columns: new[] { "EntityType", "EntityId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_UserId_CreatedAt",
                table: "AuditLog",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatConversation_AssignedStaffId",
                table: "ChatConversation",
                column: "AssignedStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatConversation_InitiatorUserId",
                table: "ChatConversation",
                column: "InitiatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatConversation_StudentId",
                table: "ChatConversation",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessage_ConversationId_SentAt",
                table: "ChatMessage",
                columns: new[] { "ConversationId", "SentAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessage_SenderUserId",
                table: "ChatMessage",
                column: "SenderUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentBlock_NodeId_OrderIndex",
                table: "ContentBlock",
                columns: new[] { "NodeId", "OrderIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_ContentImportJob_CourseVersionId",
                table: "ContentImportJob",
                column: "CourseVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentImportJob_UploadedBy",
                table: "ContentImportJob",
                column: "UploadedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ContentNode_CourseVersionId_ParentNodeId_OrderIndex",
                table: "ContentNode",
                columns: new[] { "CourseVersionId", "ParentNodeId", "OrderIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_ContentNode_MaterializedPath",
                table: "ContentNode",
                column: "MaterializedPath");

            migrationBuilder.CreateIndex(
                name: "IX_ContentNode_ParentNodeId",
                table: "ContentNode",
                column: "ParentNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentReview_CourseVersionId",
                table: "ContentReview",
                column: "CourseVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentReview_ReviewerId",
                table: "ContentReview",
                column: "ReviewerId");

            migrationBuilder.CreateIndex(
                name: "IX_Course_CreatedBy",
                table: "Course",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Course_FrameworkId",
                table: "Course",
                column: "FrameworkId");

            migrationBuilder.CreateIndex(
                name: "IX_Course_GradeLevelId",
                table: "Course",
                column: "GradeLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_Course_Slug",
                table: "Course",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Course_SubjectId_GradeLevelId_FrameworkId",
                table: "Course",
                columns: new[] { "SubjectId", "GradeLevelId", "FrameworkId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseBundle_Slug",
                table: "CourseBundle",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseBundleItem_CourseId",
                table: "CourseBundleItem",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseVersion_CourseId",
                table: "CourseVersion",
                column: "CourseId",
                unique: true,
                filter: "\"State\" = 'Published'");

            migrationBuilder.CreateIndex(
                name: "IX_CourseVersion_CourseId_VersionNumber",
                table: "CourseVersion",
                columns: new[] { "CourseId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseVersion_PublishedBy",
                table: "CourseVersion",
                column: "PublishedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CourseVersion_SubmittedBy",
                table: "CourseVersion",
                column: "SubmittedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumFramework_Code",
                table: "CurriculumFramework",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailVerificationToken_Token",
                table: "EmailVerificationToken",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailVerificationToken_UserId",
                table: "EmailVerificationToken",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Exercise_CreatedBy",
                table: "Exercise",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Exercise_NodeId",
                table: "Exercise",
                column: "NodeId");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseAttempt_ExerciseId",
                table: "ExerciseAttempt",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseAttempt_GuestSessionId",
                table: "ExerciseAttempt",
                column: "GuestSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseAttempt_StudentId_StartTime",
                table: "ExerciseAttempt",
                columns: new[] { "StudentId", "StartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseQuestion_QuestionId",
                table: "ExerciseQuestion",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_Flashcard_DeckId",
                table: "Flashcard",
                column: "DeckId");

            migrationBuilder.CreateIndex(
                name: "IX_FlashcardDeck_NodeId",
                table: "FlashcardDeck",
                column: "NodeId");

            migrationBuilder.CreateIndex(
                name: "IX_GradeLevel_Code",
                table: "GradeLevel",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GuestSession_ConvertedToStudentId",
                table: "GuestSession",
                column: "ConvertedToStudentId");

            migrationBuilder.CreateIndex(
                name: "IX_GuestSession_GradeLevelId",
                table: "GuestSession",
                column: "GradeLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_LearningPath_StudentId",
                table: "LearningPath",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_LessonResource_MediaAssetId",
                table: "LessonResource",
                column: "MediaAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_LessonResource_NodeId",
                table: "LessonResource",
                column: "NodeId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaAsset_UploadedBy",
                table: "MediaAsset",
                column: "UploadedBy");

            migrationBuilder.CreateIndex(
                name: "IX_NodeProgress_NodeId",
                table: "NodeProgress",
                column: "NodeId");

            migrationBuilder.CreateIndex(
                name: "IX_NodeProgress_StudentId_NodeId",
                table: "NodeProgress",
                columns: new[] { "StudentId", "NodeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NodeRevision_NodeId_RevisionNumber",
                table: "NodeRevision",
                columns: new[] { "NodeId", "RevisionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NodeSkill_SkillId",
                table: "NodeSkill",
                column: "SkillId");

            migrationBuilder.CreateIndex(
                name: "IX_NodeTypeRule_SubjectId_ParentType_ChildType",
                table: "NodeTypeRule",
                columns: new[] { "SubjectId", "ParentType", "ChildType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notification_StudentId",
                table: "Notification",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Notification_UserId",
                table: "Notification",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Order_BuyerUserId",
                table: "Order",
                column: "BuyerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItem_BeneficiaryStudentId",
                table: "OrderItem",
                column: "BeneficiaryStudentId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItem_CourseBundleId",
                table: "OrderItem",
                column: "CourseBundleId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItem_CourseId",
                table: "OrderItem",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItem_OrderId",
                table: "OrderItem",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItem_PackageId",
                table: "OrderItem",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_Package_UserId",
                table: "Package",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PackageEntitlement_CourseId",
                table: "PackageEntitlement",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_PackageEntitlement_GradeLevelId",
                table: "PackageEntitlement",
                column: "GradeLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_PackageEntitlement_PackageId",
                table: "PackageEntitlement",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_PackageEntitlement_SubjectId",
                table: "PackageEntitlement",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Parent_UserId",
                table: "Parent",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ParentInvite_AcceptedByStudentId",
                table: "ParentInvite",
                column: "AcceptedByStudentId");

            migrationBuilder.CreateIndex(
                name: "IX_ParentInvite_ParentId",
                table: "ParentInvite",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_ParentInvite_Token",
                table: "ParentInvite",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ParentLink_ParentId_StudentId",
                table: "ParentLink",
                columns: new[] { "ParentId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ParentLink_StudentId",
                table: "ParentLink",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Payment_OrderId",
                table: "Payment",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Payment_PaidByUserId",
                table: "Payment",
                column: "PaidByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Payment_StudentId",
                table: "Payment",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Promotion_Code",
                table: "Promotion",
                column: "Code",
                unique: true,
                filter: "\"Code\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionRedemption_OrderId",
                table: "PromotionRedemption",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionRedemption_PromotionId_OrderId",
                table: "PromotionRedemption",
                columns: new[] { "PromotionId", "OrderId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PromotionRedemption_UserId",
                table: "PromotionRedemption",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionScope_PromotionId",
                table: "PromotionScope",
                column: "PromotionId");

            migrationBuilder.CreateIndex(
                name: "IX_Question_BankId",
                table: "Question",
                column: "BankId");

            migrationBuilder.CreateIndex(
                name: "IX_Question_CreatedBy",
                table: "Question",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Question_ReviewedBy",
                table: "Question",
                column: "ReviewedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Question_SubjectId",
                table: "Question",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionBank_CourseId",
                table: "QuestionBank",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionBank_CreatedBy",
                table: "QuestionBank",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionBank_GradeLevelId",
                table: "QuestionBank",
                column: "GradeLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionBank_PrimaryNodeId",
                table: "QuestionBank",
                column: "PrimaryNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionBank_SubjectId",
                table: "QuestionBank",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionNode_NodeId",
                table: "QuestionNode",
                column: "NodeId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionOption_QuestionId",
                table: "QuestionOption",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionSkill_SkillId",
                table: "QuestionSkill",
                column: "SkillId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionTag_TagId",
                table: "QuestionTag",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewComment_BlockId",
                table: "ReviewComment",
                column: "BlockId");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewComment_NodeId",
                table: "ReviewComment",
                column: "NodeId");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewComment_ReviewId",
                table: "ReviewComment",
                column: "ReviewId");

            migrationBuilder.CreateIndex(
                name: "IX_Skill_ParentSkillId",
                table: "Skill",
                column: "ParentSkillId");

            migrationBuilder.CreateIndex(
                name: "IX_Skill_SubjectId_Code",
                table: "Skill",
                columns: new[] { "SubjectId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SkillProgress_SkillId",
                table: "SkillProgress",
                column: "SkillId");

            migrationBuilder.CreateIndex(
                name: "IX_SkillProgress_StudentId_SkillId",
                table: "SkillProgress",
                columns: new[] { "StudentId", "SkillId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StaticPage_Slug",
                table: "StaticPage",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Student_CurrentGradeLevelId",
                table: "Student",
                column: "CurrentGradeLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_Student_UserId",
                table: "Student",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentAnswer_AttemptId_QuestionId",
                table: "StudentAnswer",
                columns: new[] { "AttemptId", "QuestionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentAnswer_QuestionId",
                table: "StudentAnswer",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentAnswer_SelectedOptionId",
                table: "StudentAnswer",
                column: "SelectedOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentCourse_CourseId",
                table: "StudentCourse",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentCourse_CourseVersionId",
                table: "StudentCourse",
                column: "CourseVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentCourse_StudentId_CourseId",
                table: "StudentCourse",
                columns: new[] { "StudentId", "CourseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subject_Code",
                table: "Subject",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subject_Slug",
                table: "Subject",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subscription_PackageId",
                table: "Subscription",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscription_PaymentId",
                table: "Subscription",
                column: "PaymentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subscription_StudentId",
                table: "Subscription",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionMember_StudentId",
                table: "SubscriptionMember",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionMember_SubscriptionId_StudentId",
                table: "SubscriptionMember",
                columns: new[] { "SubscriptionId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupportMessage_SenderUserId",
                table: "SupportMessage",
                column: "SenderUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportMessage_TicketId",
                table: "SupportMessage",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTicket_AssignedToStaffId",
                table: "SupportTicket",
                column: "AssignedToStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTicket_ConversationId",
                table: "SupportTicket",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTicket_CreatedByUserId",
                table: "SupportTicket",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemConfig_ConfigKey",
                table: "SystemConfig",
                column: "ConfigKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SystemConfig_UpdatedBy",
                table: "SystemConfig",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TabSwitchLog_AttemptId",
                table: "TabSwitchLog",
                column: "AttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_User_Email",
                table: "User",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AIFeedback");

            migrationBuilder.DropTable(
                name: "AIHint");

            migrationBuilder.DropTable(
                name: "AuditLog");

            migrationBuilder.DropTable(
                name: "ChatMessage");

            migrationBuilder.DropTable(
                name: "ContentImportJob");

            migrationBuilder.DropTable(
                name: "CourseBundleItem");

            migrationBuilder.DropTable(
                name: "DailyActivitySnapshot");

            migrationBuilder.DropTable(
                name: "EmailVerificationToken");

            migrationBuilder.DropTable(
                name: "ExerciseQuestion");

            migrationBuilder.DropTable(
                name: "Flashcard");

            migrationBuilder.DropTable(
                name: "GuestIpUsage");

            migrationBuilder.DropTable(
                name: "LearningPath");

            migrationBuilder.DropTable(
                name: "LessonDetail");

            migrationBuilder.DropTable(
                name: "LessonResource");

            migrationBuilder.DropTable(
                name: "NodeProgress");

            migrationBuilder.DropTable(
                name: "NodeRevision");

            migrationBuilder.DropTable(
                name: "NodeSkill");

            migrationBuilder.DropTable(
                name: "NodeTypeRule");

            migrationBuilder.DropTable(
                name: "Notification");

            migrationBuilder.DropTable(
                name: "OrderItem");

            migrationBuilder.DropTable(
                name: "PackageEntitlement");

            migrationBuilder.DropTable(
                name: "ParentInvite");

            migrationBuilder.DropTable(
                name: "ParentLink");

            migrationBuilder.DropTable(
                name: "PromotionRedemption");

            migrationBuilder.DropTable(
                name: "PromotionScope");

            migrationBuilder.DropTable(
                name: "QuestionNode");

            migrationBuilder.DropTable(
                name: "QuestionSkill");

            migrationBuilder.DropTable(
                name: "QuestionTag");

            migrationBuilder.DropTable(
                name: "ReviewComment");

            migrationBuilder.DropTable(
                name: "SkillProgress");

            migrationBuilder.DropTable(
                name: "StaticPage");

            migrationBuilder.DropTable(
                name: "StudentAnswer");

            migrationBuilder.DropTable(
                name: "StudentCourse");

            migrationBuilder.DropTable(
                name: "SubscriptionMember");

            migrationBuilder.DropTable(
                name: "SupportMessage");

            migrationBuilder.DropTable(
                name: "SystemConfig");

            migrationBuilder.DropTable(
                name: "TabSwitchLog");

            migrationBuilder.DropTable(
                name: "FlashcardDeck");

            migrationBuilder.DropTable(
                name: "MediaAsset");

            migrationBuilder.DropTable(
                name: "CourseBundle");

            migrationBuilder.DropTable(
                name: "Parent");

            migrationBuilder.DropTable(
                name: "Promotion");

            migrationBuilder.DropTable(
                name: "Tag");

            migrationBuilder.DropTable(
                name: "ContentBlock");

            migrationBuilder.DropTable(
                name: "ContentReview");

            migrationBuilder.DropTable(
                name: "Skill");

            migrationBuilder.DropTable(
                name: "QuestionOption");

            migrationBuilder.DropTable(
                name: "Subscription");

            migrationBuilder.DropTable(
                name: "SupportTicket");

            migrationBuilder.DropTable(
                name: "ExerciseAttempt");

            migrationBuilder.DropTable(
                name: "Question");

            migrationBuilder.DropTable(
                name: "Package");

            migrationBuilder.DropTable(
                name: "Payment");

            migrationBuilder.DropTable(
                name: "ChatConversation");

            migrationBuilder.DropTable(
                name: "Exercise");

            migrationBuilder.DropTable(
                name: "GuestSession");

            migrationBuilder.DropTable(
                name: "QuestionBank");

            migrationBuilder.DropTable(
                name: "Order");

            migrationBuilder.DropTable(
                name: "Student");

            migrationBuilder.DropTable(
                name: "ContentNode");

            migrationBuilder.DropTable(
                name: "CourseVersion");

            migrationBuilder.DropTable(
                name: "Course");

            migrationBuilder.DropTable(
                name: "CurriculumFramework");

            migrationBuilder.DropTable(
                name: "GradeLevel");

            migrationBuilder.DropTable(
                name: "Subject");

            migrationBuilder.DropTable(
                name: "User");
        }
    }
}
