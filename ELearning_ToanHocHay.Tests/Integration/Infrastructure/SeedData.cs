using ELearning_ToanHocHay_Control.Data;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Services.Implementations;
using Microsoft.EntityFrameworkCore;

namespace ELearning_ToanHocHay.Tests.Integration.Infrastructure;

public enum TestRole
{
    StudentA, StudentB, ParentLinked, ParentStranger,
    Editor, Reviewer, Finance, Support, Admin,
}

public sealed record SeededIds(
    int StudentAUserId, int StudentAId,
    int StudentBUserId, int StudentBId,
    int ParentLinkedUserId, int ParentStrangerUserId,
    int EditorUserId, int ReviewerUserId, int FinanceUserId, int SupportUserId, int AdminUserId,
    int BankId,
    int McQuestionId, int McCorrectOptionId,
    int TfQuestionId, int TfTrueOptionId,
    int FillBlankQuestionId, int EssayQuestionId,
    int QuizExerciseId, int OneShotExerciseId,
    int InProgressAttemptId,
    int PackageId, int PendingPaymentId, int CompletedPaymentId, int PendingSubscriptionId)
{
    public int UserId(TestRole role) => role switch
    {
        TestRole.StudentA => StudentAUserId,
        TestRole.StudentB => StudentBUserId,
        TestRole.ParentLinked => ParentLinkedUserId,
        TestRole.ParentStranger => ParentStrangerUserId,
        TestRole.Editor => EditorUserId,
        TestRole.Reviewer => ReviewerUserId,
        TestRole.Finance => FinanceUserId,
        TestRole.Support => SupportUserId,
        TestRole.Admin => AdminUserId,
        _ => throw new ArgumentOutOfRangeException(nameof(role)),
    };
}

/// <summary>Golden dataset — tối thiểu, ổn định, idempotent. Test tự dựng phần còn lại qua <see cref="FlowSeed"/>.</summary>
public static class SeedData
{
    public const string Password = "Test!234";

    public static async Task<SeededIds> EnsureAsync(AppDbContext db)
    {
        var existing = await db.Users.FirstOrDefaultAsync(u => u.Email == "admin@it.test");
        if (existing != null) return await LoadAsync(db);

        var hasher = new PasswordHasher();
        var hash = hasher.HashPassword(Password);

        User NewUser(string email, UserType type) => new()
        {
            Email = email,
            PasswordHash = hash,
            FullName = email.Split('@')[0],
            UserType = type,
            IsEmailConfirmed = true,
            EmailConfirmedAt = DateTime.UtcNow,
            IsActive = true,
        };

        var studentAU = NewUser("student.a@it.test", UserType.Student);
        var studentBU = NewUser("student.b@it.test", UserType.Student);
        var parentLinkedU = NewUser("parent.linked@it.test", UserType.Parent);
        var parentStrangerU = NewUser("parent.stranger@it.test", UserType.Parent);
        var editorU = NewUser("editor@it.test", UserType.ContentEditor);
        var reviewerU = NewUser("reviewer@it.test", UserType.AcademicReviewer);
        var financeU = NewUser("finance@it.test", UserType.FinanceManager);
        var supportU = NewUser("support@it.test", UserType.SupportStaff);
        var adminU = NewUser("admin@it.test", UserType.SystemAdmin);
        db.Users.AddRange(studentAU, studentBU, parentLinkedU, parentStrangerU,
            editorU, reviewerU, financeU, supportU, adminU);
        await db.SaveChangesAsync();

        var studentA = new Student { UserId = studentAU.UserId, CurrentGradeLevelId = 1 };
        var studentB = new Student { UserId = studentBU.UserId, CurrentGradeLevelId = 1 };
        db.Students.AddRange(studentA, studentB);

        var parentLinked = new Parent { UserId = parentLinkedU.UserId, ConnectionCode = "LINKAAAA" };
        var parentStranger = new Parent { UserId = parentStrangerU.UserId, ConnectionCode = "LINKBBBB" };
        db.Parents.AddRange(parentLinked, parentStranger);
        await db.SaveChangesAsync();

        db.ParentLinks.Add(new ParentLink
        {
            ParentId = parentLinked.ParentId,
            StudentId = studentA.StudentId,
            Status = LinkStatus.Active,
            Relationship = ParentRelationship.Father,
            IsPrimaryGuardian = true,
        });

        var bank = new QuestionBank { BankName = "IT Bank", SubjectId = 1, GradeLevelId = 1, IsActive = true, CreatedBy = editorU.UserId };
        db.QuestionBanks.Add(bank);
        await db.SaveChangesAsync();

        Question NewQ(QuestionType type, string text, string? correct) => new()
        {
            BankId = bank.BankId,
            SubjectId = 1,
            QuestionText = text,
            QuestionType = type,
            DifficultyLevel = DifficultyLevel.Easy,
            CorrectAnswer = correct,
            Status = QuestionStatus.Approved,
            IsActive = true,
            CreatedBy = editorU.UserId,
        };

        var mc = NewQ(QuestionType.MultipleChoice, "2 + 2 = ?", null);
        var tf = NewQ(QuestionType.TrueFalse, "3 là số lẻ?", "true");
        var fb = NewQ(QuestionType.FillBlank, "1/2 = ?", "1/2");
        var essay = NewQ(QuestionType.Essay, "Vì sao 0 là số chẵn?", null);
        db.Questions.AddRange(mc, tf, fb, essay);
        await db.SaveChangesAsync();

        var mcWrong = new QuestionOption { QuestionId = mc.QuestionId, OptionText = "3", IsCorrect = false, OrderIndex = 1 };
        var mcRight = new QuestionOption { QuestionId = mc.QuestionId, OptionText = "4", IsCorrect = true, OrderIndex = 2 };
        var tfTrue = new QuestionOption { QuestionId = tf.QuestionId, OptionText = "Đúng", IsCorrect = true, OrderIndex = 1 };
        var tfFalse = new QuestionOption { QuestionId = tf.QuestionId, OptionText = "Sai", IsCorrect = false, OrderIndex = 2 };
        db.QuestionOptions.AddRange(mcWrong, mcRight, tfTrue, tfFalse);
        await db.SaveChangesAsync();

        var quiz = new Exercise
        {
            ExerciseName = "IT Quiz", ExerciseType = ExerciseType.Quiz, TotalQuestions = 4,
            TotalScores = 4, PassingScore = 2, Status = ExerciseStatus.Published, IsActive = true,
            CreatedBy = editorU.UserId,
        };
        var oneShot = new Exercise
        {
            ExerciseName = "IT One-shot", ExerciseType = ExerciseType.Test, TotalQuestions = 1,
            TotalScores = 1, PassingScore = 1, MaxAttempts = 1, Status = ExerciseStatus.Published,
            IsActive = true, CreatedBy = editorU.UserId,
        };
        db.Exercises.AddRange(quiz, oneShot);
        await db.SaveChangesAsync();

        db.ExerciseQuestions.AddRange(
            new ExerciseQuestion { ExerciseId = quiz.ExerciseId, QuestionId = mc.QuestionId, Score = 1, OrderIndex = 1 },
            new ExerciseQuestion { ExerciseId = quiz.ExerciseId, QuestionId = tf.QuestionId, Score = 1, OrderIndex = 2 },
            new ExerciseQuestion { ExerciseId = quiz.ExerciseId, QuestionId = fb.QuestionId, Score = 1, OrderIndex = 3 },
            new ExerciseQuestion { ExerciseId = quiz.ExerciseId, QuestionId = essay.QuestionId, Score = 1, OrderIndex = 4 },
            new ExerciseQuestion { ExerciseId = oneShot.ExerciseId, QuestionId = mc.QuestionId, Score = 1, OrderIndex = 1 });

        var inProgress = new ExerciseAttempt
        {
            StudentId = studentA.StudentId, ExerciseId = quiz.ExerciseId,
            Status = AttemptStatus.InProgress, MaxScore = 4,
            StartTime = DateTime.UtcNow, PlannedEndTime = DateTime.UtcNow.AddMinutes(30),
        };
        db.ExerciseAttempts.Add(inProgress);

        var package = new Package
        {
            UserId = adminU.UserId, PackageName = "IT Standard", Tier = PackageTier.Standard,
            Price = 199_000m, DurationDays = 30, IsActive = true,
        };
        db.Packages.Add(package);
        await db.SaveChangesAsync();

        db.PackageEntitlements.Add(new PackageEntitlement
        {
            PackageId = package.PackageId, ScopeType = EntitlementScope.SubjectGrade, SubjectId = 1, GradeLevelId = 1,
        });

        var pendingPayment = new Payment
        {
            PaidByUserId = studentAU.UserId, StudentId = studentA.StudentId, Amount = 199_000m,
            PaymentMethod = PaymentMethod.BankTransfer, Status = PaymentStatus.Pending, PaymentDate = DateTime.UtcNow,
        };
        var completedPayment = new Payment
        {
            PaidByUserId = studentAU.UserId, StudentId = studentA.StudentId, Amount = 199_000m,
            PaymentMethod = PaymentMethod.BankTransfer, Status = PaymentStatus.Completed,
            TransactionId = "SEED-DONE-A", PaymentDate = DateTime.UtcNow.AddDays(-3),
        };
        db.Payments.AddRange(pendingPayment, completedPayment);
        await db.SaveChangesAsync();

        var pendingSub = new Subscription
        {
            StudentId = studentA.StudentId, PackageId = package.PackageId, PaymentId = pendingPayment.PaymentId,
            Status = SubscriptionStatus.Pending, AmountPaid = 199_000m,
            StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(30), CreatedAt = DateTime.UtcNow,
        };
        db.Subscriptions.Add(pendingSub);
        await db.SaveChangesAsync();

        return await LoadAsync(db);
    }

    private static async Task<SeededIds> LoadAsync(AppDbContext db)
    {
        var users = await db.Users.Where(u => u.Email.EndsWith("@it.test"))
            .ToDictionaryAsync(u => u.Email, u => u.UserId);
        var students = await db.Students.ToDictionaryAsync(s => s.UserId, s => s.StudentId);
        int U(string email) => users[email];
        int Sid(string email) => students[users[email]];

        var bank = await db.QuestionBanks.SingleAsync(b => b.BankName == "IT Bank");
        var mc = await db.Questions.SingleAsync(q => q.BankId == bank.BankId && q.QuestionType == QuestionType.MultipleChoice);
        var tf = await db.Questions.SingleAsync(q => q.BankId == bank.BankId && q.QuestionType == QuestionType.TrueFalse);
        var fb = await db.Questions.SingleAsync(q => q.BankId == bank.BankId && q.QuestionType == QuestionType.FillBlank);
        var essay = await db.Questions.SingleAsync(q => q.BankId == bank.BankId && q.QuestionType == QuestionType.Essay);
        var mcRight = await db.QuestionOptions.SingleAsync(o => o.QuestionId == mc.QuestionId && o.IsCorrect);
        var tfTrue = await db.QuestionOptions.SingleAsync(o => o.QuestionId == tf.QuestionId && o.IsCorrect);
        var quiz = await db.Exercises.SingleAsync(e => e.ExerciseName == "IT Quiz");
        var oneShot = await db.Exercises.SingleAsync(e => e.ExerciseName == "IT One-shot");
        var attempt = await db.ExerciseAttempts.FirstAsync(a => a.ExerciseId == quiz.ExerciseId && a.Status == AttemptStatus.InProgress);
        var package = await db.Packages.SingleAsync(p => p.PackageName == "IT Standard");
        var pendingPayment = await db.Payments.FirstAsync(p => p.Status == PaymentStatus.Pending && p.TransactionId == null);
        var completedPayment = await db.Payments.SingleAsync(p => p.TransactionId == "SEED-DONE-A");
        var pendingSub = await db.Subscriptions.FirstAsync(s => s.PaymentId == pendingPayment.PaymentId);

        return new SeededIds(
            U("student.a@it.test"), Sid("student.a@it.test"),
            U("student.b@it.test"), Sid("student.b@it.test"),
            U("parent.linked@it.test"), U("parent.stranger@it.test"),
            U("editor@it.test"), U("reviewer@it.test"), U("finance@it.test"), U("support@it.test"), U("admin@it.test"),
            bank.BankId,
            mc.QuestionId, mcRight.OptionId,
            tf.QuestionId, tfTrue.OptionId,
            fb.QuestionId, essay.QuestionId,
            quiz.ExerciseId, oneShot.ExerciseId,
            attempt.AttemptId,
            package.PackageId, pendingPayment.PaymentId, completedPayment.PaymentId, pendingSub.SubscriptionId);
    }
}
