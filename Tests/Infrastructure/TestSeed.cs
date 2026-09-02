using ELearning_ToanHocHay_Control.Data;
using ELearning_ToanHocHay_Control.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ELearning_ToanHocHay_Control.Tests.Infrastructure;

/// <summary>Ids of the minimal "golden dataset" for the A1 authorization-matrix tests.</summary>
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

    public int ExerciseId { get; init; }
    public int AttemptAId { get; init; }           // Student A's attempt
    public int PackageId { get; init; }
    public int SubscriptionAId { get; init; }      // Student A's subscription
    public int PaymentAId { get; init; }
}

public static class TestSeed
{
    public static async Task<SeededIds> SeedAsync(AppDbContext db)
    {
        // Data already present (re-run) -> return the existing ids without re-seeding.
        if (await db.Users.AnyAsync(u => u.Email == "student.a@test.local"))
            return await ReadExistingAsync(db);

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

        var package = new Package
        {
            UserId = adminUser.UserId,
            PackageName = "Gói tiêu chuẩn",
            Tier = PackageTier.Standard,
            Price = 199000,
            DurationDays = 30
        };
        var exercise = new Exercise
        {
            ExerciseName = "Bài kiểm tra thử",
            ExerciseType = ExerciseType.Quiz,
            Status = ExerciseStatus.Published,
            IsActive = true,
            CreatedBy = editorUser.UserId
        };
        db.Packages.Add(package);
        db.Exercises.Add(exercise);
        await db.SaveChangesAsync();

        var attemptA = new ExerciseAttempt
        {
            StudentId = studentA.StudentId,
            ExerciseId = exercise.ExerciseId,
            StartTime = DateTime.UtcNow,
            PlannedEndTime = DateTime.UtcNow.AddMinutes(30),
            Status = AttemptStatus.InProgress
        };
        db.ExerciseAttempts.Add(attemptA);

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
            AttemptAId = attemptA.AttemptId,
            PackageId = package.PackageId,
            SubscriptionAId = subscriptionA.SubscriptionId,
            PaymentAId = paymentA.PaymentId
        };
    }

    private static async Task<SeededIds> ReadExistingAsync(AppDbContext db)
    {
        int UserId(string email) => db.Users.Single(u => u.Email == email).UserId;

        var studentAUserId = UserId("student.a@test.local");
        var studentBUserId = UserId("student.b@test.local");
        var studentA = await db.Students.SingleAsync(s => s.UserId == studentAUserId);
        var studentB = await db.Students.SingleAsync(s => s.UserId == studentBUserId);
        var exercise = await db.Exercises.FirstAsync();
        var attemptA = await db.ExerciseAttempts.FirstAsync(a => a.StudentId == studentA.StudentId);
        var package = await db.Packages.FirstAsync();
        var subscriptionA = await db.Subscriptions.FirstAsync(s => s.StudentId == studentA.StudentId);
        var paymentA = await db.Payments.FirstAsync(p => p.StudentId == studentA.StudentId);

        return new SeededIds
        {
            StudentAUserId = studentAUserId,
            StudentAId = studentA.StudentId,
            StudentBUserId = studentBUserId,
            StudentBId = studentB.StudentId,
            ParentLinkedUserId = UserId("parent.linked@test.local"),
            ParentUnlinkedUserId = UserId("parent.unlinked@test.local"),
            ContentEditorUserId = UserId("editor@test.local"),
            FinanceUserId = UserId("finance@test.local"),
            AdminUserId = UserId("admin@test.local"),
            ExerciseId = exercise.ExerciseId,
            AttemptAId = attemptA.AttemptId,
            PackageId = package.PackageId,
            SubscriptionAId = subscriptionA.SubscriptionId,
            PaymentAId = paymentA.PaymentId
        };
    }
}
