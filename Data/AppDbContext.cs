using ELearning_ToanHocHay_Control.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ELearning_ToanHocHay_Control.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions options) : base(options)
        {
        }

        protected AppDbContext()
        {
        }

        #region DbSet

        // --- Người dùng ---
        public DbSet<User> Users { get; set; }
        public DbSet<EmailVerificationToken> EmailVerificationTokens { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Parent> Parents { get; set; }
        public DbSet<ParentLink> ParentLinks { get; set; }
        public DbSet<ParentInvite> ParentInvites { get; set; }

        // --- Tầng 1: Danh mục ---
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<GradeLevel> GradeLevels { get; set; }
        public DbSet<CurriculumFramework> CurriculumFrameworks { get; set; }

        // --- Tầng 2: Khoá học ---
        public DbSet<Course> Courses { get; set; }
        public DbSet<CourseVersion> CourseVersions { get; set; }
        public DbSet<CourseBundle> CourseBundles { get; set; }
        public DbSet<CourseBundleItem> CourseBundleItems { get; set; }

        // --- Tầng 3: Cây nội dung ---
        public DbSet<ContentNode> ContentNodes { get; set; }
        public DbSet<NodeTypeRule> NodeTypeRules { get; set; }
        public DbSet<LessonDetail> LessonDetails { get; set; }
        public DbSet<NodeRevision> NodeRevisions { get; set; }
        public DbSet<ContentBlock> ContentBlocks { get; set; }
        public DbSet<LessonResource> LessonResources { get; set; }
        public DbSet<FlashcardDeck> FlashcardDecks { get; set; }
        public DbSet<Flashcard> Flashcards { get; set; }
        public DbSet<MediaAsset> MediaAssets { get; set; }
        public DbSet<ContentImportJob> ContentImportJobs { get; set; }

        // --- Tầng 4: Kỹ năng ---
        public DbSet<Skill> Skills { get; set; }
        public DbSet<NodeSkill> NodeSkills { get; set; }
        public DbSet<QuestionSkill> QuestionSkills { get; set; }

        // --- Duyệt nội dung ---
        public DbSet<ContentReview> ContentReviews { get; set; }
        public DbSet<ReviewComment> ReviewComments { get; set; }

        // --- Ngân hàng câu hỏi & bài tập ---
        public DbSet<QuestionBank> QuestionBanks { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<QuestionOption> QuestionOptions { get; set; }
        public DbSet<QuestionTag> QuestionTags { get; set; }
        public DbSet<QuestionNode> QuestionNodes { get; set; }
        public DbSet<Exercise> Exercises { get; set; }
        public DbSet<ExerciseQuestion> ExerciseQuestions { get; set; }
        public DbSet<ExerciseAttempt> ExerciseAttempts { get; set; }
        public DbSet<StudentAnswer> StudentAnswers { get; set; }
        public DbSet<AIFeedback> AIFeedbacks { get; set; }
        public DbSet<AIHint> AIHints { get; set; }
        public DbSet<TabSwitchLog> TabSwitchLogs { get; set; }

        // --- Tiến độ ---
        public DbSet<StudentCourse> StudentCourses { get; set; }
        public DbSet<NodeProgress> NodeProgresses { get; set; }
        public DbSet<SkillProgress> SkillProgresses { get; set; }
        public DbSet<DailyActivitySnapshot> DailyActivitySnapshots { get; set; }
        public DbSet<AiUsageDaily> AiUsageDailies { get; set; }
        public DbSet<LearningPath> LearningPaths { get; set; }

        // --- Khách chưa đăng nhập ---
        public DbSet<GuestSession> GuestSessions { get; set; }
        public DbSet<GuestIpUsage> GuestIpUsages { get; set; }

        // --- Gói cước, đơn hàng, thanh toán, khuyến mãi ---
        public DbSet<Package> Packages { get; set; }
        public DbSet<PackageEntitlement> PackageEntitlements { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<SubscriptionMember> SubscriptionMembers { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<SePayIpnLog> SePayIpnLogs { get; set; }
        public DbSet<Promotion> Promotions { get; set; }
        public DbSet<PromotionScope> PromotionScopes { get; set; }
        public DbSet<PromotionRedemption> PromotionRedemptions { get; set; }

        // --- Hỗ trợ, thông báo, hệ thống ---
        public DbSet<ChatConversation> ChatConversations { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<SupportTicket> SupportTickets { get; set; }
        public DbSet<SupportMessage> SupportMessages { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<NotificationPreference> NotificationPreferences { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<SystemConfig> SystemConfigs { get; set; }
        public DbSet<StaticPage> StaticPages { get; set; }

        #endregion

        #region OnModelCreating
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ============================================================
            // NGƯỜI DÙNG
            // ============================================================
            modelBuilder.Entity<User>(e =>
            {
                e.ToTable("User");
                e.HasKey(x => x.UserId);
                e.Property(x => x.Email).IsRequired().HasMaxLength(255);
                e.HasIndex(x => x.Email).IsUnique();
                e.Property(x => x.FullName).IsRequired().HasMaxLength(255);
                e.Property(x => x.UserType).HasConversion<string>();
            });

            modelBuilder.Entity<EmailVerificationToken>(e =>
            {
                e.ToTable("EmailVerificationToken");
                e.HasKey(x => x.Id);
                e.Property(x => x.Token).IsRequired().HasMaxLength(255);
                e.HasIndex(x => x.Token).IsUnique();
                e.Property(x => x.ExpiredAt).IsRequired();
                e.Property(x => x.IsUsed).HasDefaultValue(false);
                e.Property(x => x.CreatedAt).HasDefaultValueSql("NOW()");
                e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<RefreshToken>(e =>
            {
                e.ToTable("RefreshToken");
                e.HasKey(x => x.RefreshTokenId);
                e.HasIndex(x => x.TokenHash).IsUnique();
                e.HasIndex(x => x.UserId);
                e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<PasswordResetToken>(e =>
            {
                e.ToTable("PasswordResetToken");
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.Token).IsUnique();
                e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Student>(e =>
            {
                e.ToTable("Student");
                e.HasKey(x => x.StudentId);
                e.HasOne(s => s.User).WithOne(u => u.Student)
                    .HasForeignKey<Student>(s => s.UserId);
                e.HasOne(s => s.CurrentGradeLevel).WithMany()
                    .HasForeignKey(s => s.CurrentGradeLevelId);
                e.Property(x => x.AiDataSharingLevel).HasConversion<string>();
            });

            modelBuilder.Entity<Parent>(e =>
            {
                e.ToTable("Parent");
                e.HasKey(x => x.ParentId);
                e.HasOne(p => p.User).WithOne(u => u.Parent)
                    .HasForeignKey<Parent>(p => p.UserId);
            });

            modelBuilder.Entity<ParentLink>(e =>
            {
                e.ToTable("ParentLink");
                e.HasKey(x => x.ParentLinkId);
                e.HasIndex(x => new { x.ParentId, x.StudentId }).IsUnique();
                e.Property(x => x.Relationship).HasConversion<string>();
                e.Property(x => x.Status).HasConversion<string>();
                e.HasOne(x => x.Parent).WithMany(p => p.ParentLinks).HasForeignKey(x => x.ParentId);
                e.HasOne(x => x.Student).WithMany(s => s.ParentLinks).HasForeignKey(x => x.StudentId);
            });

            modelBuilder.Entity<ParentInvite>(e =>
            {
                e.ToTable("ParentInvite");
                e.HasKey(x => x.ParentInviteId);
                e.HasIndex(x => x.Token).IsUnique();
                e.Property(x => x.Status).HasConversion<string>();
                e.HasOne(x => x.Parent).WithMany(p => p.ParentInvites).HasForeignKey(x => x.ParentId);
                e.HasOne(x => x.AcceptedByStudent).WithMany().HasForeignKey(x => x.AcceptedByStudentId);
            });

            // ============================================================
            // TẦNG 1 — DANH MỤC
            // ============================================================
            modelBuilder.Entity<Subject>(e =>
            {
                e.ToTable("Subject");
                e.HasKey(x => x.SubjectId);
                e.HasIndex(x => x.Code).IsUnique();
                e.HasIndex(x => x.Slug).IsUnique();
                e.Property(x => x.Description).HasColumnType("text");
            });

            modelBuilder.Entity<GradeLevel>(e =>
            {
                e.ToTable("GradeLevel");
                e.HasKey(x => x.GradeLevelId);
                e.HasIndex(x => x.Code).IsUnique();
                e.Property(x => x.Stage).HasConversion<string>();
            });

            modelBuilder.Entity<CurriculumFramework>(e =>
            {
                e.ToTable("CurriculumFramework");
                e.HasKey(x => x.FrameworkId);
                e.HasIndex(x => x.Code).IsUnique();
            });

            // ============================================================
            // TẦNG 2 — KHOÁ HỌC
            // ============================================================
            modelBuilder.Entity<Course>(e =>
            {
                e.ToTable("Course");
                e.HasKey(x => x.CourseId);
                e.HasIndex(x => x.Slug).IsUnique();
                e.HasIndex(x => new { x.SubjectId, x.GradeLevelId, x.FrameworkId }).IsUnique();
                e.Property(x => x.Status).HasConversion<string>();
                e.HasOne(x => x.Subject).WithMany(s => s.Courses).HasForeignKey(x => x.SubjectId);
                e.HasOne(x => x.GradeLevel).WithMany(g => g.Courses).HasForeignKey(x => x.GradeLevelId);
                e.HasOne(x => x.Framework).WithMany(f => f.Courses).HasForeignKey(x => x.FrameworkId);
                e.HasOne(x => x.Creator).WithMany(u => u.CreatedCourses).HasForeignKey(x => x.CreatedBy);
            });

            modelBuilder.Entity<CourseVersion>(e =>
            {
                e.ToTable("CourseVersion");
                e.HasKey(x => x.CourseVersionId);
                e.HasIndex(x => new { x.CourseId, x.VersionNumber }).IsUnique();
                e.HasIndex(x => x.CourseId)
                    .IsUnique()
                    .HasFilter("\"State\" = 'Published'");   // tối đa 1 version Published / Course
                e.Property(x => x.State).HasConversion<string>();
                e.HasOne(x => x.Course).WithMany(c => c.Versions).HasForeignKey(x => x.CourseId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.Submitter).WithMany().HasForeignKey(x => x.SubmittedBy);
                e.HasOne(x => x.Publisher).WithMany().HasForeignKey(x => x.PublishedBy);
            });

            modelBuilder.Entity<CourseBundle>(e =>
            {
                e.ToTable("CourseBundle");
                e.HasKey(x => x.CourseBundleId);
                e.HasIndex(x => x.Slug).IsUnique();
            });

            modelBuilder.Entity<CourseBundleItem>(e =>
            {
                e.ToTable("CourseBundleItem");
                e.HasKey(x => new { x.CourseBundleId, x.CourseId });
                e.HasOne(x => x.Bundle).WithMany(b => b.Items).HasForeignKey(x => x.CourseBundleId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.Course).WithMany(c => c.BundleItems).HasForeignKey(x => x.CourseId);
            });

            // ============================================================
            // TẦNG 3 — CÂY NỘI DUNG
            // ============================================================
            modelBuilder.Entity<ContentNode>(e =>
            {
                e.ToTable("ContentNode", t =>
                {
                    t.HasCheckConstraint("CK_ContentNode_Depth", "\"Depth\" >= 0");
                });
                e.HasKey(x => x.NodeId);
                e.Property(x => x.NodeType).HasConversion<string>();
                e.HasIndex(x => new { x.CourseVersionId, x.ParentNodeId, x.OrderIndex });
                e.HasIndex(x => x.MaterializedPath);
                e.HasOne(x => x.CourseVersion).WithMany(v => v.Nodes).HasForeignKey(x => x.CourseVersionId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.Parent).WithMany(p => p.Children).HasForeignKey(x => x.ParentNodeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<NodeTypeRule>(e =>
            {
                e.ToTable("NodeTypeRule");
                e.HasKey(x => x.NodeTypeRuleId);
                e.Property(x => x.ParentType).HasConversion<string>();
                e.Property(x => x.ChildType).HasConversion<string>();
                e.HasOne(x => x.Subject).WithMany().HasForeignKey(x => x.SubjectId);
                e.HasIndex(x => new { x.SubjectId, x.ParentType, x.ChildType }).IsUnique();
            });

            modelBuilder.Entity<LessonDetail>(e =>
            {
                e.ToTable("LessonDetail");
                e.HasKey(x => x.NodeId);
                e.HasOne(x => x.Node).WithOne(n => n.LessonDetail).HasForeignKey<LessonDetail>(x => x.NodeId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<NodeRevision>(e =>
            {
                e.ToTable("NodeRevision");
                e.HasKey(x => x.RevisionId);
                e.HasIndex(x => new { x.NodeId, x.RevisionNumber }).IsUnique();
                e.HasOne(x => x.Node).WithMany(n => n.Revisions).HasForeignKey(x => x.NodeId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ContentBlock>(e =>
            {
                e.ToTable("ContentBlock");
                e.HasKey(x => x.BlockId);
                e.Property(x => x.BlockType).HasConversion<string>();
                e.HasIndex(x => new { x.NodeId, x.OrderIndex });
                e.HasOne(x => x.Node).WithMany(n => n.Blocks).HasForeignKey(x => x.NodeId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<LessonResource>(e =>
            {
                e.ToTable("LessonResource");
                e.HasKey(x => x.ResourceId);
                e.Property(x => x.ResourceType).HasConversion<string>();
                e.HasOne(x => x.Node).WithMany(n => n.Resources).HasForeignKey(x => x.NodeId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.MediaAsset).WithMany().HasForeignKey(x => x.MediaAssetId);
            });

            modelBuilder.Entity<FlashcardDeck>(e =>
            {
                e.ToTable("FlashcardDeck");
                e.HasKey(x => x.DeckId);
                e.HasOne(x => x.Node).WithMany(n => n.FlashcardDecks).HasForeignKey(x => x.NodeId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Flashcard>(e =>
            {
                e.ToTable("Flashcard");
                e.HasKey(x => x.CardId);
                e.HasOne(x => x.Deck).WithMany(d => d.Cards).HasForeignKey(x => x.DeckId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<MediaAsset>(e =>
            {
                e.ToTable("MediaAsset");
                e.HasKey(x => x.MediaAssetId);
                e.HasOne(x => x.Uploader).WithMany().HasForeignKey(x => x.UploadedBy);
            });

            modelBuilder.Entity<ContentImportJob>(e =>
            {
                e.ToTable("ContentImportJob");
                e.HasKey(x => x.ImportJobId);
                e.Property(x => x.TargetType).HasConversion<string>();
                e.Property(x => x.Status).HasConversion<string>();
                e.HasOne(x => x.Uploader).WithMany().HasForeignKey(x => x.UploadedBy);
                e.HasOne(x => x.CourseVersion).WithMany().HasForeignKey(x => x.CourseVersionId);
            });

            // ============================================================
            // TẦNG 4 — KỸ NĂNG
            // ============================================================
            modelBuilder.Entity<Skill>(e =>
            {
                e.ToTable("Skill");
                e.HasKey(x => x.SkillId);
                e.HasIndex(x => new { x.SubjectId, x.Code }).IsUnique();
                e.HasOne(x => x.Subject).WithMany(s => s.Skills).HasForeignKey(x => x.SubjectId);
                e.HasOne(x => x.Parent).WithMany(p => p.Children).HasForeignKey(x => x.ParentSkillId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<NodeSkill>(e =>
            {
                e.ToTable("NodeSkill");
                e.HasKey(x => new { x.NodeId, x.SkillId });
                e.HasOne(x => x.Node).WithMany(n => n.NodeSkills).HasForeignKey(x => x.NodeId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.Skill).WithMany(s => s.NodeSkills).HasForeignKey(x => x.SkillId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<QuestionSkill>(e =>
            {
                e.ToTable("QuestionSkill");
                e.HasKey(x => new { x.QuestionId, x.SkillId });
                e.HasOne(x => x.Question).WithMany(q => q.QuestionSkills).HasForeignKey(x => x.QuestionId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.Skill).WithMany(s => s.QuestionSkills).HasForeignKey(x => x.SkillId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ============================================================
            // DUYỆT NỘI DUNG
            // ============================================================
            modelBuilder.Entity<ContentReview>(e =>
            {
                e.ToTable("ContentReview");
                e.HasKey(x => x.ReviewId);
                e.Property(x => x.Decision).HasConversion<string>();
                e.HasOne(x => x.CourseVersion).WithMany(v => v.Reviews).HasForeignKey(x => x.CourseVersionId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.Reviewer).WithMany().HasForeignKey(x => x.ReviewerId);
            });

            modelBuilder.Entity<ReviewComment>(e =>
            {
                e.ToTable("ReviewComment");
                e.HasKey(x => x.CommentId);
                e.Property(x => x.Status).HasConversion<string>();
                e.HasOne(x => x.Review).WithMany(r => r.Comments).HasForeignKey(x => x.ReviewId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.Node).WithMany().HasForeignKey(x => x.NodeId);
                e.HasOne(x => x.Block).WithMany().HasForeignKey(x => x.BlockId);
            });

            // ============================================================
            // NGÂN HÀNG CÂU HỎI & BÀI TẬP
            // ============================================================
            modelBuilder.Entity<QuestionBank>(e =>
            {
                e.ToTable("QuestionBank");
                e.HasKey(x => x.BankId);
                e.HasOne(x => x.Subject).WithMany(s => s.QuestionBanks).HasForeignKey(x => x.SubjectId);
                e.HasOne(x => x.GradeLevel).WithMany(g => g.QuestionBanks).HasForeignKey(x => x.GradeLevelId);
                e.HasOne(x => x.Course).WithMany().HasForeignKey(x => x.CourseId);
                e.HasOne(x => x.PrimaryNode).WithMany().HasForeignKey(x => x.PrimaryNodeId);
                e.HasOne(x => x.Creator).WithMany().HasForeignKey(x => x.CreatedBy);
            });

            modelBuilder.Entity<Tag>(e =>
            {
                e.ToTable("Tag");
                e.HasKey(x => x.TagId);
                e.Property(x => x.TagType).HasConversion<string>();
            });

            modelBuilder.Entity<Question>(e =>
            {
                e.ToTable("Question");
                e.HasKey(x => x.QuestionId);
                e.Property(x => x.QuestionType).HasConversion<string>();
                e.Property(x => x.DifficultyLevel).HasConversion<string>();
                e.Property(x => x.Status).HasConversion<string>();
                e.HasOne(x => x.QuestionBank).WithMany(qb => qb.Questions).HasForeignKey(x => x.BankId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.Subject).WithMany().HasForeignKey(x => x.SubjectId);
                e.HasOne(x => x.Creator).WithMany(u => u.CreatedQuestions).HasForeignKey(x => x.CreatedBy);
                e.HasOne(x => x.Reviewer).WithMany(u => u.ReviewedQuestions).HasForeignKey(x => x.ReviewedBy);
            });

            modelBuilder.Entity<QuestionOption>(e =>
            {
                e.ToTable("QuestionOption");
                e.HasKey(x => x.OptionId);
                e.HasOne(x => x.Question).WithMany(q => q.QuestionOptions).HasForeignKey(x => x.QuestionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<QuestionTag>(e =>
            {
                e.ToTable("QuestionTag");
                e.HasKey(x => new { x.QuestionId, x.TagId });
                e.HasOne(x => x.Question).WithMany(q => q.QuestionTags).HasForeignKey(x => x.QuestionId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.Tag).WithMany(t => t.QuestionTags).HasForeignKey(x => x.TagId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<QuestionNode>(e =>
            {
                e.ToTable("QuestionNode");
                e.HasKey(x => new { x.QuestionId, x.NodeId });
                e.HasOne(x => x.Question).WithMany(q => q.QuestionNodes).HasForeignKey(x => x.QuestionId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.Node).WithMany(n => n.QuestionNodes).HasForeignKey(x => x.NodeId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Exercise>(e =>
            {
                e.ToTable("Exercise");
                e.HasKey(x => x.ExerciseId);
                e.Property(x => x.ExerciseType).HasConversion<string>();
                e.Property(x => x.Status).HasConversion<string>();
                e.Property(x => x.RequiredTier).HasConversion<string>();
                e.HasOne(x => x.Node).WithMany(n => n.Exercises).HasForeignKey(x => x.NodeId);
                e.HasOne(x => x.Creator).WithMany(u => u.Exercises).HasForeignKey(x => x.CreatedBy);
            });

            modelBuilder.Entity<ExerciseQuestion>(e =>
            {
                e.ToTable("ExerciseQuestion");
                e.HasKey(x => new { x.ExerciseId, x.QuestionId });
                e.HasOne(x => x.Exercise).WithMany(ex => ex.ExerciseQuestions).HasForeignKey(x => x.ExerciseId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.Question).WithMany(q => q.ExerciseQuestions).HasForeignKey(x => x.QuestionId);
            });

            modelBuilder.Entity<ExerciseAttempt>(e =>
            {
                e.ToTable("ExerciseAttempt", t =>
                {
                    t.HasCheckConstraint("CK_ExerciseAttempt_Owner",
                        "(\"StudentId\" IS NOT NULL AND \"GuestSessionId\" IS NULL) OR (\"StudentId\" IS NULL AND \"GuestSessionId\" IS NOT NULL)");
                });
                e.HasKey(x => x.AttemptId);
                e.Property(x => x.Status).HasConversion<string>();
                e.Property(x => x.CompletionPercentage).HasPrecision(18, 2);
                e.HasIndex(x => new { x.StudentId, x.StartTime });
                e.HasIndex(x => new { x.StudentId, x.ExerciseId, x.Status });     // P7 — resume / MaxAttempts
                e.HasIndex(x => new { x.StudentId, x.Status, x.SubmittedAt });    // P7 — dashboard / history
                e.HasIndex(x => x.GuestSessionId);
                e.HasOne(x => x.Student).WithMany(s => s.ExerciseAttempts).HasForeignKey(x => x.StudentId);
                e.HasOne(x => x.GuestSession).WithMany().HasForeignKey(x => x.GuestSessionId);
                e.HasOne(x => x.Exercise).WithMany(ex => ex.ExerciseAttempts).HasForeignKey(x => x.ExerciseId);
            });

            modelBuilder.Entity<StudentAnswer>(e =>
            {
                e.ToTable("StudentAnswer");
                e.HasKey(x => x.AnswerId);
                e.HasIndex(x => new { x.AttemptId, x.QuestionId }).IsUnique();
                e.HasOne(x => x.ExerciseAttempt).WithMany(a => a.StudentAnswers).HasForeignKey(x => x.AttemptId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.Question).WithMany(q => q.StudentAnswers).HasForeignKey(x => x.QuestionId);
                e.HasOne(x => x.SelectedOption).WithMany().HasForeignKey(x => x.SelectedOptionId).IsRequired(false);
            });

            modelBuilder.Entity<AIFeedback>(e =>
            {
                e.ToTable("AIFeedback");
                e.HasKey(x => x.FeedbackId);
                e.HasOne(x => x.Attempt).WithMany(a => a.AIFeedbacks).HasForeignKey(x => x.AttemptId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.Question).WithMany(q => q.AIFeedbacks).HasForeignKey(x => x.QuestionId);
            });

            modelBuilder.Entity<AIHint>(e =>
            {
                e.ToTable("AIHint");
                e.HasKey(x => x.HintId);
                e.HasOne(x => x.Attempt).WithMany(a => a.AIHints).HasForeignKey(x => x.AttemptId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.Question).WithMany(q => q.AIHints).HasForeignKey(x => x.QuestionId);
            });

            modelBuilder.Entity<TabSwitchLog>(e =>
            {
                e.ToTable("TabSwitchLog");
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.AttemptId);   // P7
                e.HasOne(x => x.Attempt).WithMany().HasForeignKey(x => x.AttemptId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ============================================================
            // TIẾN ĐỘ
            // ============================================================
            modelBuilder.Entity<StudentCourse>(e =>
            {
                e.ToTable("StudentCourse");
                e.HasKey(x => x.StudentCourseId);
                e.HasIndex(x => new { x.StudentId, x.CourseId }).IsUnique();
                e.Property(x => x.Source).HasConversion<string>();
                e.Property(x => x.Status).HasConversion<string>();
                e.HasOne(x => x.Student).WithMany(s => s.StudentCourses).HasForeignKey(x => x.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.Course).WithMany(c => c.StudentCourses).HasForeignKey(x => x.CourseId);
                e.HasOne(x => x.CourseVersion).WithMany(v => v.StudentCourses).HasForeignKey(x => x.CourseVersionId);
            });

            modelBuilder.Entity<NodeProgress>(e =>
            {
                e.ToTable("NodeProgress");
                e.HasKey(x => x.NodeProgressId);
                e.HasIndex(x => new { x.StudentId, x.NodeId }).IsUnique();
                e.Property(x => x.Status).HasConversion<string>();
                e.Property(x => x.MasteryLevel).HasConversion<string>();
                e.HasOne(x => x.Student).WithMany(s => s.NodeProgresses).HasForeignKey(x => x.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.Node).WithMany(n => n.NodeProgresses).HasForeignKey(x => x.NodeId);
            });

            modelBuilder.Entity<SkillProgress>(e =>
            {
                e.ToTable("SkillProgress");
                e.HasKey(x => x.SkillProgressId);
                e.HasIndex(x => new { x.StudentId, x.SkillId }).IsUnique();
                e.HasOne(x => x.Student).WithMany(s => s.SkillProgresses).HasForeignKey(x => x.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.Skill).WithMany(s => s.SkillProgresses).HasForeignKey(x => x.SkillId);
            });

            modelBuilder.Entity<DailyActivitySnapshot>(e =>
            {
                e.ToTable("DailyActivitySnapshot");
                e.HasKey(x => new { x.StudentId, x.Date });
                e.HasOne(x => x.Student).WithMany(s => s.DailyActivitySnapshots).HasForeignKey(x => x.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<AiUsageDaily>(e =>
            {
                e.ToTable("AiUsageDaily");
                e.HasKey(x => new { x.StudentId, x.Date });
                e.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<LearningPath>(e =>
            {
                e.ToTable("LearningPath");
                e.HasKey(x => x.PathId);
                e.HasOne(x => x.Student).WithMany(s => s.LearningPaths).HasForeignKey(x => x.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ============================================================
            // KHÁCH CHƯA ĐĂNG NHẬP
            // ============================================================
            modelBuilder.Entity<GuestSession>(e =>
            {
                e.ToTable("GuestSession");
                e.HasKey(x => x.GuestSessionId);
                e.HasOne(x => x.GradeLevel).WithMany().HasForeignKey(x => x.GradeLevelId);
                e.HasOne(x => x.ConvertedToStudent).WithMany().HasForeignKey(x => x.ConvertedToStudentId);
            });

            modelBuilder.Entity<GuestIpUsage>(e =>
            {
                e.ToTable("GuestIpUsage");
                e.HasKey(x => new { x.IpHash, x.Date });
            });

            // ============================================================
            // GÓI CƯỚC · ĐƠN HÀNG · THANH TOÁN · KHUYẾN MÃI
            // ============================================================
            modelBuilder.Entity<Package>(e =>
            {
                e.ToTable("Package");
                e.HasKey(x => x.PackageId);
                e.Property(x => x.Tier).HasConversion<string>();
                e.HasOne(x => x.User).WithMany(u => u.Packages).HasForeignKey(x => x.UserId);
            });

            modelBuilder.Entity<PackageEntitlement>(e =>
            {
                e.ToTable("PackageEntitlement");
                e.HasKey(x => x.PackageEntitlementId);
                e.Property(x => x.ScopeType).HasConversion<string>();
                e.HasOne(x => x.Package).WithMany(p => p.Entitlements).HasForeignKey(x => x.PackageId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.Subject).WithMany().HasForeignKey(x => x.SubjectId);
                e.HasOne(x => x.GradeLevel).WithMany().HasForeignKey(x => x.GradeLevelId);
                e.HasOne(x => x.Course).WithMany(c => c.PackageEntitlements).HasForeignKey(x => x.CourseId);
            });

            modelBuilder.Entity<Subscription>(e =>
            {
                e.ToTable("Subscription");
                e.HasKey(x => x.SubscriptionId);
                e.Property(x => x.Status).HasConversion<string>();
                e.HasOne(x => x.Student).WithMany(s => s.Subscriptions).HasForeignKey(x => x.StudentId);
                e.HasOne(x => x.Package).WithMany(p => p.Subscriptions).HasForeignKey(x => x.PackageId);
                e.HasOne(x => x.Payment).WithOne(p => p.Subscription).HasForeignKey<Subscription>(x => x.PaymentId);
            });

            modelBuilder.Entity<SubscriptionMember>(e =>
            {
                e.ToTable("SubscriptionMember");
                e.HasKey(x => x.SubscriptionMemberId);
                e.HasIndex(x => new { x.SubscriptionId, x.StudentId }).IsUnique();
                e.HasOne(x => x.Subscription).WithMany(s => s.Members).HasForeignKey(x => x.SubscriptionId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.Student).WithMany(s => s.SubscriptionMemberships).HasForeignKey(x => x.StudentId);
            });

            modelBuilder.Entity<Order>(e =>
            {
                e.ToTable("Order");
                e.HasKey(x => x.OrderId);
                e.Property(x => x.Status).HasConversion<string>();
                e.HasOne(x => x.Buyer).WithMany().HasForeignKey(x => x.BuyerUserId);
            });

            modelBuilder.Entity<OrderItem>(e =>
            {
                e.ToTable("OrderItem");
                e.HasKey(x => x.OrderItemId);
                e.Property(x => x.ItemType).HasConversion<string>();
                e.HasOne(x => x.Order).WithMany(o => o.Items).HasForeignKey(x => x.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.Course).WithMany().HasForeignKey(x => x.CourseId);
                e.HasOne(x => x.Package).WithMany().HasForeignKey(x => x.PackageId);
                e.HasOne(x => x.Bundle).WithMany().HasForeignKey(x => x.CourseBundleId);
                e.HasOne(x => x.BeneficiaryStudent).WithMany(s => s.BenefitedOrderItems).HasForeignKey(x => x.BeneficiaryStudentId);
            });

            modelBuilder.Entity<Payment>(e =>
            {
                e.ToTable("Payment");
                e.HasKey(x => x.PaymentId);
                e.Property(x => x.PaymentMethod).HasConversion<string>();
                e.Property(x => x.Status).HasConversion<string>();
                e.HasOne(x => x.Order).WithMany(o => o.Payments).HasForeignKey(x => x.OrderId);
                e.HasOne(x => x.PaidByUser).WithMany().HasForeignKey(x => x.PaidByUserId);
                e.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId);
            });

            modelBuilder.Entity<SePayIpnLog>(e =>
            {
                e.ToTable("SePayIpnLog");
                e.HasKey(x => x.IpnLogId);
                e.HasIndex(x => x.ReferenceCode).IsUnique();
                e.HasIndex(x => x.SubscriptionId);
                e.Property(x => x.Outcome).HasConversion<string>();
            });

            modelBuilder.Entity<Promotion>(e =>
            {
                e.ToTable("Promotion");
                e.HasKey(x => x.PromotionId);
                e.HasIndex(x => x.Code).IsUnique().HasFilter("\"Code\" IS NOT NULL");
                e.Property(x => x.PromotionType).HasConversion<string>();
                e.Property(x => x.DiscountKind).HasConversion<string>();
            });

            modelBuilder.Entity<PromotionScope>(e =>
            {
                e.ToTable("PromotionScope");
                e.HasKey(x => x.PromotionScopeId);
                e.Property(x => x.ScopeType).HasConversion<string>();
                e.HasOne(x => x.Promotion).WithMany(p => p.Scopes).HasForeignKey(x => x.PromotionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<PromotionRedemption>(e =>
            {
                e.ToTable("PromotionRedemption");
                e.HasKey(x => x.RedemptionId);
                e.HasIndex(x => new { x.PromotionId, x.OrderId }).IsUnique();
                e.Property(x => x.Status).HasConversion<string>();
                e.HasOne(x => x.Promotion).WithMany(p => p.Redemptions).HasForeignKey(x => x.PromotionId);
                e.HasOne(x => x.Order).WithMany(o => o.PromotionRedemptions).HasForeignKey(x => x.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
            });

            // ============================================================
            // HỖ TRỢ · THÔNG BÁO · HỆ THỐNG
            // ============================================================
            modelBuilder.Entity<ChatConversation>(e =>
            {
                e.ToTable("ChatConversation");
                e.HasKey(x => x.ConversationId);
                e.Property(x => x.Status).HasConversion<string>();
                e.HasOne(x => x.Initiator).WithMany().HasForeignKey(x => x.InitiatorUserId);
                e.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId);
                e.HasOne(x => x.AssignedStaff).WithMany().HasForeignKey(x => x.AssignedStaffId);
            });

            modelBuilder.Entity<ChatMessage>(e =>
            {
                e.ToTable("ChatMessage");
                e.HasKey(x => x.MessageId);
                e.Property(x => x.SenderType).HasConversion<string>();
                e.HasIndex(x => new { x.ConversationId, x.SentAt });
                e.HasOne(x => x.Conversation).WithMany(c => c.Messages).HasForeignKey(x => x.ConversationId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.Sender).WithMany().HasForeignKey(x => x.SenderUserId);
            });

            modelBuilder.Entity<SupportTicket>(e =>
            {
                e.ToTable("SupportTicket");
                e.HasKey(x => x.TicketId);
                e.Property(x => x.Status).HasConversion<string>();
                e.Property(x => x.Priority).HasConversion<string>();
                e.HasOne(x => x.CreatedBy).WithMany(u => u.CreatedSupportTickets).HasForeignKey(x => x.CreatedByUserId);
                e.HasOne(x => x.AssignedStaff).WithMany(u => u.AssignedSupportTickets).HasForeignKey(x => x.AssignedToStaffId);
                e.HasOne(x => x.Conversation).WithMany().HasForeignKey(x => x.ConversationId);
            });

            modelBuilder.Entity<SupportMessage>(e =>
            {
                e.ToTable("SupportMessage");
                e.HasKey(x => x.MessageId);
                e.HasOne(x => x.Ticket).WithMany(t => t.Messages).HasForeignKey(x => x.TicketId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.Sender).WithMany(u => u.SupportMessages).HasForeignKey(x => x.SenderUserId);
            });

            modelBuilder.Entity<Notification>(e =>
            {
                e.ToTable("Notification");
                e.HasKey(x => x.NotificationId);
                e.HasIndex(x => new { x.UserId, x.IsRead });   // P7 — unread count / list
                e.Property(x => x.NotificationType).HasConversion<string>();
                e.Property(x => x.Audience).HasConversion<string>();
                e.HasOne(x => x.User).WithMany(u => u.Notifications).HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId);
            });

            modelBuilder.Entity<NotificationPreference>(e =>
            {
                e.ToTable("NotificationPreference");
                e.HasKey(x => new { x.UserId, x.RuleKey });
                e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<AuditLog>(e =>
            {
                e.ToTable("AuditLog");
                e.HasKey(x => x.LogId);
                e.HasIndex(x => new { x.EntityType, x.EntityId, x.CreatedAt });
                e.HasIndex(x => new { x.UserId, x.CreatedAt });
                e.HasOne(x => x.User).WithMany(u => u.AuditLogs).HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<SystemConfig>(e =>
            {
                e.ToTable("SystemConfig");
                e.HasKey(x => x.ConfigId);
                e.HasIndex(x => x.ConfigKey).IsUnique();
                e.Property(x => x.ConfigType).HasConversion<string>();
                e.HasOne(x => x.UpdatedByUser).WithMany(u => u.SystemConfigs).HasForeignKey(x => x.UpdatedBy);
            });

            modelBuilder.Entity<StaticPage>(e =>
            {
                e.ToTable("StaticPage");
                e.HasKey(x => x.StaticPageId);
                e.HasIndex(x => x.Slug).IsUnique();
            });

            SeedCatalog(modelBuilder);
            SeedSystemConfig(modelBuilder);
        }
        #endregion

        #region Seed
        private static void SeedCatalog(ModelBuilder mb)
        {
            // §12.1 — GradeLevel: chỉ G6 active, còn lại bật khi có nội dung.
            mb.Entity<GradeLevel>().HasData(
                new GradeLevel { GradeLevelId = 1, Code = "G6", Name = "Lớp 6", Stage = EducationStage.LowerSecondary, DisplayOrder = 6, IsActive = true },
                new GradeLevel { GradeLevelId = 2, Code = "G7", Name = "Lớp 7", Stage = EducationStage.LowerSecondary, DisplayOrder = 7, IsActive = false },
                new GradeLevel { GradeLevelId = 3, Code = "G8", Name = "Lớp 8", Stage = EducationStage.LowerSecondary, DisplayOrder = 8, IsActive = false },
                new GradeLevel { GradeLevelId = 4, Code = "G9", Name = "Lớp 9", Stage = EducationStage.LowerSecondary, DisplayOrder = 9, IsActive = false },
                new GradeLevel { GradeLevelId = 5, Code = "EXAM10", Name = "Ôn thi vào 10", Stage = EducationStage.ExamPrep, DisplayOrder = 10, IsActive = false }
            );

            // §12.1 — Subject: chỉ Toán.
            mb.Entity<Subject>().HasData(
                new Subject { SubjectId = 1, Code = "MATH", Name = "Toán", Slug = "toan", ColorHex = "#1f5fae", DisplayOrder = 1, IsActive = true }
            );

            // §12.1 — CurriculumFramework: cả 3 bộ SGK (sự thật cố định).
            mb.Entity<CurriculumFramework>().HasData(
                new CurriculumFramework { FrameworkId = 1, Code = "KNTT", Name = "Kết nối tri thức với cuộc sống", Publisher = "NXB Giáo dục Việt Nam", IsActive = true },
                new CurriculumFramework { FrameworkId = 2, Code = "CTST", Name = "Chân trời sáng tạo", Publisher = "NXB Giáo dục Việt Nam", IsActive = true },
                new CurriculumFramework { FrameworkId = 3, Code = "CD", Name = "Cánh Diều", Publisher = "Liên danh ĐHSP / VEPIC", IsActive = true }
            );

            // §11 — NodeTypeRule: 6 dòng luật mặc định (SubjectId = null).
            // Lesson không có dòng con ⇒ luôn là lá.
            mb.Entity<NodeTypeRule>().HasData(
                new NodeTypeRule { NodeTypeRuleId = 1, SubjectId = null, ParentType = null, ChildType = NodeType.Chapter },
                new NodeTypeRule { NodeTypeRuleId = 2, SubjectId = null, ParentType = NodeType.Chapter, ChildType = NodeType.Topic },
                new NodeTypeRule { NodeTypeRuleId = 3, SubjectId = null, ParentType = NodeType.Chapter, ChildType = NodeType.Lesson },
                new NodeTypeRule { NodeTypeRuleId = 4, SubjectId = null, ParentType = NodeType.Topic, ChildType = NodeType.SubTopic },
                new NodeTypeRule { NodeTypeRuleId = 5, SubjectId = null, ParentType = NodeType.Topic, ChildType = NodeType.Lesson },
                new NodeTypeRule { NodeTypeRuleId = 6, SubjectId = null, ParentType = NodeType.SubTopic, ChildType = NodeType.Lesson }
            );
        }

        private static void SeedSystemConfig(ModelBuilder mb)
        {
            var seededAt = new DateTime(2026, 8, 31, 0, 0, 0, DateTimeKind.Utc);
            (string key, string val, ConfigValueType type, string group, string note)[] rows =
            {
                ("guest.maxFreeLessons", "5", ConfigValueType.Int, "guest", "Xem N bài IsFree → tường mềm đăng ký"),
                ("guest.maxAttempts", "5", ConfigValueType.Int, "guest", "Lượt làm bài / session"),
                ("guest.maxAttemptsPerIpPerDay", "50", ConfigValueType.Int, "guest", "Rộng hơn để không khoá nhầm lớp NAT"),
                ("guest.session.retentionDays", "90", ConfigValueType.Int, "guest", "Dọn GuestSession chưa convert"),
                ("guest.ipUsage.retentionDays", "60", ConfigValueType.Int, "guest", "Dọn GuestIpUsage"),
                ("content.maxTreeDepth", "4", ConfigValueType.Int, "content", "Chapter → Topic → SubTopic → Lesson"),
                ("content.import.maxRowsPerJob", "2000", ConfigValueType.Int, "content", "Cap file import"),
                ("version.clone.timeoutSeconds", "120", ConfigValueType.Int, "content", "Timeout job clone version"),
                ("exercise.defaultMaxAttempts", "3", ConfigValueType.Int, "exercise", "Khi Exercise không tự đặt"),
                ("exercise.attempt.abandonTimeoutMinutes", "30", ConfigValueType.Int, "exercise", "InProgress quá hạn → Timeout"),
                ("promo.reservation.ttlMinutes", "20", ConfigValueType.Int, "promo", "Nhả PromotionRedemption Reserved quá hạn"),
                ("notify.inactivity.days", "3", ConfigValueType.Int, "notify", "Con nghỉ N ngày → báo phụ huynh"),
                ("notify.lowScore.threshold", "5.0", ConfigValueType.Decimal, "notify", "Điểm dưới X (thang 10)"),
                ("notify.parentDigest.dayOfWeek", "Monday", ConfigValueType.String, "notify", "Bản tổng hợp tuần"),
                ("support.phone", "", ConfigValueType.String, "support", "BẮT BUỘC đặt trước launch — số escalate"),
                ("support.chat.aiHandoffAfterTurns", "3", ConfigValueType.Int, "support", "AI thử N lượt → mời điện thoại/nhân viên"),
                ("support.ticket.slaFirstResponseHours", "24", ConfigValueType.Int, "support", ""),
                ("ai.chat.parentContextMaxTier", "2", ConfigValueType.Int, "ai", "Phụ huynh-trong-chat đọc tới tầng dữ liệu nào"),
                ("ai.hint.dailyLimitFreeTier", "3", ConfigValueType.Int, "ai", "Gói free"),
                ("ipHash.secretVersion", "1", ConfigValueType.Int, "security", "Con trỏ version; secret thật ở env var"),
                ("ipHash.rotationDays", "90", ConfigValueType.Int, "security", ""),
                ("referral.qualifyingOrderMinAmount", "99000", ConfigValueType.Int, "referral", "đồng"),
                ("referral.maxQualifiedPerReferrerPer30Days", "10", ConfigValueType.Int, "referral", ""),
            };

            var seed = new List<SystemConfig>();
            for (int i = 0; i < rows.Length; i++)
            {
                var (key, val, type, group, note) = rows[i];
                seed.Add(new SystemConfig
                {
                    ConfigId = i + 1,
                    ConfigKey = key,
                    ConfigValue = val,
                    ConfigType = type,
                    ConfigGroup = group,
                    Description = string.IsNullOrEmpty(note) ? null : note,
                    UpdatedAt = seededAt,
                    UpdatedBy = null
                });
            }
            mb.Entity<SystemConfig>().HasData(seed);
        }
        #endregion
    }
}
