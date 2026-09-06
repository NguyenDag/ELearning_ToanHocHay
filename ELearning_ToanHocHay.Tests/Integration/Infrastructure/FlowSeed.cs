using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Services.Helpers;
using ELearning_ToanHocHay_Control.Services.Implementations;

namespace ELearning_ToanHocHay.Tests.Integration.Infrastructure;

/// <summary>
/// Builder theo luồng — mỗi hàm <c>Guid</c>-hoá tên/email, KHÔNG đụng golden dataset.
/// Bổ sung dần theo từng flow F1…F12.
/// </summary>
public sealed class FlowSeed(ApiFactory app)
{
    private static string Rand() => Guid.NewGuid().ToString("N")[..8];

    /// <summary>Người dùng đã xác nhận email, đăng nhập được ngay bằng <see cref="SeedData.Password"/>.</summary>
    public async Task<(int userId, string email, string password)> NewConfirmedUserAsync(UserType type)
    {
        var email = $"{type.ToString().ToLowerInvariant()}.{Rand()}@flow.test";
        var userId = await app.Db(async db =>
        {
            var u = new User
            {
                Email = email,
                PasswordHash = new PasswordHasher().HashPassword(SeedData.Password),
                FullName = $"Flow {Rand()}",
                UserType = type,
                IsEmailConfirmed = true,
                EmailConfirmedAt = DateTime.UtcNow,
                IsActive = true,
            };
            db.Users.Add(u);
            await db.SaveChangesAsync();
            await AttachRoleRowAsync(db, u);
            return u.UserId;
        });
        return (userId, email, SeedData.Password);
    }

    /// <summary>Người dùng CHƯA xác nhận + token xác nhận thô (để gọi <c>confirm-email</c>).</summary>
    public async Task<(int userId, string email, string token)> NewUnconfirmedUserAsync(UserType type)
    {
        var email = $"{type.ToString().ToLowerInvariant()}.{Rand()}@flow.test";
        var raw = Guid.NewGuid().ToString("N");
        var userId = await app.Db(async db =>
        {
            var u = new User
            {
                Email = email,
                PasswordHash = new PasswordHasher().HashPassword(SeedData.Password),
                FullName = $"Flow {Rand()}",
                UserType = type,
                IsEmailConfirmed = false,
                IsActive = true,
            };
            db.Users.Add(u);
            await db.SaveChangesAsync();
            await AttachRoleRowAsync(db, u);

            db.EmailVerificationTokens.Add(new EmailVerificationToken
            {
                UserId = u.UserId,
                Token = raw,
                ExpiredAt = DateTime.UtcNow.AddHours(24),
                IsUsed = false,
            });
            await db.SaveChangesAsync();
            return u.UserId;
        });
        return (userId, email, raw);
    }

    private static async Task AttachRoleRowAsync(ELearning_ToanHocHay_Control.Data.AppDbContext db, User u)
    {
        switch (u.UserType)
        {
            case UserType.Student:
                db.Students.Add(new Student { UserId = u.UserId, CurrentGradeLevelId = 1 });
                break;
            case UserType.Parent:
                db.Parents.Add(new Parent { UserId = u.UserId, ConnectionCode = Guid.NewGuid().ToString("N")[..8].ToUpper() });
                break;
        }
        await db.SaveChangesAsync();
    }

    // ---------------------------------------------------------------- nội dung (F2/F3)

    public sealed record CourseSeed(
        int CourseId, int VersionId,
        int[] FreeChapterIds, int[] PaidChapterIds,
        int FreeLessonId, int FirstPaidLessonId);

    /// <summary>
    /// Khoá Published + 1 version Published. Mỗi chương free có 1 bài free + 1 bài trả phí;
    /// mỗi chương trả phí có 1 bài trả phí.
    /// </summary>
    public async Task<CourseSeed> PublishCourseAsync(string? slug = null, int freeChapters = 1, int paidChapters = 1)
        => await app.Db(async db =>
        {
            var course = new Course
            {
                SubjectId = 1, GradeLevelId = 1, FrameworkId = null,
                Title = $"Khoá {Rand()}",
                Slug = slug ?? $"khoa-{Rand()}",
                Status = CourseStatus.Published,
                ListPrice = 0, IsPurchasable = true, CreatedBy = 1,
            };
            db.Courses.Add(course);
            await db.SaveChangesAsync();

            var version = new CourseVersion { CourseId = course.CourseId, VersionNumber = 1, State = VersionState.Published };
            db.CourseVersions.Add(version);
            await db.SaveChangesAsync();

            int order = 0;
            async Task<ContentNode> Node(NodeType type, ContentNode? parent, bool isFree)
            {
                var n = new ContentNode
                {
                    CourseVersionId = version.CourseVersionId,
                    NodeType = type,
                    Title = $"{type} {Rand()}",
                    ParentNodeId = parent?.NodeId,
                    Depth = parent is null ? 0 : parent.Depth + 1,
                    OrderIndex = order++,
                    IsFree = isFree,
                    MaterializedPath = "/",
                    CreatedBy = 1,
                };
                db.ContentNodes.Add(n);
                await db.SaveChangesAsync();
                n.MaterializedPath = (parent?.MaterializedPath ?? "/") + n.NodeId + "/";
                await db.SaveChangesAsync();
                return n;
            }

            var freeChapterIds = new List<int>();
            var paidChapterIds = new List<int>();
            int freeLessonId = 0, firstPaidLessonId = 0;

            for (var i = 0; i < freeChapters; i++)
            {
                var ch = await Node(NodeType.Chapter, null, isFree: true);
                freeChapterIds.Add(ch.NodeId);
                var freeLesson = await Node(NodeType.Lesson, ch, isFree: true);
                var paidLesson = await Node(NodeType.Lesson, ch, isFree: false);
                if (freeLessonId == 0) freeLessonId = freeLesson.NodeId;
                if (firstPaidLessonId == 0) firstPaidLessonId = paidLesson.NodeId;
            }

            for (var i = 0; i < paidChapters; i++)
            {
                var ch = await Node(NodeType.Chapter, null, isFree: false);
                paidChapterIds.Add(ch.NodeId);
                var paidLesson = await Node(NodeType.Lesson, ch, isFree: false);
                if (firstPaidLessonId == 0) firstPaidLessonId = paidLesson.NodeId;
            }

            return new CourseSeed(course.CourseId, version.CourseVersionId,
                freeChapterIds.ToArray(), paidChapterIds.ToArray(), freeLessonId, firstPaidLessonId);
        });

    /// <summary>Ghi danh trực tiếp (StudentCourse Active).</summary>
    public Task EnrolAsync(int studentUserId, int courseId, int courseVersionId)
        => app.Db(async db =>
        {
            var studentId = db.Students.Single(s => s.UserId == studentUserId).StudentId;
            db.StudentCourses.Add(new StudentCourse
            {
                StudentId = studentId, CourseId = courseId, CourseVersionId = courseVersionId,
                Source = EnrollSource.Self, Status = StudentCourseStatus.Active, ProgressPercent = 0,
                EnrolledAt = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        });

    /// <summary>Cấp entitlement qua một subscription Active (package + entitlement + subscription).</summary>
    public Task GrantEntitlementAsync(
        int studentId, EntitlementScope scope, int? subjectId = null, int? gradeId = null, DateTime? expiresAt = null)
        => app.Db(async db =>
        {
            var owner = db.Users.First(u => u.UserType == UserType.SystemAdmin);
            var pkg = new Package
            {
                UserId = owner.UserId, PackageName = $"Pkg {Rand()}", Tier = PackageTier.Standard,
                Price = 199_000m, DurationDays = 30, IsActive = true,
            };
            db.Packages.Add(pkg);
            await db.SaveChangesAsync();

            db.PackageEntitlements.Add(new PackageEntitlement
            {
                PackageId = pkg.PackageId, ScopeType = scope, SubjectId = subjectId, GradeLevelId = gradeId,
            });
            var now = DateTime.UtcNow;
            db.Subscriptions.Add(new Subscription
            {
                StudentId = studentId, PackageId = pkg.PackageId, Status = SubscriptionStatus.Active,
                AmountPaid = 199_000m, StartDate = now.AddDays(-1), EndDate = expiresAt ?? now.AddDays(30), CreatedAt = now,
            });
            await db.SaveChangesAsync();
        });

    /// <summary>Payment đã Completed, không gắn subscription — dùng cho luồng hoàn tiền F8.</summary>
    public async Task<int> SeedRefundablePaymentAsync(int payerUserId, decimal amount = 199_000m, DateTime? paidAt = null)
        => await app.Db(async db =>
        {
            var studentId = db.Students.Where(s => s.UserId == payerUserId).Select(s => (int?)s.StudentId).FirstOrDefault();
            var p = new Payment
            {
                PaidByUserId = payerUserId,
                StudentId = studentId,
                Amount = amount,
                PaymentMethod = PaymentMethod.BankTransfer,
                Status = PaymentStatus.Completed,
                TransactionId = "FLOW-" + Rand(),
                PaymentDate = paidAt ?? DateTime.UtcNow.AddDays(-1),
            };
            db.Payments.Add(p);
            await db.SaveChangesAsync();
            return p.PaymentId;
        });
}
