using System.Security.Claims;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs.Refund;
using ELearning_ToanHocHay_Control.Services.Helpers;
using ELearning_ToanHocHay_Control.Services.Implementations;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using ELearning_ToanHocHay.Tests.Unit.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace ELearning_ToanHocHay.Tests.Unit.Refund;

/// <summary>§3.22 — UT-RFP-*. SQLite (RefundRequests + Payments) + config/access/event fake.</summary>
[Trait("Level", "Unit")]
[Trait("Tier", "U2")]
public class RefundServicePolicyTests : IDisposable
{
    private readonly SqliteDb _sql = SqliteDb.New();
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 9, 6, 12, 0, 0, TimeSpan.Zero));
    private readonly FakeSystemConfig _config = new();
    private readonly IResourceAccessService _access = Substitute.For<IResourceAccessService>();

    public RefundServicePolicyTests()
        => _access.CanAccessPaymentAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<int>()).Returns(true);

    public void Dispose() => _sql.Dispose();

    private RefundService Svc() => new(
        _sql.NewContext(), _config, _access, new FakeRefundFieldProtector(),
        Substitute.For<IRefundEventWriter>(), NullLogger<RefundService>.Instance, _clock);

    private DateTime Now => _clock.GetUtcNow().UtcDateTime;

    private Payment AddPayment(decimal amount = 199_000m, PaymentStatus status = PaymentStatus.Completed,
        decimal? refunded = null, DateTime? paidAt = null)
    {
        var payment = Seed.Payment(_sql.Db, amount, status, p =>
        {
            p.RefundAmount = refunded;
            p.PaymentDate = paidAt ?? Now.AddDays(-1);
        });
        return payment;
    }

    private RefundRequest AddRequest(int paymentId, decimal amount, RefundRequestStatus status,
        int beneficiaryUserId = 1, DateTime? createdAt = null, DateTime? approvedAt = null)
    {
        var r = Entities.NewRefundRequest(paymentId, amount, status, tweak: x =>
        {
            x.BeneficiaryUserId = beneficiaryUserId;
            x.CreatedAt = createdAt ?? Now;
            x.ApprovedAt = approvedAt;
        });
        _sql.Db.RefundRequests.Add(r);
        _sql.Db.SaveChanges();
        return r;
    }

    private static CreateRefundRequestDto CreateDto(int paymentId, decimal? amount = null) => new()
    {
        PaymentId = paymentId,
        Amount = amount,
        ReasonCode = RefundReasonCode.CustomerRequest,
        BankBin = "970418",
        BankAccountNumber = "0071000123456",
        BankAccountHolderName = "Nguyen Van A",
    };

    private static ClaimsPrincipal Finance(int userId = 10) => Claims.For(UserType.FinanceManager, userId: userId);
    private static ClaimsPrincipal Admin(int userId = 11) => Claims.For(UserType.SystemAdmin, userId: userId);
    private static ClaimsPrincipal Customer(int userId = 20) => Claims.For(UserType.Parent, userId: userId);

    private RefundRequestStatus StatusOf(int id)
        => _sql.NewContext().RefundRequests.Single(r => r.RefundRequestId == id).Status;

    // ---------------------------------------------------------------- CreateAsync

    [Fact] // UT-RFP-12
    public async Task Create_rejects_non_completed_payment()
    {
        var p = AddPayment(status: PaymentStatus.Pending);
        var res = await Svc().CreateAsync(CreateDto(p.PaymentId), Customer());

        res.Success.Should().BeFalse();
        res.Message.Should().Contain("Completed");
    }

    [Fact] // UT-RFP-13
    public async Task Create_rejects_amount_over_remaining()
    {
        var p = AddPayment(amount: 199_000m, refunded: 150_000m, status: PaymentStatus.PartiallyRefunded);
        var res = await Svc().CreateAsync(CreateDto(p.PaymentId, amount: 100_000m), Customer());

        res.Success.Should().BeFalse();
        res.StatusCode.Should().Be(400);
    }

    [Fact] // UT-RFP-14
    public async Task Create_rejects_payment_older_than_max_age()
    {
        _config.Set("refund.maxPaymentAgeDays", "180");
        var p = AddPayment(paidAt: Now.AddDays(-200));

        var res = await Svc().CreateAsync(CreateDto(p.PaymentId), Customer());

        res.Success.Should().BeFalse();
        res.Message.Should().Contain("180");
    }

    [Fact] // UT-RFP-15
    public async Task Create_rejects_when_an_open_request_already_exists()
    {
        var p = AddPayment();
        AddRequest(p.PaymentId, 199_000m, RefundRequestStatus.PendingReview);

        var res = await Svc().CreateAsync(CreateDto(p.PaymentId), Customer());

        res.StatusCode.Should().Be(409);
    }

    [Fact] // UT-RFP-05
    public async Task Create_rejects_when_beneficiary_hit_the_30d_limit()
    {
        _config.Set("refund.maxRequestsPerUserPer30d", "3");
        var p = AddPayment();
        var beneficiary = p.PaidByUserId;
        for (var i = 0; i < 3; i++)
        {
            var other = AddPayment();
            AddRequest(other.PaymentId, 10_000m, RefundRequestStatus.Completed, beneficiaryUserId: beneficiary,
                createdAt: Now.AddDays(-i));
        }

        var res = await Svc().CreateAsync(CreateDto(p.PaymentId), Customer());

        res.StatusCode.Should().Be(409);
    }

    [Fact] // UT-RFP-06
    public async Task Create_by_admin_ignores_the_per_user_limit()
    {
        _config.Set("refund.maxRequestsPerUserPer30d", "1");
        var p = AddPayment();
        AddRequest(AddPayment().PaymentId, 10_000m, RefundRequestStatus.Completed, beneficiaryUserId: p.PaidByUserId);

        var res = await Svc().CreateAsync(CreateDto(p.PaymentId), Admin());

        res.Success.Should().BeTrue();
    }

    [Fact] // UT-RFP-03 — chỉ request đã duyệt trong ngày mới tính vào "đã dùng"
    public async Task Create_succeeds_when_prior_requests_are_pending_or_from_yesterday()
    {
        _config.Set("refund.dailyCapVnd", "250000").Set("refund.dualControlThresholdVnd", "0");
        var p = AddPayment(amount: 199_000m);
        // pending hôm nay (không tính) + approved hôm qua (ngoài mốc ngày)
        AddRequest(AddPayment().PaymentId, 199_000m, RefundRequestStatus.PendingReview);
        AddRequest(AddPayment().PaymentId, 199_000m, RefundRequestStatus.Approved, approvedAt: Now.AddDays(-1));

        var created = await Svc().CreateAsync(CreateDto(p.PaymentId), Customer());
        created.Success.Should().BeTrue();

        var approve = await Svc().ApproveAsync(created.Data.RefundRequestId, new ApproveRefundDto(), Finance());
        approve.Success.Should().BeTrue();
    }

    // ---------------------------------------------------------------- ApproveAsync

    private int CreatePending(decimal amount = 199_000m)
    {
        var p = AddPayment(amount: amount);
        var res = Svc().CreateAsync(CreateDto(p.PaymentId, amount), Customer()).GetAwaiter().GetResult();
        res.Success.Should().BeTrue();
        return res.Data.RefundRequestId;
    }

    [Fact] // UT-RFP-01
    public async Task Approve_within_daily_cap()
    {
        _config.Set("refund.dailyCapVnd", "20000000").Set("refund.dualControlThresholdVnd", "0");
        var id = CreatePending();

        var res = await Svc().ApproveAsync(id, new ApproveRefundDto(), Finance());

        res.Success.Should().BeTrue();
        StatusOf(id).Should().Be(RefundRequestStatus.Approved);
        _sql.NewContext().RefundRequests.Single(r => r.RefundRequestId == id).ApprovedAt.Should().NotBeNull();
    }

    [Fact] // UT-RFP-02
    public async Task Approve_over_daily_cap_is_blocked()
    {
        _config.Set("refund.dailyCapVnd", "100000").Set("refund.dualControlThresholdVnd", "0");
        var id = CreatePending(amount: 199_000m);

        var res = await Svc().ApproveAsync(id, new ApproveRefundDto(), Finance());

        res.Success.Should().BeFalse();
        res.Message.Should().Contain("Vượt trần");
        StatusOf(id).Should().Be(RefundRequestStatus.PendingReview);
    }

    [Fact] // UT-RFP-08
    public async Task Approve_once_when_dual_control_disabled()
    {
        _config.Set("refund.dualControlThresholdVnd", "0").Set("refund.dailyCapVnd", "20000000");
        var id = CreatePending();

        (await Svc().ApproveAsync(id, new ApproveRefundDto(), Finance())).Success.Should().BeTrue();
        StatusOf(id).Should().Be(RefundRequestStatus.Approved);
    }

    [Fact] // UT-RFP-09
    public async Task First_approval_of_a_dual_control_request_does_not_consume_the_cap()
    {
        _config.Set("refund.dualControlThresholdVnd", "100000").Set("refund.dailyCapVnd", "20000000");
        var id = CreatePending(amount: 199_000m);

        var res = await Svc().ApproveAsync(id, new ApproveRefundDto(), Finance(userId: 10));

        res.Success.Should().BeTrue();
        StatusOf(id).Should().Be(RefundRequestStatus.PendingSecondApproval);
        _sql.NewContext().RefundRequests.Single(r => r.RefundRequestId == id)
            .FirstApprovedByUserId.Should().Be(10);
    }

    [Fact] // UT-RFP-10
    public async Task Same_person_cannot_do_both_approvals()
    {
        _config.Set("refund.dualControlThresholdVnd", "100000").Set("refund.dailyCapVnd", "20000000");
        var id = CreatePending(amount: 199_000m);
        await Svc().ApproveAsync(id, new ApproveRefundDto(), Finance(userId: 10));

        var res = await Svc().ApproveAsync(id, new ApproveRefundDto(), Finance(userId: 10));

        res.StatusCode.Should().Be(409);
        StatusOf(id).Should().Be(RefundRequestStatus.PendingSecondApproval);
    }

    [Fact] // UT-RFP-11
    public async Task Second_approver_completes_the_dual_control()
    {
        _config.Set("refund.dualControlThresholdVnd", "100000").Set("refund.dailyCapVnd", "20000000");
        var id = CreatePending(amount: 199_000m);
        await Svc().ApproveAsync(id, new ApproveRefundDto(), Finance(userId: 10));

        var res = await Svc().ApproveAsync(id, new ApproveRefundDto(), Finance(userId: 11));

        res.Success.Should().BeTrue();
        StatusOf(id).Should().Be(RefundRequestStatus.Approved);
    }

    [Fact] // UT-RFP-16
    public async Task Rejected_request_cannot_be_approved()
    {
        var id = CreatePending();
        await Svc().RejectAsync(id, new RejectRefundDto { Reason = "no" }, Finance());

        var res = await Svc().ApproveAsync(id, new ApproveRefundDto(), Finance());

        res.StatusCode.Should().Be(409);
    }
}
