using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ELearning_ToanHocHay_Control.Tests;

/// <summary>
/// Pha 2 — semi-automatic refund workflow: create → approve (daily cap, dual-control,
/// per-user rate limit) → batch → export CSV → mark-disbursed → confirm, with a full
/// RefundEvent audit trail and AuditLog on status change.
/// </summary>
[Collection(IntegrationCollection.Name)]
public class RefundWorkflowTests
{
    private readonly A1TestFactory _f;
    private SeededIds Id => _f.Ids;

    public RefundWorkflowTests(A1TestFactory f) => _f = f;

    private void RequireDocker() => Skip.IfNot(_f.DockerAvailable, "Docker not available — skipping integration test.");

    private static async Task<JsonElement> Root(HttpResponseMessage res)
        => JsonDocument.Parse(await res.Content.ReadAsStringAsync()).RootElement.Clone();

    /// <summary>A fresh Student payer + a Completed payment they made — fully isolated per test.</summary>
    private async Task<(int paymentId, int payerUserId)> SeedRefundablePaymentAsync(decimal amount = 199_000m)
        => await _f.QueryDbAsync(async db =>
        {
            var payer = new User
            {
                Email = $"refund-payer-{Guid.NewGuid():N}@test.local",
                PasswordHash = "x",
                FullName = "Refund Payer",
                UserType = UserType.Student,
                IsEmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            db.Users.Add(payer);
            await db.SaveChangesAsync();

            var payment = new Payment
            {
                PaidByUserId = payer.UserId,
                Amount = amount,
                Status = PaymentStatus.Completed,
                PaymentMethod = PaymentMethod.BankTransfer,
                PaymentDate = DateTime.UtcNow,
                TransactionId = $"SEED-{Guid.NewGuid():N}"
            };
            db.Payments.Add(payment);
            await db.SaveChangesAsync();
            return (payment.PaymentId, payer.UserId);
        });

    private static object CreateBody(int paymentId, decimal? amount = null, string reason = "CustomerRequest")
        => new
        {
            PaymentId = paymentId,
            Amount = amount,
            ReasonCode = reason,
            BankBin = "970436",
            BankAccountNumber = "0071000123456",
            BankAccountHolderName = "NGUYEN VAN A"
        };

    private async Task<int> CreateAsFinanceAsync(int paymentId, decimal? amount = null)
    {
        var res = await _f.ClientFor(Id.FinanceUserId).PostAsJsonAsync("/api/finance/refunds", CreateBody(paymentId, amount));
        res.StatusCode.Should().Be(HttpStatusCode.Created, await res.Content.ReadAsStringAsync());
        return (await Root(res)).GetProperty("Data").GetProperty("RefundRequestId").GetInt32();
    }

    private async Task ApproveAsync(int refundId)
    {
        var res = await _f.ClientFor(Id.FinanceUserId)
            .PostAsJsonAsync($"/api/finance/refunds/{refundId}/approve", new { Note = "ok" });
        res.StatusCode.Should().Be(HttpStatusCode.OK, await res.Content.ReadAsStringAsync());
    }

    private async Task SetConfigAsync(string key, string value)
    {
        var res = await _f.ClientFor(Id.AdminUserId).PutAsJsonAsync($"/api/admin/config/{key}", new { Value = value });
        res.StatusCode.Should().Be(HttpStatusCode.OK, await res.Content.ReadAsStringAsync());
    }

    // ---------------------------------------------------------------- create

    [SkippableFact]
    public async Task Student_creates_a_refund_request_for_their_own_payment()
    {
        RequireDocker();
        var (paymentId, payerUserId) = await SeedRefundablePaymentAsync();

        var res = await _f.ClientFor(payerUserId).PostAsJsonAsync("/api/refunds", CreateBody(paymentId));
        res.StatusCode.Should().Be(HttpStatusCode.Created, await res.Content.ReadAsStringAsync());

        var data = (await Root(res)).GetProperty("Data");
        data.GetProperty("Status").GetString().Should().Be("PendingReview");
        data.GetProperty("BankAccountNumberLast4").GetString().Should().Be("3456");
        var refundId = data.GetProperty("RefundRequestId").GetInt32();

        await _f.QueryDbAsync(async db =>
        {
            var events = await db.RefundEvents.Where(e => e.RefundRequestId == refundId).ToListAsync();
            events.Should().ContainSingle(e => e.EventType == RefundEventType.Created);
            // number is encrypted at rest, never stored plaintext
            var row = await db.RefundRequests.SingleAsync(r => r.RefundRequestId == refundId);
            row.BankAccountNumberProtected.Should().NotContain("0071000123456");
            return true;
        });
    }

    [SkippableFact]
    public async Task Student_cannot_refund_someone_elses_payment()
    {
        RequireDocker();
        var (paymentId, _) = await SeedRefundablePaymentAsync();

        var res = await _f.ClientFor(Id.StudentBUserId).PostAsJsonAsync("/api/refunds", CreateBody(paymentId));
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [SkippableFact]
    public async Task Cannot_refund_a_pending_payment_or_over_the_amount()
    {
        RequireDocker();

        (await _f.ClientFor(Id.FinanceUserId).PostAsJsonAsync("/api/finance/refunds", CreateBody(Id.PaymentAId)))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var (paymentId, _) = await SeedRefundablePaymentAsync(100_000m);
        (await _f.ClientFor(Id.FinanceUserId).PostAsJsonAsync("/api/finance/refunds", CreateBody(paymentId, 150_000m)))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [SkippableFact]
    public async Task A_second_open_request_for_the_same_payment_is_rejected()
    {
        RequireDocker();
        var (paymentId, _) = await SeedRefundablePaymentAsync();
        await CreateAsFinanceAsync(paymentId);

        var res = await _f.ClientFor(Id.FinanceUserId).PostAsJsonAsync("/api/finance/refunds", CreateBody(paymentId));
        res.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [SkippableFact]
    public async Task Per_user_30_day_limit_blocks_further_requests()
    {
        RequireDocker();
        await SetConfigAsync("refund.maxRequestsPerUserPer30d", "2");
        try
        {
            var payer = await _f.QueryDbAsync(async db =>
            {
                var u = new User
                {
                    Email = $"refund-limit-{Guid.NewGuid():N}@test.local", PasswordHash = "x",
                    FullName = "L", UserType = UserType.Student, IsEmailConfirmed = true, IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                db.Users.Add(u);
                await db.SaveChangesAsync();
                for (var i = 0; i < 3; i++)
                    db.Payments.Add(new Payment
                    {
                        PaidByUserId = u.UserId, Amount = 50_000m, Status = PaymentStatus.Completed,
                        PaymentMethod = PaymentMethod.BankTransfer, PaymentDate = DateTime.UtcNow
                    });
                await db.SaveChangesAsync();
                return u.UserId;
            });

            var payments = await _f.QueryDbAsync(db => db.Payments.Where(p => p.PaidByUserId == payer)
                .Select(p => p.PaymentId).ToListAsync());

            (await _f.ClientFor(Id.FinanceUserId).PostAsJsonAsync("/api/finance/refunds", CreateBody(payments[0])))
                .StatusCode.Should().Be(HttpStatusCode.Created);
            (await _f.ClientFor(Id.FinanceUserId).PostAsJsonAsync("/api/finance/refunds", CreateBody(payments[1])))
                .StatusCode.Should().Be(HttpStatusCode.Created);
            (await _f.ClientFor(Id.FinanceUserId).PostAsJsonAsync("/api/finance/refunds", CreateBody(payments[2])))
                .StatusCode.Should().Be(HttpStatusCode.Conflict);
        }
        finally
        {
            await SetConfigAsync("refund.maxRequestsPerUserPer30d", "3");
        }
    }

    // ---------------------------------------------------------------- approve

    [SkippableFact]
    public async Task Approve_moves_to_Approved_and_consumes_the_daily_cap()
    {
        RequireDocker();
        var (paymentId, _) = await SeedRefundablePaymentAsync();
        var refundId = await CreateAsFinanceAsync(paymentId);

        var before = (await Root(await _f.ClientFor(Id.FinanceUserId).GetAsync("/api/finance/refunds/daily-usage")))
            .GetProperty("Data").GetProperty("UsedVnd").GetDecimal();

        await ApproveAsync(refundId);

        var after = (await Root(await _f.ClientFor(Id.FinanceUserId).GetAsync("/api/finance/refunds/daily-usage")))
            .GetProperty("Data").GetProperty("UsedVnd").GetDecimal();
        (after - before).Should().Be(199_000m);
    }

    [SkippableFact]
    public async Task Daily_cap_blocks_an_approval_over_the_limit()
    {
        RequireDocker();

        // The daily cap window is process-wide; set the cap relative to what's already used
        // so exactly one more 199k approval fits and the next does not.
        var used = (await Root(await _f.ClientFor(Id.FinanceUserId).GetAsync("/api/finance/refunds/daily-usage")))
            .GetProperty("Data").GetProperty("UsedVnd").GetDecimal();
        await SetConfigAsync("refund.dailyCapVnd", (used + 300_000m).ToString("0"));
        try
        {
            var (p1, _) = await SeedRefundablePaymentAsync(199_000m);
            var (p2, _) = await SeedRefundablePaymentAsync(199_000m);
            var r1 = await CreateAsFinanceAsync(p1);
            var r2 = await CreateAsFinanceAsync(p2);

            await ApproveAsync(r1); // used + 199k <= used + 300k -> ok

            var res = await _f.ClientFor(Id.FinanceUserId)
                .PostAsJsonAsync($"/api/finance/refunds/{r2}/approve", new { });
            res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await Root(res)).GetProperty("Message").GetString().Should().Contain("trần");
        }
        finally
        {
            await SetConfigAsync("refund.dailyCapVnd", "20000000");
        }
    }

    [SkippableFact]
    public async Task Dual_control_needs_two_distinct_approvers()
    {
        RequireDocker();
        await SetConfigAsync("refund.dualControlThresholdVnd", "100000");
        try
        {
            var (paymentId, _) = await SeedRefundablePaymentAsync(199_000m);
            var refundId = await CreateAsFinanceAsync(paymentId);

            var first = await _f.ClientFor(Id.FinanceUserId)
                .PostAsJsonAsync($"/api/finance/refunds/{refundId}/approve", new { });
            first.StatusCode.Should().Be(HttpStatusCode.OK);
            (await Root(first)).GetProperty("Data").GetProperty("Status").GetString().Should().Be("PendingSecondApproval");

            var sameUser = await _f.ClientFor(Id.FinanceUserId)
                .PostAsJsonAsync($"/api/finance/refunds/{refundId}/approve", new { });
            sameUser.StatusCode.Should().Be(HttpStatusCode.Conflict);

            var secondUser = await _f.ClientFor(Id.AdminUserId)
                .PostAsJsonAsync($"/api/finance/refunds/{refundId}/approve", new { });
            secondUser.StatusCode.Should().Be(HttpStatusCode.OK);
            (await Root(secondUser)).GetProperty("Data").GetProperty("Status").GetString().Should().Be("Approved");
        }
        finally
        {
            await SetConfigAsync("refund.dualControlThresholdVnd", "0");
        }
    }

    [SkippableFact]
    public async Task Rejected_request_cannot_then_be_approved()
    {
        RequireDocker();
        var (paymentId, _) = await SeedRefundablePaymentAsync();
        var refundId = await CreateAsFinanceAsync(paymentId);

        (await _f.ClientFor(Id.FinanceUserId)
            .PostAsJsonAsync($"/api/finance/refunds/{refundId}/reject", new { Reason = "không hợp lệ" }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        (await _f.ClientFor(Id.FinanceUserId).PostAsJsonAsync($"/api/finance/refunds/{refundId}/approve", new { }))
            .StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // ---------------------------------------------------------------- batch → CSV → disburse → confirm

    [SkippableFact]
    public async Task Full_batch_flow_completes_the_payment_and_writes_the_audit_trail()
    {
        RequireDocker();
        var (paymentId, _) = await SeedRefundablePaymentAsync(199_000m);
        var refundId = await CreateAsFinanceAsync(paymentId);
        await ApproveAsync(refundId);

        // batch
        var batchRes = await _f.ClientFor(Id.FinanceUserId)
            .PostAsJsonAsync("/api/finance/refund-batches", new { RefundRequestIds = new[] { refundId } });
        batchRes.StatusCode.Should().Be(HttpStatusCode.Created, await batchRes.Content.ReadAsStringAsync());
        var batchId = (await Root(batchRes)).GetProperty("Data").GetProperty("RefundBatchId").GetInt32();

        await _f.QueryDbAsync(async db =>
        {
            (await db.RefundRequests.SingleAsync(r => r.RefundRequestId == refundId)).Status
                .Should().Be(RefundRequestStatus.Batched);
            return true;
        });

        // export CSV
        var csvRes = await _f.ClientFor(Id.FinanceUserId).GetAsync($"/api/finance/refund-batches/{batchId}/export");
        csvRes.StatusCode.Should().Be(HttpStatusCode.OK);
        csvRes.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");
        var csv = await csvRes.Content.ReadAsStringAsync();
        var lines = csv.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        lines[0].Should().Be("STT,SoTaiKhoan,TenNguoiHuong,MaNganHang,SoTien,NoiDung");
        lines[1].Should().StartWith("1,0071000123456,NGUYEN VAN A,970436,199000,");

        // mark disbursed
        (await _f.ClientFor(Id.FinanceUserId)
            .PostAsJsonAsync($"/api/finance/refund-batches/{batchId}/mark-disbursed", new { Note = "uploaded" }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        // confirm all
        (await _f.ClientFor(Id.FinanceUserId).PostAsync($"/api/finance/refund-batches/{batchId}/confirm-all", null))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        await _f.QueryDbAsync(async db =>
        {
            var req = await db.RefundRequests.SingleAsync(r => r.RefundRequestId == refundId);
            req.Status.Should().Be(RefundRequestStatus.Completed);

            var payment = await db.Payments.SingleAsync(p => p.PaymentId == paymentId);
            payment.Status.Should().Be(PaymentStatus.Refunded);
            payment.RefundAmount.Should().Be(199_000m);

            var types = await db.RefundEvents.Where(e => e.RefundRequestId == refundId)
                .Select(e => e.EventType).ToListAsync();
            types.Should().Contain(new[]
            {
                RefundEventType.Created, RefundEventType.Approved,
                RefundEventType.AddedToBatch, RefundEventType.MarkedDisbursed, RefundEventType.Confirmed
            });

            var audit = await db.AuditLogs.Where(a => a.EntityType == "RefundRequest" && a.EntityId == refundId)
                .ToListAsync();
            audit.Should().NotBeEmpty("the AuditSaveChangesInterceptor records RefundRequest status changes");
            return true;
        });
    }

    [SkippableFact]
    public async Task Partial_refund_leaves_the_payment_PartiallyRefunded_and_allows_a_second()
    {
        RequireDocker();
        var (paymentId, _) = await SeedRefundablePaymentAsync(200_000m);

        var r1 = await CreateAsFinanceAsync(paymentId, 120_000m);
        await ApproveAsync(r1);
        (await _f.ClientFor(Id.FinanceUserId)
            .PostAsJsonAsync($"/api/finance/refunds/{r1}/confirm", new { BankTransactionRef = "FT001" }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        await _f.QueryDbAsync(async db =>
        {
            (await db.Payments.SingleAsync(p => p.PaymentId == paymentId)).Status
                .Should().Be(PaymentStatus.PartiallyRefunded);
            return true;
        });

        // remaining 80k can still be requested
        var r2 = await _f.ClientFor(Id.FinanceUserId).PostAsJsonAsync("/api/finance/refunds", CreateBody(paymentId, 80_000m));
        r2.StatusCode.Should().Be(HttpStatusCode.Created);

        // a request over the remainder is rejected up front (400)
        (await _f.ClientFor(Id.FinanceUserId).PostAsJsonAsync("/api/finance/refunds", CreateBody(paymentId, 90_000m)))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // and a second in-flight request for the same payment is a conflict
        (await _f.ClientFor(Id.FinanceUserId).PostAsJsonAsync("/api/finance/refunds", CreateBody(paymentId, 50_000m)))
            .StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [SkippableFact]
    public async Task Cancelling_a_batch_returns_its_requests_to_Approved()
    {
        RequireDocker();
        var (paymentId, _) = await SeedRefundablePaymentAsync();
        var refundId = await CreateAsFinanceAsync(paymentId);
        await ApproveAsync(refundId);

        var batchId = (await Root(await _f.ClientFor(Id.FinanceUserId)
                .PostAsJsonAsync("/api/finance/refund-batches", new { RefundRequestIds = new[] { refundId } })))
            .GetProperty("Data").GetProperty("RefundBatchId").GetInt32();

        (await _f.ClientFor(Id.FinanceUserId).PostAsync($"/api/finance/refund-batches/{batchId}/cancel", null))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        await _f.QueryDbAsync(async db =>
        {
            var r = await db.RefundRequests.SingleAsync(x => x.RefundRequestId == refundId);
            r.Status.Should().Be(RefundRequestStatus.Approved);
            r.RefundBatchId.Should().BeNull();
            return true;
        });
    }

    // ---------------------------------------------------------------- role gating

    [SkippableFact]
    public async Task Finance_endpoints_are_role_gated()
    {
        RequireDocker();
        var (paymentId, payerUserId) = await SeedRefundablePaymentAsync();

        (await _f.ClientFor(payerUserId).GetAsync("/api/finance/refunds"))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await _f.ClientFor(payerUserId).GetAsync("/api/finance/refunds/reconciliation"))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var recon = await _f.ClientFor(Id.FinanceUserId).GetAsync("/api/finance/refunds/reconciliation");
        recon.StatusCode.Should().Be(HttpStatusCode.OK, await recon.Content.ReadAsStringAsync());
        (await Root(recon)).TryGetProperty("Balanced", out _).Should().BeTrue();
    }

    [SkippableFact]
    public async Task A_non_finance_user_cannot_reach_the_finance_refund_actions()
    {
        RequireDocker();
        var (paymentId, payerUserId) = await SeedRefundablePaymentAsync();
        var refundId = await CreateAsFinanceAsync(paymentId);

        // anonymous
        (await _f.CreateClient().PostAsJsonAsync("/api/refunds", CreateBody(paymentId)))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // authenticated non-finance hitting finance-only actions
        foreach (var (method, url) in new (string, string)[]
        {
            ("POST", $"/api/finance/refunds/{refundId}/approve"),
            ("POST", $"/api/finance/refunds/{refundId}/confirm"),
            ("POST", "/api/finance/refund-batches"),
            ("GET",  $"/api/finance/refund-batches/1/export"),
            ("GET",  "/api/finance/refunds/daily-usage"),
        })
        {
            var req = new HttpRequestMessage(new HttpMethod(method), url);
            if (method == "POST") req.Content = JsonContent.Create(new { });
            var res = await _f.ClientFor(payerUserId).SendAsync(req);
            res.StatusCode.Should().Be(HttpStatusCode.Forbidden, $"{method} {url}");
        }
    }

    [SkippableFact]
    public async Task Csv_export_neutralises_a_formula_injection_in_the_holder_name()
    {
        RequireDocker();
        var (paymentId, _) = await SeedRefundablePaymentAsync();

        var createRes = await _f.ClientFor(Id.FinanceUserId).PostAsJsonAsync("/api/finance/refunds", new
        {
            PaymentId = paymentId, ReasonCode = "CustomerRequest",
            BankBin = "970436", BankAccountNumber = "0071000123456",
            BankAccountHolderName = "=HYPERLINK(\"http://evil\")"
        });
        var refundId = (await Root(createRes)).GetProperty("Data").GetProperty("RefundRequestId").GetInt32();
        await ApproveAsync(refundId);

        var batchId = (await Root(await _f.ClientFor(Id.FinanceUserId)
                .PostAsJsonAsync("/api/finance/refund-batches", new { RefundRequestIds = new[] { refundId } })))
            .GetProperty("Data").GetProperty("RefundBatchId").GetInt32();

        var csv = await (await _f.ClientFor(Id.FinanceUserId)
            .GetAsync($"/api/finance/refund-batches/{batchId}/export")).Content.ReadAsStringAsync();

        csv.Should().Contain("'=HYPERLINK", "a leading '=' must be escaped so Excel treats it as text");
        csv.Should().NotContain(",=HYPERLINK");
    }

    [SkippableFact]
    public async Task Owner_sees_their_request_in_me_and_a_stranger_gets_403_on_detail()
    {
        RequireDocker();
        var (paymentId, payerUserId) = await SeedRefundablePaymentAsync();
        var res = await _f.ClientFor(payerUserId).PostAsJsonAsync("/api/refunds", CreateBody(paymentId));
        var refundId = (await Root(res)).GetProperty("Data").GetProperty("RefundRequestId").GetInt32();

        var mine = await Root(await _f.ClientFor(payerUserId).GetAsync("/api/refunds/me"));
        mine.GetProperty("Data").GetProperty("Items").EnumerateArray()
            .Should().Contain(e => e.GetProperty("RefundRequestId").GetInt32() == refundId);

        (await _f.ClientFor(Id.StudentBUserId).GetAsync($"/api/refunds/{refundId}"))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
