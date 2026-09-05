using System.Threading;
using ELearning_ToanHocHay_Control.Data.Entities;

namespace ELearning_ToanHocHay.Tests.Unit.Infrastructure;

/// <summary>
/// Object mother — dựng entity với <b>chỉ field bắt buộc</b>, phần còn lại nhận giá trị
/// hợp lệ tối thiểu. Mọi factory nhận <c>Action&lt;T&gt;? tweak</c> để test tinh chỉnh.
/// Không tự gắn vào <see cref="ELearning_ToanHocHay_Control.Data.AppDbContext"/> — test tự
/// <c>Add</c>/<c>SaveChanges</c> khi cần (U2).
/// </summary>
public static class Entities
{
    private static int _seq;
    private static int Next() => Interlocked.Increment(ref _seq);

    public static User NewUser(
        string? email = null,
        UserType type = UserType.Student,
        bool confirmed = true,
        Action<User>? tweak = null)
    {
        var n = Next();
        var u = new User
        {
            Email = email ?? $"user{n}@test.local",
            PasswordHash = "$2a$11$0123456789012345678901uVeryFakeBcryptHashValue.abcdefghi",
            FullName = $"Người Dùng {n}",
            UserType = type,
            IsActive = true,
            IsEmailConfirmed = confirmed,
            EmailConfirmedAt = confirmed ? DateTime.UtcNow : null,
            SecurityStamp = Guid.NewGuid().ToString("N"),
            CreatedAt = DateTime.UtcNow,
        };
        tweak?.Invoke(u);
        return u;
    }

    public static Student NewStudent(int userId, int? gradeId = 1, Action<Student>? tweak = null)
    {
        var s = new Student
        {
            UserId = userId,
            CurrentGradeLevelId = gradeId,
            AiDataSharingLevel = AiDataSharingLevel.SummaryOnly,
        };
        tweak?.Invoke(s);
        return s;
    }

    public static Parent NewParent(int userId, string? code = null, Action<Parent>? tweak = null)
    {
        var p = new Parent
        {
            UserId = userId,
            ConnectionCode = code ?? Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
        };
        tweak?.Invoke(p);
        return p;
    }

    public static Payment NewPayment(
        int payerUserId,
        decimal amount,
        PaymentStatus status = PaymentStatus.Completed,
        Action<Payment>? tweak = null)
    {
        var p = new Payment
        {
            PaidByUserId = payerUserId,
            Amount = amount,
            Status = status,
            PaymentMethod = PaymentMethod.BankTransfer,
            PaymentDate = DateTime.UtcNow,
        };
        tweak?.Invoke(p);
        return p;
    }

    public static Subscription NewSubscription(
        int packageId,
        SubscriptionStatus status = SubscriptionStatus.Active,
        int? studentId = null,
        int? paymentId = null,
        DateTime? endDate = null,
        Action<Subscription>? tweak = null)
    {
        var now = DateTime.UtcNow;
        var s = new Subscription
        {
            PackageId = packageId,
            StudentId = studentId,
            PaymentId = paymentId,
            Status = status,
            StartDate = now.AddDays(-1),
            EndDate = endDate ?? now.AddDays(30),
            CreatedAt = now,
            AmountPaid = 0m,
        };
        tweak?.Invoke(s);
        return s;
    }

    public static Package NewPackage(
        int userId,
        PackageTier tier = PackageTier.Standard,
        Action<Package>? tweak = null)
    {
        var n = Next();
        var p = new Package
        {
            UserId = userId,
            PackageName = $"Gói {tier} {n}",
            Tier = tier,
            Price = 199000m,
            DurationDays = 30,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };
        tweak?.Invoke(p);
        return p;
    }

    public static Course NewCourse(
        CourseStatus status = CourseStatus.Published,
        int subjectId = 1,
        int gradeId = 1,
        Action<Course>? tweak = null)
    {
        var n = Next();
        var c = new Course
        {
            SubjectId = subjectId,
            GradeLevelId = gradeId,
            FrameworkId = null,
            Title = $"Khoá học {n}",
            Slug = $"khoa-hoc-{n}-{Guid.NewGuid():N}"[..40],
            Status = status,
            ListPrice = 0m,
            IsPurchasable = true,
            DisplayOrder = n,
            CreatedBy = 1,
            CreatedAt = DateTime.UtcNow,
        };
        tweak?.Invoke(c);
        return c;
    }

    public static Question NewQuestion(
        QuestionType type,
        string? correct = null,
        int bankId = 1,
        int subjectId = 1,
        Action<Question>? tweak = null)
    {
        var n = Next();
        var q = new Question
        {
            BankId = bankId,
            SubjectId = subjectId,
            QuestionText = $"Câu hỏi {n}?",
            QuestionType = type,
            DifficultyLevel = DifficultyLevel.Easy,
            CorrectAnswer = correct,
            Status = QuestionStatus.Approved,
            IsActive = true,
            CreatedBy = 1,
            CreatedAt = DateTime.UtcNow,
        };
        tweak?.Invoke(q);
        return q;
    }

    public static RefundRequest NewRefundRequest(
        int paymentId,
        decimal amount,
        RefundRequestStatus status = RefundRequestStatus.PendingReview,
        Action<RefundRequest>? tweak = null)
    {
        var r = new RefundRequest
        {
            PublicId = Guid.NewGuid(),
            PaymentId = paymentId,
            Amount = amount,
            Status = status,
            ReasonCode = RefundReasonCode.CustomerRequest,
            RequestedByUserId = 1,
            BeneficiaryUserId = 1,
            BankBin = "970418",
            BankAccountNumberProtected = "enc:0071000123456",
            BankAccountNumberLast4 = "3456",
            BankAccountHolderName = "Nguyen Van A",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        tweak?.Invoke(r);
        return r;
    }
}
