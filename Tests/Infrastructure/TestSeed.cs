using ELearning_ToanHocHay_Control.Data;
using ELearning_ToanHocHay_Control.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ELearning_ToanHocHay_Control.Tests.Infrastructure;

/// <summary>Ids of the "golden dataset" used by the A1 / A2 integration tests.</summary>
public record SeededIds
{
    public int StudentAUserId { get; init; }
    public int StudentAId { get; init; }
    public int StudentBUserId { get; init; }
    public int StudentBId { get; init; }
    public int ParentLinkedUserId { get; init; }   // active link to Student A
    public int ParentUnlinkedUserId { get; init; }
    public int ContentEditorUserId { get; init; }
    public int FinanceUserId { get; init; }
    public int AdminUserId { get; init; }

    public int ExerciseId { get; init; }           // 4 graded questions (MC / TF / FillBlank / Essay)
    public int MaxAttemptsExerciseId { get; init; } // MaxAttempts = 1
    public int AttemptAId { get; init; }            // Student A's in-progress attempt on ExerciseId
    public int PackageId { get; init; }
    public decimal PackagePrice { get; init; }
    public int SubscriptionAId { get; init; }
    public int PaymentAId { get; init; }

    public int BankId { get; init; }
    public int McQuestionId { get; init; }
    public int McCorrectOptionId { get; init; }
    public int TfQuestionId { get; init; }
    public int TfTrueOptionId { get; init; }
    public int FillBlankQuestionId { get; init; }
    public int EssayQuestionId { get; init; }
}

public static class TestSeed
{
    private static readonly SemaphoreSlim Gate = new(1, 1);

    public static async Task<SeededIds> SeedAsync(AppDbContext db)
    {
        await Gate.WaitAsync();
        try
        {
            if (await db.Users.AsNoTracking().AnyAsync(u => u.Email == "admin@test.local"))
                return await ReadExistingAsync(db);

            return await SeedCoreAsync(db);
        }
        finally
        {
            Gate.Release();
        }
    }

    private static async Task<SeededIds> ReadExistingAsync(AppDbContext db)
    {
        int U(string email) => db.Users.AsNoTracking().Single(u => u.Email == email).UserId;

        var studentAUserId = U("student.a@test.local");
        var studentBUserId = U("student.b@test.local");
        var studentA = await db.Students.AsNoTracking().SingleAsync(s => s.UserId == studentAUserId);
        var studentB = await db.Students.AsNoTracking().SingleAsync(s => s.UserId == studentBUserId);
        var graded = await db.Exercises.AsNoTracking().SingleAsync(e => e.ExerciseName == "Graded quiz");
        var oneShot = await db.Exercises.AsNoTracking().SingleAsync(e => e.ExerciseName == "One-shot test");
        var attemptA = await db.ExerciseAttempts.AsNoTracking().FirstAsync(a => a.StudentId == studentA.StudentId);
        var package = await db.Packages.AsNoTracking().FirstAsync();
        var bank = await db.QuestionBanks.AsNoTracking().FirstAsync();
        var qs = await db.Questions.AsNoTracking().Where(q => q.BankId == bank.BankId).ToListAsync();
        var mc = qs.Single(q => q.QuestionType == QuestionType.MultipleChoice);
        var tf = qs.Single(q => q.QuestionType == QuestionType.TrueFalse);
        var fb = qs.Single(q => q.QuestionType == QuestionType.FillBlank);
        var essay = qs.Single(q => q.QuestionType == QuestionType.Essay);
        var mcCorrect = await db.QuestionOptions.AsNoTracking().FirstAsync(o => o.QuestionId == mc.QuestionId && o.IsCorrect);
        var tfTrue = await db.QuestionOptions.AsNoTracking().FirstAsync(o => o.QuestionId == tf.QuestionId && o.IsCorrect);
        var subscriptionA = await db.Subscriptions.AsNoTracking().FirstAsync(s => s.StudentId == studentA.StudentId);
        var paymentA = await db.Payments.AsNoTracking().FirstAsync(p => p.StudentId == studentA.StudentId);

        return new SeededIds
        {
            StudentAUserId = studentAUserId,
            StudentAId = studentA.StudentId,
            StudentBUserId = studentBUserId,
            StudentBId = studentB.StudentId,
            ParentLinkedUserId = U("parent.linked@test.local"),
            ParentUnlinkedUserId = U("parent.unlinked@test.local"),
            ContentEditorUserId = U("editor@test.local"),
            FinanceUserId = U("finance@test.local"),
            AdminUserId = U("admin@test.local"),
            ExerciseId = graded.ExerciseId,
            MaxAttemptsExerciseId = oneShot.ExerciseId,
            AttemptAId = attemptA.AttemptId,
            PackageId = package.PackageId,
            PackagePrice = package.Price,
            SubscriptionAId = subscriptionA.SubscriptionId,
            PaymentAId = paymentA.PaymentId,
            BankId = bank.BankId,
            McQuestionId = mc.QuestionId,
            McCorrectOptionId = mcCorrect.OptionId,
            TfQuestionId = tf.QuestionId,
            TfTrueOptionId = tfTrue.OptionId,
            FillBlankQuestionId = fb.QuestionId,
            EssayQuestionId = essay.QuestionId
        };
    }

    private static async Task<SeededIds> SeedCoreAsync(AppDbContext db)
    {
        User NewUser(string email, UserType type) => new()
        {
            Email = email,
            PasswordHash = "not-used-in-tests",
            FullName = email,
            UserType = type,
            IsEmailConfirmed = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var studentAUser = NewUser("student.a@test.local", UserType.Student);
        var studentBUser = NewUser("student.b@test.local", UserType.Student);
        var parentLinkedUser = NewUser("parent.linked@test.local", UserType.Parent);
        var parentUnlinkedUser = NewUser("parent.unlinked@test.local", UserType.Parent);
        var editorUser = NewUser("editor@test.local", UserType.ContentEditor);
        var financeUser = NewUser("finance@test.local", UserType.FinanceManager);
        var adminUser = NewUser("admin@test.local", UserType.SystemAdmin);

        db.Users.AddRange(studentAUser, studentBUser, parentLinkedUser, parentUnlinkedUser,
            editorUser, financeUser, adminUser);
        await db.SaveChangesAsync();

        var studentA = new Student { UserId = studentAUser.UserId };
        var studentB = new Student { UserId = studentBUser.UserId };
        var parentLinked = new Parent { UserId = parentLinkedUser.UserId, ConnectionCode = "LINK0001" };
        var parentUnlinked = new Parent { UserId = parentUnlinkedUser.UserId, ConnectionCode = "LINK0002" };
        db.Students.AddRange(studentA, studentB);
        db.Parents.AddRange(parentLinked, parentUnlinked);
        await db.SaveChangesAsync();

        db.ParentLinks.Add(new ParentLink
        {
            ParentId = parentLinked.ParentId,
            StudentId = studentA.StudentId,
            Status = LinkStatus.Active,
            Relationship = ParentRelationship.Father,
            IsPrimaryGuardian = true
        });

        // --- Question bank (Subject "MATH" + GradeLevel "G6" come from the InitialCreate seed) ---
        var subject = await db.Subjects.SingleAsync(s => s.Code == "MATH");
        var grade = await db.GradeLevels.SingleAsync(g => g.Code == "G6");

        var bank = new QuestionBank
        {
            BankName = "Grade 6 Math bank",
            SubjectId = subject.SubjectId,
            GradeLevelId = grade.GradeLevelId,
            CreatedBy = editorUser.UserId,
            IsActive = true
        };
        db.QuestionBanks.Add(bank);
        await db.SaveChangesAsync();

        Question NewQuestion(QuestionType type, string text, string? correct) => new()
        {
            BankId = bank.BankId,
            SubjectId = subject.SubjectId,
            QuestionText = text,
            QuestionType = type,
            DifficultyLevel = DifficultyLevel.Easy,
            CorrectAnswer = correct,
            Status = QuestionStatus.Approved,
            IsActive = true,
            CreatedBy = editorUser.UserId
        };

        var mc = NewQuestion(QuestionType.MultipleChoice, "2 + 2 = ?", null);
        var tf = NewQuestion(QuestionType.TrueFalse, "Is 3 an odd number?", "true");
        var fb = NewQuestion(QuestionType.FillBlank, "Half of 1 = ?", "1/2");
        var essay = NewQuestion(QuestionType.Essay, "Explain why 0 is even.", null);
        db.Questions.AddRange(mc, tf, fb, essay);
        await db.SaveChangesAsync();

        var mcA = new QuestionOption { QuestionId = mc.QuestionId, OptionText = "4", IsCorrect = true, OrderIndex = 1 };
        var mcB = new QuestionOption { QuestionId = mc.QuestionId, OptionText = "5", IsCorrect = false, OrderIndex = 2 };
        var tfTrue = new QuestionOption { QuestionId = tf.QuestionId, OptionText = "True", IsCorrect = true, OrderIndex = 1 };
        var tfFalse = new QuestionOption { QuestionId = tf.QuestionId, OptionText = "False", IsCorrect = false, OrderIndex = 2 };
        db.QuestionOptions.AddRange(mcA, mcB, tfTrue, tfFalse);
        await db.SaveChangesAsync();

        // --- Exercises ---
        var exercise = new Exercise
        {
            ExerciseName = "Graded quiz",
            ExerciseType = ExerciseType.Quiz,
            Status = ExerciseStatus.Published,
            IsActive = true,
            TotalScores = 4,
            PassingScore = 2,
            TotalQuestions = 4,
            CreatedBy = editorUser.UserId
        };
        var maxAttemptsExercise = new Exercise
        {
            ExerciseName = "One-shot test",
            ExerciseType = ExerciseType.Test,
            Status = ExerciseStatus.Published,
            IsActive = true,
            TotalScores = 1,
            MaxAttempts = 1,
            CreatedBy = editorUser.UserId
        };
        db.Exercises.AddRange(exercise, maxAttemptsExercise);
        await db.SaveChangesAsync();

        db.ExerciseQuestions.AddRange(
            new ExerciseQuestion { ExerciseId = exercise.ExerciseId, QuestionId = mc.QuestionId, Score = 1, OrderIndex = 1 },
            new ExerciseQuestion { ExerciseId = exercise.ExerciseId, QuestionId = tf.QuestionId, Score = 1, OrderIndex = 2 },
            new ExerciseQuestion { ExerciseId = exercise.ExerciseId, QuestionId = fb.QuestionId, Score = 1, OrderIndex = 3 },
            new ExerciseQuestion { ExerciseId = exercise.ExerciseId, QuestionId = essay.QuestionId, Score = 1, OrderIndex = 4 });

        var attemptA = new ExerciseAttempt
        {
            StudentId = studentA.StudentId,
            ExerciseId = exercise.ExerciseId,
            StartTime = DateTime.UtcNow,
            PlannedEndTime = DateTime.UtcNow.AddMinutes(30),
            MaxScore = 4,
            Status = AttemptStatus.InProgress
        };
        db.ExerciseAttempts.Add(attemptA);

        // --- Package / payment / subscription ---
        var package = new Package
        {
            UserId = adminUser.UserId,
            PackageName = "Standard",
            Tier = PackageTier.Standard,
            Price = 199000,
            DurationDays = 30
        };
        db.Packages.Add(package);
        await db.SaveChangesAsync();

        var paymentA = new Payment
        {
            PaidByUserId = studentAUser.UserId,
            StudentId = studentA.StudentId,
            Amount = 199000,
            Status = PaymentStatus.Pending,
            PaymentMethod = PaymentMethod.BankTransfer
        };
        db.Payments.Add(paymentA);
        await db.SaveChangesAsync();

        var subscriptionA = new Subscription
        {
            StudentId = studentA.StudentId,
            PackageId = package.PackageId,
            PaymentId = paymentA.PaymentId,
            Status = SubscriptionStatus.Pending,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30),
            AmountPaid = 199000
        };
        db.Subscriptions.Add(subscriptionA);
        await db.SaveChangesAsync();

        return new SeededIds
        {
            StudentAUserId = studentAUser.UserId,
            StudentAId = studentA.StudentId,
            StudentBUserId = studentBUser.UserId,
            StudentBId = studentB.StudentId,
            ParentLinkedUserId = parentLinkedUser.UserId,
            ParentUnlinkedUserId = parentUnlinkedUser.UserId,
            ContentEditorUserId = editorUser.UserId,
            FinanceUserId = financeUser.UserId,
            AdminUserId = adminUser.UserId,
            ExerciseId = exercise.ExerciseId,
            MaxAttemptsExerciseId = maxAttemptsExercise.ExerciseId,
            AttemptAId = attemptA.AttemptId,
            PackageId = package.PackageId,
            PackagePrice = package.Price,
            SubscriptionAId = subscriptionA.SubscriptionId,
            PaymentAId = paymentA.PaymentId,
            BankId = bank.BankId,
            McQuestionId = mc.QuestionId,
            McCorrectOptionId = mcA.OptionId,
            TfQuestionId = tf.QuestionId,
            TfTrueOptionId = tfTrue.OptionId,
            FillBlankQuestionId = fb.QuestionId,
            EssayQuestionId = essay.QuestionId
        };
    }
}
