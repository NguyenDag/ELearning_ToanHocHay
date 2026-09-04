using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ELearning_ToanHocHay_Control.Data.Seed
{
    /// <summary>
    /// One-time demo dataset for local / staging: 7 roles, the three Toán 6 textbooks with a full
    /// KNTT content tree, a ~300-question bank, free + paid exercises, subscriptions/payments and
    /// ~90 days of attempt history so dashboards, the heatmap and streaks render with real numbers.
    /// Idempotent — a re-run is a no-op once <c>admin@thh.local</c> exists. Dev-only, gated by
    /// <c>Seed:DemoData</c>.
    /// </summary>
    public sealed class DemoDataSeeder(
        AppDbContext db,
        IPasswordHasher passwordHasher,
        IProgressProjectionService progression,
        ILogger<DemoDataSeeder> logger)
    {
        private const string AdminEmail = "admin@thh.local";
        private const string DemoPassword = "123456";
        private const int StandardPrice = 149_000;
        private const int PremiumPrice = 199_000;

        private readonly DateTime _now = DateTime.UtcNow;
        private readonly Random _rng = new(20260904);
        private string _pwHash = string.Empty;

        public async Task SeedAsync()
        {
            if (await db.Users.AsNoTracking().AnyAsync(u => u.Email == AdminEmail))
            {
                logger.LogInformation("DemoDataSeeder: demo data already present — skipping.");
                return;
            }

            _pwHash = passwordHasher.HashPassword(DemoPassword);
            logger.LogInformation("DemoDataSeeder: seeding demo data (this runs once)…");

            await using var tx = await db.Database.BeginTransactionAsync();
            var c = new SeedContext();

            await SeedUsersAsync(c);
            await SeedPackagesAsync(c);
            await SeedCoursesAsync(c);
            await SeedQuestionBanksAsync(c);
            await SeedExercisesAsync(c);
            await SeedSubscriptionsAndEnrolmentsAsync(c);
            await SeedAttemptHistoryAsync(c);
            await SeedProgressAsync(c);

            db.ChangeTracker.Clear();
            foreach (var (studentId, versionId) in c.EnrolledVersions.Distinct())
                await progression.RecomputeCourseVersionAsync(studentId, versionId);

            await SeedParentLinksAsync(c);
            await SeedNotificationsAsync(c);

            await tx.CommitAsync();

            logger.LogInformation(
                "DemoDataSeeder: done — {Users} users, 3 courses, {Questions} questions, {Exercises} exercises, {Attempts} attempts.",
                c.StudentIds.Count + c.ParentIds.Count + 3, c.AllQuestionIds.Count, c.Exercises.Count, c.AttemptCount);
        }

        // ==============================================================
        //  users
        // ==============================================================
        private async Task SeedUsersAsync(SeedContext c)
        {
            var admin = MakeUser(AdminEmail, "Trần Quản Trị", UserType.SystemAdmin, "0900000001");
            var editor = MakeUser("editor@thh.local", "Nguyễn Biên Tập", UserType.ContentEditor, "0900000002");
            var reviewer = MakeUser("reviewer@thh.local", "Lê Thẩm Định", UserType.AcademicReviewer, "0900000003");
            db.Users.AddRange(admin, editor, reviewer);

            var studentUsers = new List<User>();
            for (int i = 0; i < 20; i++)
            {
                var u = MakeUser($"hs{i + 1:00}@thh.local", StudentNames[i], UserType.Student, $"09110000{i:00}");
                studentUsers.Add(u);
                db.Users.Add(u);
            }

            var parentUsers = new List<User>();
            for (int i = 0; i < 10; i++)
            {
                var u = MakeUser($"ph{i + 1:00}@thh.local", ParentNames[i], UserType.Parent, $"09880000{i:00}");
                parentUsers.Add(u);
                db.Users.Add(u);
            }

            await db.SaveChangesAsync();

            c.AdminUserId = admin.UserId;
            c.EditorUserId = editor.UserId;
            c.ReviewerUserId = reviewer.UserId;

            var students = new List<Student>();
            for (int i = 0; i < studentUsers.Count; i++)
            {
                var s = new Student
                {
                    UserId = studentUsers[i].UserId,
                    CurrentGradeLevelId = 1,
                    SchoolName = Schools[i % Schools.Length],
                    AiDataSharingLevel = i % 4 == 0 ? AiDataSharingLevel.Detailed : AiDataSharingLevel.SummaryOnly,
                };
                students.Add(s);
                db.Students.Add(s);
            }

            var parents = new List<Parent>();
            for (int i = 0; i < parentUsers.Count; i++)
            {
                var p = new Parent
                {
                    UserId = parentUsers[i].UserId,
                    Job = Jobs[i % Jobs.Length],
                    ConnectionCode = $"PH{i + 1:00}{RandCode(4)}",
                };
                parents.Add(p);
                db.Parents.Add(p);
            }

            await db.SaveChangesAsync();

            c.StudentUserIds.AddRange(studentUsers.Select(u => u.UserId));
            c.StudentIds.AddRange(students.Select(s => s.StudentId));
            c.ParentUserIds.AddRange(parentUsers.Select(u => u.UserId));
            c.ParentIds.AddRange(parents.Select(p => p.ParentId));
        }

        private User MakeUser(string email, string fullName, UserType type, string phone) => new()
        {
            Email = email,
            PasswordHash = _pwHash,
            FullName = fullName,
            Phone = phone,
            UserType = type,
            IsEmailConfirmed = true,
            EmailConfirmedAt = _now.AddDays(-120),
            IsActive = true,
            CreatedAt = _now.AddDays(-120),
            SecurityStamp = Guid.NewGuid().ToString("N"),
        };

        // ==============================================================
        //  packages
        // ==============================================================
        private async Task SeedPackagesAsync(SeedContext c)
        {
            var std = new Package
            {
                UserId = c.AdminUserId,
                PackageName = "Gói Tiêu chuẩn",
                Description = "Mở khoá toàn bộ bài giảng và đề kiểm tra Toán 6; 20 lượt gợi ý AI mỗi ngày.",
                Tier = PackageTier.Standard,
                Price = StandardPrice,
                DurationDays = 30,
                AiHintLimitDaily = 20,
                UnlimitedAiHint = false,
                PersonalizedPath = false,
                MistakeRetry = true,
                SmartReminder = true,
                PrioritySupport = false,
                IsActive = true,
                CreatedAt = _now.AddDays(-110),
            };
            var prm = new Package
            {
                UserId = c.AdminUserId,
                PackageName = "Gói Cao cấp",
                Description = "Toàn bộ quyền lợi Tiêu chuẩn + gợi ý AI không giới hạn, lộ trình cá nhân hoá, hỗ trợ ưu tiên.",
                Tier = PackageTier.Premium,
                Price = PremiumPrice,
                DurationDays = 30,
                AiHintLimitDaily = null,
                UnlimitedAiHint = true,
                PersonalizedPath = true,
                MistakeRetry = true,
                SmartReminder = true,
                PrioritySupport = true,
                IsActive = true,
                CreatedAt = _now.AddDays(-110),
            };
            db.Packages.AddRange(std, prm);
            await db.SaveChangesAsync();

            db.PackageEntitlements.AddRange(
                new PackageEntitlement { PackageId = std.PackageId, ScopeType = EntitlementScope.SubjectGrade, SubjectId = 1, GradeLevelId = 1 },
                new PackageEntitlement { PackageId = prm.PackageId, ScopeType = EntitlementScope.SubjectGrade, SubjectId = 1, GradeLevelId = 1 });
            await db.SaveChangesAsync();

            c.StandardPackageId = std.PackageId;
            c.PremiumPackageId = prm.PackageId;
        }

        // ==============================================================
        //  courses + content tree
        // ==============================================================
        private async Task SeedCoursesAsync(SeedContext c)
        {
            await SeedCourseAsync(c, 1, "KNTT", "Kết nối tri thức với cuộc sống", DemoContent.CourseKnttSlug, deep: true);
            await SeedCourseAsync(c, 2, "CTST", "Chân trời sáng tạo", DemoContent.CourseCtstSlug, deep: false);
            await SeedCourseAsync(c, 3, "CD", "Cánh Diều", DemoContent.CourseCdSlug, deep: false);
        }

        private async Task SeedCourseAsync(SeedContext c, int frameworkId, string code, string bookName, string slug, bool deep)
        {
            var course = new Course
            {
                SubjectId = 1,
                GradeLevelId = 1,
                FrameworkId = frameworkId,
                Title = $"Toán 6 – {bookName}",
                Slug = slug,
                Description = $"Khoá học Toán lớp 6 bám sát bộ sách {bookName}: lý thuyết, ví dụ, video minh hoạ, " +
                              "bài tập tự luyện và đề kiểm tra theo từng chương.",
                ThumbnailUrl = DemoContent.ImageUrl,
                Status = CourseStatus.Published,
                ListPrice = 299_000,
                SalePrice = 249_000,
                IsPurchasable = true,
                AccessDurationDays = 365,
                DisplayOrder = frameworkId,
                CreatedBy = c.EditorUserId,
                CreatedAt = _now.AddDays(-95),
                UpdatedAt = _now.AddDays(-30),
            };
            db.Courses.Add(course);
            await db.SaveChangesAsync();

            var version = new CourseVersion
            {
                CourseId = course.CourseId,
                VersionNumber = 1,
                Label = "Năm học 2026–2027",
                State = VersionState.Published,
                SubmittedBy = c.EditorUserId,
                SubmittedAt = _now.AddDays(-82),
                PublishedBy = c.ReviewerUserId,
                PublishedAt = _now.AddDays(-76),
                CreatedAt = _now.AddDays(-95),
            };
            db.CourseVersions.Add(version);
            await db.SaveChangesAsync();

            db.ContentReviews.Add(new ContentReview
            {
                CourseVersionId = version.CourseVersionId,
                ReviewerId = c.ReviewerUserId,
                Decision = ReviewDecision.Approve,
                Summary = "Nội dung đầy đủ, chính xác theo chuẩn chương trình GDPT 2018. Đồng ý xuất bản.",
                CreatedAt = _now.AddDays(-77),
            });
            await db.SaveChangesAsync();

            var chapters = DemoContent.Chapters[code];
            for (int ci = 0; ci < chapters.Length; ci++)
            {
                bool chapterFree = ci == 0;
                var chapter = await AddNodeAsync(version, null, NodeType.Chapter,
                    $"Chương {ci + 1}. {chapters[ci]}", ci, chapterFree, c.EditorUserId);

                string[] lessonTitles = deep
                    ? DemoContent.KnttLessons[ci]
                    : DemoContent.LightLessons(chapters[ci]);

                var lessonIds = new List<int>();
                for (int li = 0; li < lessonTitles.Length; li++)
                {
                    bool showcase = deep && li == 0;
                    var lesson = await AddNodeAsync(version, chapter, NodeType.Lesson, lessonTitles[li], li,
                        chapterFree, c.EditorUserId, durationMinutes: 20 + li * 5);
                    lessonIds.Add(lesson.NodeId);

                    var blocks = DemoContent.BuildBlocks(ci + 1, chapters[ci], lessonTitles[li], showcase);
                    foreach (var b in blocks) b.NodeId = lesson.NodeId;
                    db.ContentBlocks.AddRange(blocks);

                    if (!chapterFree)
                    {
                        db.LessonResources.Add(new LessonResource
                        {
                            NodeId = lesson.NodeId,
                            Title = $"Tóm tắt lý thuyết – {lessonTitles[li]}",
                            ResourceType = li % 2 == 0 ? ResourceType.Pdf : ResourceType.Slide,
                            ExternalUrl = li % 2 == 0 ? DemoContent.PdfUrl : DemoContent.SlideUrl,
                            IsDownloadable = true,
                            OrderIndex = 0,
                        });
                    }

                    if (deep && ci == 0 && li == 0)
                        await AddFlashcardsAsync(lesson.NodeId);
                }
                await db.SaveChangesAsync();

                // KNTT chapters 2 & 6 get an intermediate Topic to show a 3-level tree
                if (deep && (ci == 1 || ci == 5))
                {
                    var topic = await AddNodeAsync(version, chapter, NodeType.Topic,
                        $"Chuyên đề luyện tập: {chapters[ci]}", lessonTitles.Length, chapterFree, c.EditorUserId);
                    var sub = await AddNodeAsync(version, topic, NodeType.Lesson,
                        $"Bài tập tổng hợp – {chapters[ci]}", 0, chapterFree, c.EditorUserId, durationMinutes: 30);
                    lessonIds.Add(sub.NodeId);
                    var blk = DemoContent.BuildBlocks(ci + 1, chapters[ci], sub.Title, showcase: false);
                    foreach (var b in blk) b.NodeId = sub.NodeId;
                    db.ContentBlocks.AddRange(blk);
                    await db.SaveChangesAsync();
                }

                if (deep)
                {
                    c.KnttChapterNodeIds.Add(chapter.NodeId);
                    c.KnttChapterLessonIds.Add(lessonIds);
                    if (lessonIds.Count > 0) c.KnttShowcaseLessonIds.Add(lessonIds[0]);
                }
            }

            if (deep)
            {
                c.KnttCourseId = course.CourseId;
                c.KnttVersionId = version.CourseVersionId;
            }
        }

        private async Task<ContentNode> AddNodeAsync(CourseVersion version, ContentNode? parent, NodeType type,
            string title, int orderIndex, bool isFree, int userId, int? durationMinutes = null)
        {
            var node = new ContentNode
            {
                CourseVersionId = version.CourseVersionId,
                ParentNodeId = parent?.NodeId,
                NodeType = type,
                Title = title,
                Slug = DemoContent.Slugify(title),
                OrderIndex = orderIndex,
                Depth = parent == null ? 0 : parent.Depth + 1,
                MaterializedPath = "/",
                IsFree = isFree,
                CreatedBy = userId,
                CreatedAt = _now.AddDays(-85),
            };
            db.ContentNodes.Add(node);
            await db.SaveChangesAsync();

            node.MaterializedPath = (parent?.MaterializedPath ?? "/") + node.NodeId + "/";
            if (type == NodeType.Lesson && durationMinutes.HasValue)
                db.LessonDetails.Add(new LessonDetail { NodeId = node.NodeId, DurationMinutes = durationMinutes });
            await db.SaveChangesAsync();
            return node;
        }

        private async Task AddFlashcardsAsync(int nodeId)
        {
            var deck = new FlashcardDeck { NodeId = nodeId, Title = "Thẻ ghi nhớ: Tập hợp số tự nhiên", CreatedAt = _now };
            db.FlashcardDecks.Add(deck);
            await db.SaveChangesAsync();

            db.Flashcards.AddRange(
                new Flashcard { DeckId = deck.DeckId, FrontText = "Kí hiệu tập hợp số tự nhiên?", BackText = "ℕ = {0; 1; 2; 3; …}", OrderIndex = 0 },
                new Flashcard { DeckId = deck.DeckId, FrontText = "Số tự nhiên nhỏ nhất là số nào?", BackText = "Số 0", OrderIndex = 1 },
                new Flashcard { DeckId = deck.DeckId, FrontText = "\"a ∈ A\" nghĩa là gì?", BackText = "a là một phần tử của tập hợp A", OrderIndex = 2 },
                new Flashcard { DeckId = deck.DeckId, FrontText = "Tập hợp rỗng kí hiệu là gì?", BackText = "∅ hoặc { }", OrderIndex = 3 },
                new Flashcard { DeckId = deck.DeckId, FrontText = "Có số tự nhiên lớn nhất không?", BackText = "Không — dãy số tự nhiên là vô hạn", OrderIndex = 4 });
            await db.SaveChangesAsync();
        }

        // ==============================================================
        //  question bank
        // ==============================================================
        private async Task SeedQuestionBanksAsync(SeedContext c)
        {
            var chapters = DemoContent.Chapters["KNTT"];
            for (int ci = 0; ci < chapters.Length; ci++)
            {
                var bank = new QuestionBank
                {
                    BankName = $"Ngân hàng câu hỏi – Chương {ci + 1}. {chapters[ci]}",
                    Description = $"Câu hỏi trắc nghiệm, đúng/sai và điền khuyết cho Chương {ci + 1} (Toán 6 – KNTT).",
                    SubjectId = 1,
                    GradeLevelId = 1,
                    CourseId = c.KnttCourseId,
                    PrimaryNodeId = c.KnttChapterNodeIds[ci],
                    CreatedBy = c.EditorUserId,
                    IsActive = true,
                    CreatedAt = _now.AddDays(-70),
                };
                db.QuestionBanks.Add(bank);
                await db.SaveChangesAsync();
                c.ChapterBankIds.Add(bank.BankId);

                var generated = DemoQuestionFactory.ForChapter(ci + 1, 33, _rng);
                var chapterQuestionIds = new List<int>();

                foreach (var g in generated)
                {
                    var q = new Question
                    {
                        BankId = bank.BankId,
                        SubjectId = 1,
                        QuestionText = g.Text,
                        QuestionType = g.Type,
                        DifficultyLevel = g.Difficulty,
                        CorrectAnswer = g.CorrectAnswer,
                        Explanation = g.Explanation,
                        Status = QuestionStatus.Approved,
                        IsActive = true,
                        CreatedBy = c.EditorUserId,
                        ReviewedBy = c.ReviewerUserId,
                        CreatedAt = _now.AddDays(-70),
                        ReviewedAt = _now.AddDays(-68),
                        PublishedAt = _now.AddDays(-68),
                    };
                    db.Questions.Add(q);
                    await db.SaveChangesAsync();

                    if (g.Options.Count > 0)
                    {
                        int oi = 0;
                        foreach (var o in g.Options)
                            db.QuestionOptions.Add(new QuestionOption
                            {
                                QuestionId = q.QuestionId,
                                OptionText = o.Text,
                                IsCorrect = o.IsCorrect,
                                OrderIndex = oi++,
                            });
                    }
                    db.QuestionNodes.Add(new QuestionNode { QuestionId = q.QuestionId, NodeId = c.KnttChapterNodeIds[ci] });

                    c.QuestionMeta[q.QuestionId] = new QMeta
                    {
                        Type = g.Type,
                        CorrectAnswer = g.CorrectAnswer ?? string.Empty,
                    };
                    chapterQuestionIds.Add(q.QuestionId);
                    c.AllQuestionIds.Add(q.QuestionId);
                }
                await db.SaveChangesAsync();
                c.ChapterQuestionIds.Add(chapterQuestionIds);
            }

            // resolve option ids for grading, in one pass
            var options = await db.QuestionOptions.AsNoTracking()
                .Where(o => c.AllQuestionIds.Contains(o.QuestionId))
                .Select(o => new { o.QuestionId, o.OptionId, o.IsCorrect })
                .ToListAsync();
            foreach (var grp in options.GroupBy(o => o.QuestionId))
            {
                var m = c.QuestionMeta[grp.Key];
                m.CorrectOptionId = grp.First(x => x.IsCorrect).OptionId;
                m.WrongOptionId = grp.First(x => !x.IsCorrect).OptionId;
            }
        }

        // ==============================================================
        //  exercises
        // ==============================================================
        private async Task SeedExercisesAsync(SeedContext c)
        {
            var chapters = DemoContent.Chapters["KNTT"];
            for (int ci = 0; ci < chapters.Length; ci++)
            {
                var qids = c.ChapterQuestionIds[ci];
                var chapterNode = c.KnttChapterNodeIds[ci];

                await AddExerciseAsync(c, $"Bài tập tự luyện – Chương {ci + 1}: {chapters[ci]}",
                    ExerciseType.Quiz, chapterNode, isFree: true, tier: AccessTier.Free,
                    durationMinutes: null, maxAttempts: null, pick: qids.Take(10).ToList());

                await AddExerciseAsync(c, $"Đề kiểm tra 15 phút – Chương {ci + 1}",
                    ExerciseType.Test, chapterNode, isFree: false, tier: AccessTier.Standard,
                    durationMinutes: 15, maxAttempts: 3, pick: qids.Skip(8).Take(12).ToList());

                await AddExerciseAsync(c, $"Đề thi cuối chương {ci + 1} (đề soạn sẵn)",
                    ExerciseType.Exam, chapterNode, isFree: false, tier: AccessTier.Premium,
                    durationMinutes: 45, maxAttempts: 2, pick: qids.Skip(10).Take(20).ToList());
            }

            await AddExerciseAsync(c, "Đề ngẫu nhiên – Ôn tập tổng hợp (miễn phí)",
                ExerciseType.Quiz, c.KnttChapterNodeIds[0], isFree: true, tier: AccessTier.Free,
                durationMinutes: null, maxAttempts: null,
                pick: c.AllQuestionIds.OrderBy(_ => _rng.Next()).Take(10).ToList());

            await AddExerciseAsync(c, "Đề ngẫu nhiên – Luyện thi tổng hợp (Cao cấp)",
                ExerciseType.Exam, c.KnttChapterNodeIds[2], isFree: false, tier: AccessTier.Premium,
                durationMinutes: 30, maxAttempts: null,
                pick: c.AllQuestionIds.OrderBy(_ => _rng.Next()).Take(15).ToList());
        }

        private async Task AddExerciseAsync(SeedContext c, string name, ExerciseType type, int? nodeId,
            bool isFree, AccessTier tier, int? durationMinutes, int? maxAttempts, List<int> pick)
        {
            if (pick.Count == 0) return;

            var ex = new Exercise
            {
                NodeId = nodeId,
                ExerciseName = name,
                ExerciseType = type,
                TotalQuestions = pick.Count,
                DurationMinutes = durationMinutes,
                MaxAttempts = maxAttempts,
                IsFree = isFree,
                RequiredTier = tier,
                IsActive = true,
                TotalScores = pick.Count,
                PassingScore = Math.Ceiling(pick.Count / 2.0),
                Status = ExerciseStatus.Published,
                CreatedBy = c.EditorUserId,
                CreatedAt = _now.AddDays(-60),
            };
            db.Exercises.Add(ex);
            await db.SaveChangesAsync();

            var seedEx = new SeedExercise
            {
                ExerciseId = ex.ExerciseId,
                NodeId = nodeId,
                TotalScores = ex.TotalScores,
                DurationMinutes = durationMinutes,
                IsFree = isFree,
                RequiredTier = tier,
            };
            int oi = 0;
            foreach (var qid in pick)
            {
                db.ExerciseQuestions.Add(new ExerciseQuestion { ExerciseId = ex.ExerciseId, QuestionId = qid, Score = 1, OrderIndex = oi++ });
                seedEx.Questions.Add((qid, 1));
            }
            await db.SaveChangesAsync();
            c.Exercises.Add(seedEx);
        }

        // ==============================================================
        //  subscriptions / payments / enrolments
        // ==============================================================
        private async Task SeedSubscriptionsAndEnrolmentsAsync(SeedContext c)
        {
            for (int i = 0; i < 10; i++)   // students 0..4 → Standard, 5..9 → Premium
            {
                int studentId = c.StudentIds[i];
                int studentUserId = c.StudentUserIds[i];
                bool premium = i >= 5;
                int packageId = premium ? c.PremiumPackageId : c.StandardPackageId;
                int price = premium ? PremiumPrice : StandardPrice;
                int daysAgo = 3 + _rng.Next(0, 22);
                var start = _now.AddDays(-daysAgo);
                int payerUserId = (i % 3 == 0) ? c.ParentUserIds[i / 2] : studentUserId;

                var payment = new Payment
                {
                    PaidByUserId = payerUserId,
                    StudentId = studentId,
                    Amount = price,
                    PaymentMethod = PaymentMethod.BankTransfer,
                    Status = PaymentStatus.Completed,
                    PaymentDate = start,
                    TransactionId = $"SEEDPAY{i:00}",
                    Notes = "Thanh toán gói cước (dữ liệu demo).",
                };
                db.Payments.Add(payment);
                await db.SaveChangesAsync();

                db.Subscriptions.Add(new Subscription
                {
                    StudentId = studentId,
                    PackageId = packageId,
                    PaymentId = payment.PaymentId,
                    StartDate = start,
                    EndDate = start.AddDays(30),
                    Status = SubscriptionStatus.Active,
                    AmountPaid = price,
                    CreatedAt = start,
                });
                db.StudentCourses.Add(new StudentCourse
                {
                    StudentId = studentId,
                    CourseId = c.KnttCourseId,
                    CourseVersionId = c.KnttVersionId,
                    Source = EnrollSource.Subscription,
                    Status = StudentCourseStatus.Active,
                    ProgressPercent = 0,
                    EnrolledAt = start,
                });
                await db.SaveChangesAsync();

                c.EnrolledVersions.Add((studentId, c.KnttVersionId));
            }

            // an expired subscription in student 1's history
            {
                var start = _now.AddDays(-140);
                var pay = new Payment
                {
                    PaidByUserId = c.StudentUserIds[1],
                    StudentId = c.StudentIds[1],
                    Amount = StandardPrice,
                    PaymentMethod = PaymentMethod.Momo,
                    Status = PaymentStatus.Completed,
                    PaymentDate = start,
                    TransactionId = "SEEDPAYOLD01",
                };
                db.Payments.Add(pay);
                await db.SaveChangesAsync();
                db.Subscriptions.Add(new Subscription
                {
                    StudentId = c.StudentIds[1],
                    PackageId = c.StandardPackageId,
                    PaymentId = pay.PaymentId,
                    StartDate = start,
                    EndDate = start.AddDays(30),
                    Status = SubscriptionStatus.Expired,
                    AmountPaid = StandardPrice,
                    CreatedAt = start,
                });
                await db.SaveChangesAsync();
            }

            // an abandoned checkout for a Free student (index 10) — kept recent so the
            // subscription-lifecycle sweep doesn't immediately cancel it
            {
                var when = _now.AddMinutes(-4);
                var pay = new Payment
                {
                    PaidByUserId = c.StudentUserIds[10],
                    StudentId = c.StudentIds[10],
                    Amount = PremiumPrice,
                    PaymentMethod = PaymentMethod.BankTransfer,
                    Status = PaymentStatus.Pending,
                    PaymentDate = when,
                    Notes = "Chưa nhận được chuyển khoản (demo).",
                };
                db.Payments.Add(pay);
                await db.SaveChangesAsync();
                db.Subscriptions.Add(new Subscription
                {
                    StudentId = c.StudentIds[10],
                    PackageId = c.PremiumPackageId,
                    PaymentId = pay.PaymentId,
                    StartDate = when,
                    EndDate = when.AddDays(30),
                    Status = SubscriptionStatus.Pending,
                    AmountPaid = 0,
                    CreatedAt = when,
                });
                await db.SaveChangesAsync();
            }

            // two single-course purchases (Free students buy one textbook outright)
            await SeedCoursePurchaseAsync(c, 11, DemoContent.CourseCtstSlug);
            await SeedCoursePurchaseAsync(c, 12, DemoContent.CourseCdSlug);
        }

        private async Task SeedCoursePurchaseAsync(SeedContext c, int studentIndex, string courseSlug)
        {
            var course = await db.Courses.FirstAsync(x => x.Slug == courseSlug);
            var version = await db.CourseVersions.FirstAsync(x => x.CourseId == course.CourseId && x.State == VersionState.Published);
            int studentId = c.StudentIds[studentIndex];
            int userId = c.StudentUserIds[studentIndex];
            var when = _now.AddDays(-_rng.Next(5, 40));
            decimal price = course.SalePrice ?? course.ListPrice;

            var order = new Order
            {
                BuyerUserId = userId,
                Status = OrderStatus.Paid,
                SubtotalAmount = price,
                DiscountAmount = 0,
                TotalAmount = price,
                CreatedAt = when,
                PaidAt = when,
            };
            db.Orders.Add(order);
            await db.SaveChangesAsync();

            db.OrderItems.Add(new OrderItem
            {
                OrderId = order.OrderId,
                ItemType = OrderItemType.Course,
                CourseId = course.CourseId,
                BeneficiaryStudentId = studentId,
                UnitPrice = price,
                DiscountAmount = 0,
                Quantity = 1,
            });
            db.Payments.Add(new Payment
            {
                OrderId = order.OrderId,
                PaidByUserId = userId,
                StudentId = studentId,
                Amount = price,
                PaymentMethod = PaymentMethod.VNPay,
                Status = PaymentStatus.Completed,
                PaymentDate = when,
                TransactionId = $"SEEDORD{studentIndex:00}",
            });
            db.StudentCourses.Add(new StudentCourse
            {
                StudentId = studentId,
                CourseId = course.CourseId,
                CourseVersionId = version.CourseVersionId,
                Source = EnrollSource.Purchase,
                Status = StudentCourseStatus.Active,
                ProgressPercent = 0,
                EnrolledAt = when,
                AccessExpiresAt = course.AccessDurationDays.HasValue ? when.AddDays(course.AccessDurationDays.Value) : null,
            });
            await db.SaveChangesAsync();
            c.EnrolledVersions.Add((studentId, version.CourseVersionId));
        }

        // ==============================================================
        //  attempt history
        // ==============================================================
        private async Task SeedAttemptHistoryAsync(SeedContext c)
        {
            var participants = new List<(int idx, AccessTier tier, int count)>();
            for (int i = 0; i < 10; i++)
                participants.Add((i, i >= 5 ? AccessTier.Premium : AccessTier.Standard, 14 + _rng.Next(0, 26)));
            foreach (var i in new[] { 11, 12, 13, 14, 15, 16 })
                participants.Add((i, AccessTier.Free, 4 + _rng.Next(0, 10)));

            foreach (var (idx, tier, count) in participants)
            {
                int studentId = c.StudentIds[idx];
                var pool = c.Exercises.Where(e => e.IsFree || (int)e.RequiredTier <= (int)tier).ToList();
                if (pool.Count == 0) continue;

                double baseSkill = 0.45 + _rng.NextDouble() * 0.4;

                for (int a = 0; a < count; a++)
                {
                    var ex = pool[_rng.Next(pool.Count)];
                    int daysAgo = _rng.Next(0, 90);
                    var startTime = _now.Date.AddDays(-daysAgo)
                        .AddHours(7 + _rng.Next(0, 14))
                        .AddMinutes(_rng.Next(0, 60));
                    int cap = ex.DurationMinutes ?? 40;
                    int spentMin = Math.Max(3, 3 + _rng.Next(0, cap + 6));
                    var submittedAt = startTime.AddMinutes(spentMin);
                    var status = (ex.DurationMinutes.HasValue && spentMin > ex.DurationMinutes.Value)
                        ? AttemptStatus.Timeout
                        : AttemptStatus.Submitted;
                    double accuracy = Math.Clamp(baseSkill + (_rng.NextDouble() - 0.5) * 0.25, 0.15, 0.98);

                    var attempt = new ExerciseAttempt
                    {
                        StudentId = studentId,
                        ExerciseId = ex.ExerciseId,
                        StartTime = startTime,
                        PlannedEndTime = ex.DurationMinutes.HasValue ? startTime.AddMinutes(ex.DurationMinutes.Value) : null,
                        SubmittedAt = submittedAt,
                        Status = status,
                        MaxScore = ex.TotalScores,
                    };
                    db.ExerciseAttempts.Add(attempt);
                    await db.SaveChangesAsync();

                    double totalScore = 0;
                    int correctCount = 0, wrongCount = 0;
                    var answers = new List<StudentAnswer>();
                    foreach (var (qid, score) in ex.Questions)
                    {
                        var meta = c.QuestionMeta[qid];
                        bool correct = _rng.NextDouble() < accuracy;
                        var ans = new StudentAnswer
                        {
                            AttemptId = attempt.AttemptId,
                            QuestionId = qid,
                            IsCorrect = correct,
                            NeedsManualGrading = false,
                            PointsEarned = correct ? score : 0,
                            AnsweredAt = submittedAt,
                        };
                        if (meta.Type == QuestionType.FillBlank)
                            ans.AnswerText = correct ? FirstVariant(meta.CorrectAnswer) : "kết quả sai";
                        else
                            ans.SelectedOptionId = correct ? meta.CorrectOptionId : meta.WrongOptionId;

                        answers.Add(ans);
                        if (correct) { totalScore += score; correctCount++; } else wrongCount++;
                    }
                    db.StudentAnswers.AddRange(answers);

                    attempt.TotalScore = totalScore;
                    attempt.CorrectAnswers = correctCount;
                    attempt.WrongAnswers = wrongCount;
                    attempt.CompletionPercentage = attempt.MaxScore > 0
                        ? (decimal)(totalScore / attempt.MaxScore) * 100m
                        : 0m;
                    await db.SaveChangesAsync();

                    c.AttemptCount++;
                    AddActivity(c, idx, DateOnly.FromDateTime(startTime), spentMin, exercises: 1,
                        questions: ex.Questions.Count, lessons: 0);
                }
            }
        }

        private static string FirstVariant(string correctAnswer)
        {
            var head = correctAnswer.Split('|', ';')[0].Trim();
            return string.IsNullOrEmpty(head) ? correctAnswer : head;
        }

        // ==============================================================
        //  progress + daily snapshots
        // ==============================================================
        private async Task SeedProgressAsync(SeedContext c)
        {
            foreach (var (studentId, versionId) in c.EnrolledVersions.Distinct())
            {
                int studentIdx = c.StudentIds.IndexOf(studentId);
                var lessons = await db.ContentNodes.AsNoTracking()
                    .Where(n => n.CourseVersionId == versionId && n.NodeType == NodeType.Lesson && !n.IsHidden)
                    .OrderBy(n => n.MaterializedPath)
                    .Select(n => n.NodeId)
                    .ToListAsync();
                if (lessons.Count == 0) continue;

                int completed = Math.Max(1, (int)(lessons.Count * (0.15 + _rng.NextDouble() * 0.6)));
                for (int li = 0; li < lessons.Count; li++)
                {
                    bool done = li < completed;
                    bool inProgress = li == completed;
                    if (!done && !inProgress) continue;

                    var lastAccess = _now.AddDays(-_rng.Next(0, 75));
                    int timeSpent = 300 + _rng.Next(0, 1500);
                    db.NodeProgresses.Add(new NodeProgress
                    {
                        StudentId = studentId,
                        NodeId = lessons[li],
                        Status = done ? ProgressStatus.Completed : ProgressStatus.InProgress,
                        MasteryLevel = done
                            ? (_rng.Next(2) == 0 ? MasteryLevel.Advanced : MasteryLevel.Mastered)
                            : MasteryLevel.Beginner,
                        CompletionPercent = done ? 100m : 30m + _rng.Next(0, 40),
                        TimeSpentSeconds = timeSpent,
                        TotalAttempts = _rng.Next(0, 4),
                        CorrectCount = _rng.Next(3, 12),
                        WrongCount = _rng.Next(0, 6),
                        LastAccessedAt = lastAccess,
                    });

                    if (done && studentIdx >= 0)
                        AddActivity(c, studentIdx, DateOnly.FromDateTime(lastAccess), timeSpent / 60,
                            exercises: 0, questions: 0, lessons: 1);
                }
            }
            await db.SaveChangesAsync();

            foreach (var (studentIdx, byDate) in c.Activity)
            {
                int studentId = c.StudentIds[studentIdx];
                foreach (var (date, v) in byDate)
                    db.DailyActivitySnapshots.Add(new DailyActivitySnapshot
                    {
                        StudentId = studentId,
                        Date = date,
                        MinutesStudied = v[0],
                        ExercisesDone = v[1],
                        QuestionsAnswered = v[2],
                        LessonsDone = v[3],
                    });
            }
            await db.SaveChangesAsync();

            // guarantee a visible recent streak for a few students
            foreach (var idx in new[] { 0, 1, 5, 6 })
            {
                int studentId = c.StudentIds[idx];
                for (int d = 0; d < 6; d++)
                {
                    var date = DateOnly.FromDateTime(_now.Date.AddDays(-d));
                    var existing = await db.DailyActivitySnapshots
                        .FirstOrDefaultAsync(s => s.StudentId == studentId && s.Date == date);
                    if (existing == null)
                        db.DailyActivitySnapshots.Add(new DailyActivitySnapshot
                        {
                            StudentId = studentId,
                            Date = date,
                            MinutesStudied = 15 + _rng.Next(0, 40),
                            ExercisesDone = 1,
                            QuestionsAnswered = 8,
                            LessonsDone = _rng.Next(0, 2),
                        });
                    else
                    {
                        existing.MinutesStudied += 10;
                        existing.ExercisesDone += 1;
                    }
                }
            }
            await db.SaveChangesAsync();
        }

        private static void AddActivity(SeedContext c, int studentIdx, DateOnly date,
            int minutes, int exercises, int questions, int lessons)
        {
            if (!c.Activity.TryGetValue(studentIdx, out var byDate))
            {
                byDate = new Dictionary<DateOnly, int[]>();
                c.Activity[studentIdx] = byDate;
            }
            if (!byDate.TryGetValue(date, out var v))
            {
                v = new int[4];
                byDate[date] = v;
            }
            v[0] += minutes;
            v[1] += exercises;
            v[2] += questions;
            v[3] += lessons;
        }

        // ==============================================================
        //  parent links
        // ==============================================================
        private async Task SeedParentLinksAsync(SeedContext c)
        {
            for (int p = 0; p < c.ParentIds.Count; p++)
            {
                var childIndexes = new[] { p * 2, p * 2 + 1 }.Where(x => x < c.StudentIds.Count).ToArray();
                for (int k = 0; k < childIndexes.Length; k++)
                {
                    var status = LinkStatus.Active;
                    DateTime? revokedAt = null;
                    if ((p == 8 || p == 9) && k == 1) status = LinkStatus.Pending;
                    else if (p == 7 && k == 1) { status = LinkStatus.Revoked; revokedAt = _now.AddDays(-10); }

                    db.ParentLinks.Add(new ParentLink
                    {
                        ParentId = c.ParentIds[p],
                        StudentId = c.StudentIds[childIndexes[k]],
                        Relationship = k == 0 ? ParentRelationship.Mother : ParentRelationship.Father,
                        Status = status,
                        IsPrimaryGuardian = k == 0,
                        LinkedAt = _now.AddDays(-100),
                        RevokedAt = revokedAt,
                    });
                }
            }
            await db.SaveChangesAsync();
        }

        // ==============================================================
        //  notifications
        // ==============================================================
        private async Task SeedNotificationsAsync(SeedContext c)
        {
            var notifs = new List<Notification>();
            for (int i = 0; i < 6; i++)
            {
                notifs.Add(new Notification
                {
                    UserId = c.StudentUserIds[i],
                    StudentId = c.StudentIds[i],
                    Audience = NotifyAudience.Student,
                    Title = "Nhắc học tập",
                    Message = "Bạn chưa hoàn thành bài luyện tập hôm nay. Cùng ôn 10 phút nhé!",
                    NotificationType = NotificationType.Reminder,
                    IsRead = i % 2 == 0,
                    CreatedAt = _now.AddDays(-i),
                    ReadAt = i % 2 == 0 ? _now.AddDays(-i).AddHours(2) : null,
                });
                notifs.Add(new Notification
                {
                    UserId = c.ParentUserIds[i / 2],
                    StudentId = c.StudentIds[i],
                    Audience = NotifyAudience.Parent,
                    Title = "Kết quả học tập của con",
                    Message = "Con vừa hoàn thành một bài kiểm tra với điểm dưới 5. Bạn nên xem lại cùng con.",
                    NotificationType = NotificationType.Warning,
                    IsRead = false,
                    CreatedAt = _now.AddDays(-i),
                });
            }
            db.Notifications.AddRange(notifs);
            await db.SaveChangesAsync();
        }

        // ==============================================================
        //  helpers / data
        // ==============================================================
        private string RandCode(int n)
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var buf = new char[n];
            for (int i = 0; i < n; i++) buf[i] = chars[_rng.Next(chars.Length)];
            return new string(buf);
        }

        private static readonly string[] StudentNames =
        {
            "Nguyễn Minh An", "Trần Gia Bảo", "Lê Thảo Chi", "Phạm Đức Duy", "Hoàng Khánh Đan",
            "Vũ Nhật Hà", "Đặng Gia Hân", "Bùi Quang Huy", "Đỗ Bảo Khánh", "Ngô Tuệ Lâm",
            "Dương Hải Long", "Lý Phương Mai", "Hồ Minh Nhật", "Đinh Yến Nhi", "Trịnh Gia Phúc",
            "Mai Thanh Quỳnh", "Phan Đăng Sơn", "Tạ Thu Trang", "Cao Anh Tú", "Lương Khả Vy",
        };

        private static readonly string[] ParentNames =
        {
            "Nguyễn Văn Hùng", "Trần Thị Lan", "Lê Quốc Thắng", "Phạm Thị Hồng", "Hoàng Văn Nam",
            "Vũ Thị Thu", "Đặng Minh Tuấn", "Bùi Thị Hằng", "Đỗ Văn Cường", "Ngô Thị Bích",
        };

        private static readonly string[] Schools =
        {
            "THCS Nguyễn Du", "THCS Lê Quý Đôn", "THCS Chu Văn An", "THCS Trần Phú", "THCS Ngô Quyền",
        };

        private static readonly string[] Jobs =
        {
            "Kỹ sư", "Giáo viên", "Nhân viên văn phòng", "Kinh doanh tự do", "Bác sĩ",
        };

        // ==============================================================
        //  scratch state carried between phases
        // ==============================================================
        private sealed class SeedContext
        {
            public int AdminUserId;
            public int EditorUserId;
            public int ReviewerUserId;

            public readonly List<int> StudentUserIds = new();
            public readonly List<int> StudentIds = new();
            public readonly List<int> ParentUserIds = new();
            public readonly List<int> ParentIds = new();

            public int StandardPackageId;
            public int PremiumPackageId;

            public int KnttCourseId;
            public int KnttVersionId;
            public readonly List<int> KnttChapterNodeIds = new();
            public readonly List<List<int>> KnttChapterLessonIds = new();
            public readonly List<int> KnttShowcaseLessonIds = new();

            public readonly List<int> ChapterBankIds = new();
            public readonly List<List<int>> ChapterQuestionIds = new();
            public readonly List<int> AllQuestionIds = new();
            public readonly Dictionary<int, QMeta> QuestionMeta = new();

            public readonly List<SeedExercise> Exercises = new();

            public readonly List<(int studentId, int versionId)> EnrolledVersions = new();
            public readonly Dictionary<int, Dictionary<DateOnly, int[]>> Activity = new();
            public int AttemptCount;
        }

        private sealed class QMeta
        {
            public QuestionType Type;
            public int? CorrectOptionId;
            public int? WrongOptionId;
            public string CorrectAnswer = string.Empty;
        }

        private sealed class SeedExercise
        {
            public int ExerciseId;
            public int? NodeId;
            public double TotalScores;
            public int? DurationMinutes;
            public bool IsFree;
            public AccessTier RequiredTier;
            public readonly List<(int questionId, double score)> Questions = new();
        }
    }
}
