using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using ELearning_ToanHocHay_Control.Services.Implementations;
using ELearning_ToanHocHay.Tests.Unit.Infrastructure;
using FluentAssertions;
using NSubstitute;

namespace ELearning_ToanHocHay.Tests.Unit.Access;

/// <summary>§3.32 — UT-RES-*. Repo fake hoàn toàn (NSubstitute).</summary>
[Trait("Level", "Unit")]
[Trait("Tier", "U1")]
public class ResourceAccessServiceTests
{
    private readonly IStudentRepository _students = Substitute.For<IStudentRepository>();
    private readonly IParentRepository _parents = Substitute.For<IParentRepository>();
    private readonly IParentLinkRepository _links = Substitute.For<IParentLinkRepository>();
    private readonly IExerciseAttemptRepository _attempts = Substitute.For<IExerciseAttemptRepository>();
    private readonly ISubscriptionRepository _subs = Substitute.For<ISubscriptionRepository>();
    private readonly IPaymentRepository _payments = Substitute.For<IPaymentRepository>();

    private ResourceAccessService Svc() => new(_students, _parents, _links, _attempts, _subs, _payments);

    // ---- CanAccessStudentAsync ----

    [Fact] // UT-RES-01
    public async Task Admin_gets_access_without_touching_repos()
    {
        (await Svc().CanAccessStudentAsync(5, 10, UserType.SystemAdmin)).Should().BeTrue();
        await _students.DidNotReceive().GetByIdAsync(Arg.Any<int>());
    }

    [Fact] // UT-RES-02
    public async Task Missing_student_is_denied()
    {
        _students.GetByIdAsync(5).Returns((Student?)null);
        (await Svc().CanAccessStudentAsync(5, 10, UserType.Student)).Should().BeFalse();
    }

    [Fact] // UT-RES-03
    public async Task The_student_themselves_has_access()
    {
        _students.GetByIdAsync(5).Returns(new Student { StudentId = 5, UserId = 10 });
        (await Svc().CanAccessStudentAsync(5, 10, UserType.Student)).Should().BeTrue();
    }

    [Fact] // UT-RES-04
    public async Task Parent_with_active_link_has_access()
    {
        _students.GetByIdAsync(5).Returns(new Student { StudentId = 5, UserId = 99 });
        _parents.GetByUserIdAsync(10).Returns(new Parent { ParentId = 7, UserId = 10 });
        _links.ExistsActiveAsync(5, 7).Returns(true);

        (await Svc().CanAccessStudentAsync(5, 10, UserType.Parent)).Should().BeTrue();
    }

    [Fact] // UT-RES-05 / 06 — link không Active, hoặc không có link
    public async Task Parent_without_active_link_is_denied()
    {
        _students.GetByIdAsync(5).Returns(new Student { StudentId = 5, UserId = 99 });
        _parents.GetByUserIdAsync(10).Returns(new Parent { ParentId = 7, UserId = 10 });
        _links.ExistsActiveAsync(5, 7).Returns(false);

        (await Svc().CanAccessStudentAsync(5, 10, UserType.Parent)).Should().BeFalse();
    }

    [Fact] // UT-RES-07
    public async Task Another_student_is_denied()
    {
        _students.GetByIdAsync(5).Returns(new Student { StudentId = 5, UserId = 99 });
        _parents.GetByUserIdAsync(10).Returns((Parent?)null);

        (await Svc().CanAccessStudentAsync(5, 10, UserType.Student)).Should().BeFalse();
    }

    // ---- CanAccessPaymentAsync ----

    [Fact] // UT-RES-08
    public async Task Payer_can_access_payment()
    {
        _payments.GetByIdAsync(1).Returns(new Payment { PaymentId = 1, PaidByUserId = 10 });
        (await Svc().CanAccessPaymentAsync(Claims.For(UserType.Student, userId: 10), 1)).Should().BeTrue();
    }

    [Fact] // UT-RES-09
    public async Task Beneficiary_student_can_access_payment()
    {
        _payments.GetByIdAsync(1).Returns(new Payment { PaymentId = 1, PaidByUserId = 99, StudentId = 3 });
        _students.GetByIdAsync(3).Returns(new Student { StudentId = 3, UserId = 10 });

        (await Svc().CanAccessPaymentAsync(Claims.For(UserType.Student, userId: 10, studentId: 3), 1)).Should().BeTrue();
    }

    [Fact] // UT-RES-10
    public async Task Stranger_cannot_access_payment()
    {
        _payments.GetByIdAsync(1).Returns(new Payment { PaymentId = 1, PaidByUserId = 99, StudentId = null });
        (await Svc().CanAccessPaymentAsync(Claims.For(UserType.Student, userId: 10), 1)).Should().BeFalse();
    }

    // ---- CanViewAttemptAsync ----

    [Theory] // UT-RES-11
    [InlineData(10, false, true)]   // chủ attempt
    [InlineData(99, true, true)]    // phụ huynh có link
    [InlineData(99, false, false)]  // người lạ
    public async Task View_attempt_owner_parent_stranger(int studentUserId, bool parentLinked, bool expected)
    {
        _attempts.GetAttemptByIdAsync(1).Returns(new ExerciseAttempt { AttemptId = 1, StudentId = 3 });
        _students.GetByIdAsync(3).Returns(new Student { StudentId = 3, UserId = studentUserId });
        _parents.GetByUserIdAsync(10).Returns(parentLinked ? new Parent { ParentId = 7, UserId = 10 } : null);
        _links.ExistsActiveAsync(3, 7).Returns(parentLinked);

        (await Svc().CanViewAttemptAsync(Claims.For(UserType.Parent, userId: 10), 1)).Should().Be(expected);
    }

    // ---- CanAccessSubscriptionAsync ----

    [Fact] // UT-RES-12a — Finance
    public async Task Finance_can_access_any_subscription()
        => (await Svc().CanAccessSubscriptionAsync(Claims.For(UserType.FinanceManager, userId: 10), 1)).Should().BeTrue();

    [Fact] // UT-RES-12b — chủ
    public async Task Owner_can_access_subscription()
    {
        _subs.GetByIdAsync(1).Returns(new Subscription { SubscriptionId = 1, StudentId = 3 });
        _students.GetByIdAsync(3).Returns(new Student { StudentId = 3, UserId = 10 });

        (await Svc().CanAccessSubscriptionAsync(Claims.For(UserType.Student, userId: 10, studentId: 3), 1)).Should().BeTrue();
    }

    [Fact] // UT-RES-12c — người lạ
    public async Task Stranger_cannot_access_subscription()
    {
        _subs.GetByIdAsync(1).Returns(new Subscription { SubscriptionId = 1, StudentId = 3 });
        _students.GetByIdAsync(3).Returns(new Student { StudentId = 3, UserId = 99 });
        _parents.GetByUserIdAsync(10).Returns((Parent?)null);

        (await Svc().CanAccessSubscriptionAsync(Claims.For(UserType.Student, userId: 10), 1)).Should().BeFalse();
    }
}
