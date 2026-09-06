using System.Net.Http.Json;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Services.Implementations;
using Microsoft.EntityFrameworkCore;

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

    /// <summary>Nhồi đủ 12 loại <see cref="LessonBlockType"/> + 1 resource + 1 deck (2 thẻ) vào 1 node.</summary>
    public Task AddShowcaseContentAsync(int nodeId)
        => app.Db(async db =>
        {
            var order = 0;
            foreach (LessonBlockType type in Enum.GetValues<LessonBlockType>())
                db.ContentBlocks.Add(new ContentBlock
                {
                    NodeId = nodeId, BlockType = type, ContentText = $"{type} demo", OrderIndex = order++,
                });

            db.LessonResources.Add(new LessonResource
            {
                NodeId = nodeId, Title = "Tài liệu PDF", ResourceType = ResourceType.Pdf,
                ExternalUrl = "https://webapp.test/x.pdf", IsDownloadable = true, OrderIndex = 0,
            });

            var deck = new FlashcardDeck { NodeId = nodeId, Title = "Bộ thẻ", CreatedAt = DateTime.UtcNow };
            db.FlashcardDecks.Add(deck);
            await db.SaveChangesAsync();
            db.Flashcards.AddRange(
                new Flashcard { DeckId = deck.DeckId, FrontText = "2+2", BackText = "4", OrderIndex = 0 },
                new Flashcard { DeckId = deck.DeckId, FrontText = "3+3", BackText = "6", OrderIndex = 1 });
            await db.SaveChangesAsync();
        });

    /// <summary>1 dòng <see cref="DailyActivitySnapshot"/> có hoạt động (mặc định) cho ngày chỉ định.</summary>
    public Task SeedDailyActivityAsync(int studentId, DateOnly date, int minutes = 20, int lessons = 1)
        => app.Db(async db =>
        {
            db.DailyActivitySnapshots.Add(new DailyActivitySnapshot
            {
                StudentId = studentId, Date = date,
                MinutesStudied = minutes, ExercisesDone = 0, LessonsDone = lessons, QuestionsAnswered = lessons,
            });
            await db.SaveChangesAsync();
        });

    /// <summary>Chèn <paramref name="count"/> dòng <see cref="TabSwitchLog"/>; dòng cuối lùi <paramref name="lastAgo"/>.</summary>
    public Task AddTabSwitchLogsAsync(int attemptId, int count, TimeSpan lastAgo)
        => app.Db(async db =>
        {
            for (var i = 0; i < count; i++)
                db.TabSwitchLogs.Add(new TabSwitchLog
                {
                    AttemptId = attemptId,
                    SwitchedAt = DateTime.UtcNow - (i == 0 ? lastAgo : lastAgo + TimeSpan.FromMinutes(i)),
                });
            await db.SaveChangesAsync();
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
        int studentId, EntitlementScope scope, int? subjectId = null, int? gradeId = null,
        DateTime? expiresAt = null, PackageTier tier = PackageTier.Standard, string? packageName = null)
        => app.Db(async db =>
        {
            var owner = db.Users.First(u => u.UserType == UserType.SystemAdmin);
            var pkg = new Package
            {
                UserId = owner.UserId, PackageName = packageName ?? $"Pkg {Rand()}", Tier = tier,
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

    public Task<int> SeedNotificationAsync(int userId, string title = "Thử", bool read = false)
        => app.Db(async db =>
        {
            var n = new Notification
            {
                UserId = userId, Audience = NotifyAudience.Student, Title = title, Message = "nội dung",
                NotificationType = NotificationType.Info, IsRead = read, CreatedAt = DateTime.UtcNow,
            };
            db.Notifications.Add(n);
            await db.SaveChangesAsync();
            return n.NotificationId;
        });

    /// <summary>Phụ huynh mới đã xác nhận — trả <c>userId</c>, <c>parentId</c>, <c>connectionCode</c>.</summary>
    public async Task<(int userId, int parentId, string code)> NewParentAsync()
    {
        var (userId, _, _) = await NewConfirmedUserAsync(UserType.Parent);
        return await app.Db(async db =>
        {
            var p = await db.Parents.SingleAsync(x => x.UserId == userId);
            return (userId, p.ParentId, p.ConnectionCode);
        });
    }

    // ---------------------------------------------------------------- bài tập (F4)

    /// <summary>Học sinh mới đã xác nhận — trả cả <c>userId</c> lẫn <c>studentId</c>.</summary>
    public async Task<(int userId, int studentId)> NewStudentAsync()
    {
        var (userId, _, _) = await NewConfirmedUserAsync(UserType.Student);
        var studentId = await app.Db(db => db.Students.Where(s => s.UserId == userId)
            .Select(s => s.StudentId).FirstAsync());
        return (userId, studentId);
    }

    /// <summary>
    /// Exercise Published gắn 4 câu golden (MC/TF/FillBlank/Essay). <paramref name="tier"/> &gt; Free
    /// ⇒ đặt <c>IsFree=false</c> để kích hoạt cổng gói.
    /// </summary>
    public async Task<int> PublishExerciseAsync(
        AccessTier tier = AccessTier.Free, int? maxAttempts = null, int? durationMinutes = null)
        => await app.Db(async db =>
        {
            var ids = app.Ids;
            var ex = new Exercise
            {
                ExerciseName = $"Ex {Rand()}",
                ExerciseType = ExerciseType.Quiz,
                TotalQuestions = 4,
                TotalScores = 4,
                PassingScore = 2,
                MaxAttempts = maxAttempts,
                DurationMinutes = durationMinutes,
                RequiredTier = tier,
                IsFree = tier == AccessTier.Free,
                Status = ExerciseStatus.Published,
                IsActive = true,
                CreatedBy = ids.EditorUserId,
            };
            db.Exercises.Add(ex);
            await db.SaveChangesAsync();

            db.ExerciseQuestions.AddRange(
                new ExerciseQuestion { ExerciseId = ex.ExerciseId, QuestionId = ids.McQuestionId, Score = 1, OrderIndex = 1 },
                new ExerciseQuestion { ExerciseId = ex.ExerciseId, QuestionId = ids.TfQuestionId, Score = 1, OrderIndex = 2 },
                new ExerciseQuestion { ExerciseId = ex.ExerciseId, QuestionId = ids.FillBlankQuestionId, Score = 1, OrderIndex = 3 },
                new ExerciseQuestion { ExerciseId = ex.ExerciseId, QuestionId = ids.EssayQuestionId, Score = 1, OrderIndex = 4 });
            await db.SaveChangesAsync();
            return ex.ExerciseId;
        });

    // ---------------------------------------------------------------- thanh toán (F7)

    public async Task<(int subId, long amount)> CreatePendingSubscriptionAsync(int studentUserId, int packageId)
    {
        var res = await app.As(studentUserId).PostAsJsonAsync("/api/subscriptions",
            new { StudentId = await StudentIdOf(studentUserId), PackageId = packageId });
        res.EnsureSuccessStatusCode();
        var data = await res.DataAsync();
        return (data.GetProperty("subscriptionId").GetInt32(), (long)data.GetProperty("amount").GetDecimal());
    }

    public async Task<string> ActivateSubscriptionViaIpnAsync(int subId, long amount, string? reference = null)
    {
        reference ??= "IPN-" + Rand();
        var res = await SePayIpn.Client(app).PostAsJsonAsync("/api/sepay/ipn", SePayIpn.In(subId, amount, reference));
        res.EnsureSuccessStatusCode();
        return reference;
    }

    private Task<int> StudentIdOf(int userId)
        => app.Db(db => db.Students.Where(s => s.UserId == userId).Select(s => s.StudentId).FirstAsync());

    // ---------------------------------------------------------------- hoàn tiền (F8)

    public async Task<int> CreateRefundRequestAsync(int actorUserId, int paymentId, decimal? amount = null,
        string holderName = "Nguyen Van A")
    {
        var res = await app.As(actorUserId).PostAsJsonAsync("/api/refunds", new
        {
            PaymentId = paymentId,
            Amount = amount,
            ReasonCode = "CustomerRequest",
            BankBin = "970418",
            BankAccountNumber = "0071000123456",
            BankAccountHolderName = holderName,
        });
        res.EnsureSuccessStatusCode();
        return (await res.DataAsync()).GetProperty("RefundRequestId").GetInt32();
    }

    /// <summary>Sửa 1 SystemConfig rồi xoá cache tương ứng. Nhớ trả lại giá trị cũ sau test.</summary>
    public async Task SetConfigAsync(string key, string value)
    {
        await app.Db(async db =>
        {
            var row = await db.SystemConfigs.SingleAsync(c => c.ConfigKey == key);
            row.ConfigValue = value;
            await db.SaveChangesAsync();
        });
        app.BustCache($"cfg:{key}");
    }

    /// <summary>Payment Completed + Subscription Active (qua IPN thật) — trả <c>(paymentId, subId)</c>.</summary>
    public async Task<(int paymentId, int subId)> SeedActiveSubscriptionAsync(int studentUserId, int packageId)
    {
        var (subId, amount) = await CreatePendingSubscriptionAsync(studentUserId, packageId);
        await ActivateSubscriptionViaIpnAsync(subId, amount);
        var paymentId = await app.Db(db => db.Subscriptions.Where(s => s.SubscriptionId == subId)
            .Select(s => s.PaymentId!.Value).FirstAsync());
        return (paymentId, subId);
    }

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
