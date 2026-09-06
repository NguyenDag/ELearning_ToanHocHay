using System.Net;
using System.Net.Http.Json;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay.Tests.Integration.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ELearning_ToanHocHay.Tests.Integration;

/// <summary>§4 F8 — Hoàn tiền bán tự động (IT-F8).</summary>
[Collection(IntegrationCollection.Name)]
[Trait("Level", "Integration")]
[Trait("Flow", "Refund")]
public class IT_F8_RefundTests : IntegrationTest
{
    public IT_F8_RefundTests(ApiFactory app) : base(app) { }

    private object CreateBody(int paymentId, decimal? amount = null, string holder = "Nguyen Van A") => new
    {
        PaymentId = paymentId, Amount = amount, ReasonCode = "CustomerRequest",
        BankBin = "970418", BankAccountNumber = "0071000123456", BankAccountHolderName = holder,
    };

    private Task<HttpResponseMessage> Approve(HttpClient finance, int id)
        => finance.PostAsJsonAsync($"/api/finance/refunds/{id}/approve", new { Note = "ok" });

    [SkippableFact] // IT-F8-01
    public async Task IT_F8_01_Student_creates_a_refund_request()
    {
        RequireDocker();
        var (userId, _) = await Flow.NewStudentAsync();
        var paymentId = await Flow.SeedRefundablePaymentAsync(userId);

        var res = await App.As(userId).PostAsJsonAsync("/api/refunds", CreateBody(paymentId));
        await res.ShouldBeCreated();
        var id = (await res.DataAsync()).GetProperty("RefundRequestId").GetInt32();

        await App.Db(async db =>
        {
            (await db.RefundRequests.SingleAsync(r => r.RefundRequestId == id)).Status
                .Should().Be(RefundRequestStatus.PendingReview);
            (await db.RefundEvents.AnyAsync(e => e.RefundRequestId == id && e.EventType == RefundEventType.Created))
                .Should().BeTrue();
        });
    }

    [SkippableFact] // IT-F8-02
    public async Task IT_F8_02_Cannot_refund_someone_elses_payment()
    {
        RequireDocker();
        var (ownerUserId, _) = await Flow.NewStudentAsync();
        var paymentId = await Flow.SeedRefundablePaymentAsync(ownerUserId);
        var (strangerUserId, _) = await Flow.NewStudentAsync();

        var res = await App.As(strangerUserId).PostAsJsonAsync("/api/refunds", CreateBody(paymentId));
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [SkippableFact] // IT-F8-03
    public async Task IT_F8_03_Cannot_refund_a_pending_payment()
    {
        RequireDocker();
        var (userId, studentId) = await Flow.NewStudentAsync();
        var pendingPaymentId = await App.Db(async db =>
        {
            var p = new Payment
            {
                PaidByUserId = userId, StudentId = studentId, Amount = 199_000m,
                PaymentMethod = PaymentMethod.BankTransfer, Status = PaymentStatus.Pending, PaymentDate = DateTime.UtcNow,
            };
            db.Payments.Add(p);
            await db.SaveChangesAsync();
            return p.PaymentId;
        });

        var res = await App.As(userId).PostAsJsonAsync("/api/refunds", CreateBody(pendingPaymentId));
        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [SkippableFact] // IT-F8-05
    public async Task IT_F8_05_A_second_open_request_is_409()
    {
        RequireDocker();
        var (userId, _) = await Flow.NewStudentAsync();
        var paymentId = await Flow.SeedRefundablePaymentAsync(userId, amount: 199_000m);

        await Flow.CreateRefundRequestAsync(userId, paymentId, amount: 50_000m);
        var second = await App.As(userId).PostAsJsonAsync("/api/refunds", CreateBody(paymentId, 50_000m));
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [SkippableFact] // IT-F8-06
    public async Task IT_F8_06_Per_user_30_day_limit()
    {
        RequireDocker();
        var (userId, _) = await Flow.NewStudentAsync();
        for (var i = 0; i < 3; i++)
        {
            var pid = await Flow.SeedRefundablePaymentAsync(userId);
            await Flow.CreateRefundRequestAsync(userId, pid, amount: 10_000m);
        }

        var fourthPayment = await Flow.SeedRefundablePaymentAsync(userId);
        var res = await App.As(userId).PostAsJsonAsync("/api/refunds", CreateBody(fourthPayment, 10_000m));
        res.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // SystemAdmin tạo hộ -> bỏ qua giới hạn
        var adminRes = await App.AsRole(TestRole.Admin).PostAsJsonAsync("/api/finance/refunds", CreateBody(fourthPayment, 10_000m));
        await adminRes.ShouldBeCreated();
    }

    [SkippableFact] // IT-F8-07
    public async Task IT_F8_07_Approve_moves_to_approved()
    {
        RequireDocker();
        var (userId, _) = await Flow.NewStudentAsync();
        var paymentId = await Flow.SeedRefundablePaymentAsync(userId);
        var id = await Flow.CreateRefundRequestAsync(userId, paymentId, amount: 50_000m);

        await (await Approve(App.AsRole(TestRole.Finance), id)).ShouldBeOk();
        await App.Db(async db =>
            (await db.RefundRequests.SingleAsync(r => r.RefundRequestId == id)).Status
                .Should().Be(RefundRequestStatus.Approved));
    }

    [SkippableFact] // IT-F8-08
    public async Task IT_F8_08_Daily_cap_blocks_an_approval()
    {
        RequireDocker();
        var (userId, _) = await Flow.NewStudentAsync();
        var paymentId = await Flow.SeedRefundablePaymentAsync(userId);
        var id = await Flow.CreateRefundRequestAsync(userId, paymentId, amount: 199_000m);

        await Flow.SetConfigAsync("refund.dailyCapVnd", "100000");
        try
        {
            var res = await Approve(App.AsRole(TestRole.Finance), id);
            await res.ShouldBeError(HttpStatusCode.BadRequest, "Vượt trần");
        }
        finally
        {
            await Flow.SetConfigAsync("refund.dailyCapVnd", "20000000");
        }
    }

    [SkippableFact] // IT-F8-09
    public async Task IT_F8_09_Dual_control_needs_two_distinct_approvers()
    {
        RequireDocker();
        var (userId, _) = await Flow.NewStudentAsync();
        var paymentId = await Flow.SeedRefundablePaymentAsync(userId);
        var id = await Flow.CreateRefundRequestAsync(userId, paymentId, amount: 199_000m);
        var (finance2UserId, _, _) = await Flow.NewConfirmedUserAsync(UserType.FinanceManager);

        await Flow.SetConfigAsync("refund.dualControlThresholdVnd", "100000");
        try
        {
            var first = await Approve(App.AsRole(TestRole.Finance), id);
            await first.ShouldBeOk();
            await App.Db(async db =>
                (await db.RefundRequests.SingleAsync(r => r.RefundRequestId == id)).Status
                    .Should().Be(RefundRequestStatus.PendingSecondApproval));

            (await Approve(App.AsRole(TestRole.Finance), id)).StatusCode.Should().Be(HttpStatusCode.Conflict);

            await (await Approve(App.As(finance2UserId), id)).ShouldBeOk();
            await App.Db(async db =>
                (await db.RefundRequests.SingleAsync(r => r.RefundRequestId == id)).Status
                    .Should().Be(RefundRequestStatus.Approved));
        }
        finally
        {
            await Flow.SetConfigAsync("refund.dualControlThresholdVnd", "0");
        }
    }

    [SkippableFact] // IT-F8-10
    public async Task IT_F8_10_Rejected_request_cannot_then_be_approved()
    {
        RequireDocker();
        var (userId, _) = await Flow.NewStudentAsync();
        var paymentId = await Flow.SeedRefundablePaymentAsync(userId);
        var id = await Flow.CreateRefundRequestAsync(userId, paymentId, amount: 50_000m);
        var finance = App.AsRole(TestRole.Finance);

        await (await finance.PostAsJsonAsync($"/api/finance/refunds/{id}/reject", new { Reason = "invalid" })).ShouldBeOk();
        (await Approve(finance, id)).StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [SkippableFact] // IT-F8-11
    public async Task IT_F8_11_Full_batch_flow_completes_the_refund()
    {
        RequireDocker();
        var (userId, _) = await Flow.NewStudentAsync();
        var (paymentId, subId) = await Flow.SeedActiveSubscriptionAsync(userId, Ids.PackageId);
        var id = await Flow.CreateRefundRequestAsync(userId, paymentId); // full
        var finance = App.AsRole(TestRole.Finance);

        await (await Approve(finance, id)).ShouldBeOk();
        var batchId = (await (await finance.PostAsJsonAsync("/api/finance/refund-batches",
            new { RefundRequestIds = new[] { id } })).DataAsync()).GetProperty("RefundBatchId").GetInt32();

        (await finance.GetAsync($"/api/finance/refund-batches/{batchId}/export"))
            .Content.Headers.ContentType!.MediaType.Should().Be("text/csv");
        await (await finance.PostAsJsonAsync($"/api/finance/refund-batches/{batchId}/mark-disbursed", new { Note = "done" })).ShouldBeOk();
        await (await finance.PostAsync($"/api/finance/refund-batches/{batchId}/confirm-all", null)).ShouldBeOk();

        await App.Db(async db =>
        {
            (await db.RefundRequests.SingleAsync(r => r.RefundRequestId == id)).Status.Should().Be(RefundRequestStatus.Completed);
            var payment = await db.Payments.SingleAsync(p => p.PaymentId == paymentId);
            payment.Status.Should().Be(PaymentStatus.Refunded);
            payment.RefundAmount.Should().Be(199_000m);
            (await db.Subscriptions.SingleAsync(s => s.SubscriptionId == subId)).Status.Should().Be(SubscriptionStatus.Cancelled);
        });
    }

    [SkippableFact] // IT-F8-12
    public async Task IT_F8_12_Partial_refund_leaves_the_payment_partially_refunded()
    {
        RequireDocker();
        var (userId, _) = await Flow.NewStudentAsync();
        var (paymentId, _) = await Flow.SeedActiveSubscriptionAsync(userId, Ids.PackageId);
        var id = await Flow.CreateRefundRequestAsync(userId, paymentId, amount: 100_000m);
        var finance = App.AsRole(TestRole.Finance);

        await (await Approve(finance, id)).ShouldBeOk();
        await (await finance.PostAsJsonAsync($"/api/finance/refunds/{id}/confirm", new { BankTransactionRef = "BANK-1" })).ShouldBeOk();

        await App.Db(async db =>
            (await db.Payments.SingleAsync(p => p.PaymentId == paymentId)).Status
                .Should().Be(PaymentStatus.PartiallyRefunded));

        // phần còn lại vẫn yêu cầu được
        var second = await App.As(userId).PostAsJsonAsync("/api/refunds", CreateBody(paymentId, 50_000m));
        await second.ShouldBeCreated();
    }

    [SkippableFact] // IT-F8-04
    public async Task IT_F8_04_Cannot_refund_a_payment_older_than_the_max_age()
    {
        RequireDocker();
        var (userId, _) = await Flow.NewStudentAsync();
        var paymentId = await Flow.SeedRefundablePaymentAsync(userId, paidAt: DateTime.UtcNow.AddDays(-400));

        var res = await App.As(userId).PostAsJsonAsync("/api/refunds", CreateBody(paymentId, 50_000m));
        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [SkippableFact] // IT-F8-15
    public async Task IT_F8_15_Confirm_requires_a_bank_transaction_ref()
    {
        RequireDocker();
        var (userId, _) = await Flow.NewStudentAsync();
        var paymentId = await Flow.SeedRefundablePaymentAsync(userId);
        var id = await Flow.CreateRefundRequestAsync(userId, paymentId, amount: 50_000m);
        var finance = App.AsRole(TestRole.Finance);
        await (await Approve(finance, id)).ShouldBeOk();

        (await finance.PostAsJsonAsync($"/api/finance/refunds/{id}/confirm", new { BankTransactionRef = "" }))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await (await finance.PostAsJsonAsync($"/api/finance/refunds/{id}/confirm", new { BankTransactionRef = "BANK-OK" })).ShouldBeOk();
    }

    [SkippableFact] // IT-F8-20
    public async Task IT_F8_20_Daily_usage_and_reconciliation_for_finance()
    {
        RequireDocker();
        var finance = App.AsRole(TestRole.Finance);

        var usage = await (await finance.GetAsync("/api/finance/refunds/daily-usage")).DataAsync();
        var cap = usage.GetProperty("CapVnd").GetDecimal();
        var used = usage.GetProperty("UsedVnd").GetDecimal();
        var remaining = usage.GetProperty("RemainingVnd").GetDecimal();
        remaining.Should().Be(Math.Max(0, cap - used));

        (await finance.GetAsync("/api/finance/refunds/reconciliation")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [SkippableFact] // IT-F8-21
    public async Task IT_F8_21_The_old_one_step_refund_route_is_gone()
    {
        RequireDocker();
        (await App.AsRole(TestRole.Finance).PostAsJsonAsync("/api/payments/1/refund", new { }))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [SkippableFact] // IT-F8-16
    public async Task IT_F8_16_Finance_endpoints_are_role_gated()
    {
        RequireDocker();
        (await App.AsRole(TestRole.StudentA).GetAsync("/api/finance/refunds")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await App.Anonymous().PostAsJsonAsync("/api/refunds", CreateBody(1))).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [SkippableFact] // IT-F8-17
    public async Task IT_F8_17_Csv_export_neutralises_a_formula_injection()
    {
        RequireDocker();
        var (userId, _) = await Flow.NewStudentAsync();
        var paymentId = await Flow.SeedRefundablePaymentAsync(userId);
        var id = await Flow.CreateRefundRequestAsync(userId, paymentId, amount: 50_000m, holderName: "=cmd|' /c calc'!A1");
        var finance = App.AsRole(TestRole.Finance);
        await (await Approve(finance, id)).ShouldBeOk();
        var batchId = (await (await finance.PostAsJsonAsync("/api/finance/refund-batches",
            new { RefundRequestIds = new[] { id } })).DataAsync()).GetProperty("RefundBatchId").GetInt32();

        var export = await finance.GetAsync($"/api/finance/refund-batches/{batchId}/export");
        export.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");
        var csv = await export.Content.ReadAsStringAsync();
        csv.Should().Contain("'=cmd");

        await App.Db(async db =>
            (await db.RefundBatches.SingleAsync(b => b.RefundBatchId == batchId)).Status
                .Should().Be(RefundBatchStatus.Exported));
    }

    [SkippableFact] // IT-F8-18
    public async Task IT_F8_18_Api_exposes_only_last4_db_stores_ciphertext()
    {
        RequireDocker();
        var (userId, _) = await Flow.NewStudentAsync();
        var paymentId = await Flow.SeedRefundablePaymentAsync(userId);
        var id = await Flow.CreateRefundRequestAsync(userId, paymentId, amount: 50_000m);

        var data = await (await App.As(userId).GetAsync($"/api/refunds/{id}")).DataAsync();
        data.GetProperty("BankAccountNumberLast4").GetString().Should().Be("3456");
        data.TryGetProperty("BankAccountNumberProtected", out _).Should().BeFalse();

        await App.Db(async db =>
        {
            var r = await db.RefundRequests.SingleAsync(x => x.RefundRequestId == id);
            r.BankAccountNumberProtected.Should().NotBeNullOrEmpty().And.NotContain("0071000123456");
        });
    }

    [SkippableFact] // IT-F8-19
    public async Task IT_F8_19_Owner_sees_their_request_stranger_is_403()
    {
        RequireDocker();
        var (userId, _) = await Flow.NewStudentAsync();
        var paymentId = await Flow.SeedRefundablePaymentAsync(userId);
        var id = await Flow.CreateRefundRequestAsync(userId, paymentId, amount: 50_000m);

        var mine = await (await App.As(userId).GetAsync("/api/refunds/me")).DataAsync();
        mine.GetProperty("Items").EnumerateArray().Select(x => x.GetProperty("RefundRequestId").GetInt32())
            .Should().Contain(id);

        var (strangerUserId, _) = await Flow.NewStudentAsync();
        (await App.As(strangerUserId).GetAsync($"/api/refunds/{id}")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
