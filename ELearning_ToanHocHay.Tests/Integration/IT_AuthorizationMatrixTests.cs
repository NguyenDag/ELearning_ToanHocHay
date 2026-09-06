using System.Net;
using System.Net.Http.Json;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay.Tests.Integration.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ELearning_ToanHocHay.Tests.Integration;

/// <summary>
/// §5 — Ma trận phân quyền (data-driven). Mỗi test = 1 dòng route, chạy qua mọi vai trò và
/// khẳng định mã trạng thái. Kỳ vọng <c>null</c> = "đã qua cửa phân quyền" (khác 401/403) —
/// dùng cho các route có hiệu ứng phụ / lỗi nghiệp vụ sau khi cho phép.
///
/// Một số ô lệch với bảng gốc trong tài liệu vì bảng ghi <em>ý định</em>, còn ở đây khẳng định
/// <em>hành vi thật của code</em> (tài liệu: "chốt theo code"):
///  • dashboard/overview — <c>CoreDashboardService.VerifyStudentAccessAsync</c> KHÔNG bypass cho
///    SystemAdmin ⇒ Admin đọc dashboard của học sinh khác = 403 (bảng ghi 200²).
///  • POST /api/subscriptions — <c>ResourceAccessService.CanAccessStudentAsync</c> chỉ bypass cho
///    SystemAdmin ⇒ Finance mua hộ = 403 (bảng ghi 200); phụ huynh liên kết mua hộ = cho phép.
/// </summary>
[Collection(IntegrationCollection.Name)]
[Trait("Level", "Integration")]
[Trait("Flow", "Authz")]
public class IT_AuthorizationMatrixTests : IntegrationTest
{
    public IT_AuthorizationMatrixTests(ApiFactory app) : base(app) { }

    private static string Rand() => Guid.NewGuid().ToString("N")[..8];

    /// <summary>Chạy <paramref name="call"/> cho từng (nhãn, client) và khẳng định trạng thái.</summary>
    private static async Task Matrix(
        Func<HttpClient, Task<HttpResponseMessage>> call,
        params (string who, HttpClient client, HttpStatusCode? expected)[] cases)
    {
        foreach (var (who, client, expected) in cases)
        {
            var res = await call(client);
            if (expected is { } code)
            {
                res.StatusCode.Should().Be(code, $"role={who}");
            }
            else
            {
                res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized, $"role={who} phải qua được cửa phân quyền");
                res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden, $"role={who} phải qua được cửa phân quyền");
            }
        }
    }

    private (string, HttpClient, HttpStatusCode?) Case(TestRole role, HttpStatusCode? expected)
        => (role.ToString(), App.AsRole(role), expected);

    private (string, HttpClient, HttpStatusCode?) Anon(HttpStatusCode? expected)
        => ("Anonymous", App.Anonymous(), expected);

    private Task<int> NewSubjectId() => App.Db(async db =>
    {
        var s = new Subject
        {
            Code = $"S{Rand()}"[..8], Name = $"Môn {Rand()}", Slug = $"mon-{Rand()}",
            ColorHex = "#334455", DisplayOrder = 60, IsActive = true,
        };
        db.Subjects.Add(s);
        await db.SaveChangesAsync();
        return s.SubjectId;
    });

    // ================================================================ users / catalog

    [SkippableFact] // IT-AUTHZ — GET /api/users
    public async Task Authz_GET_users()
    {
        RequireDocker();
        await Matrix(c => c.GetAsync("/api/users"),
            Anon(HttpStatusCode.Unauthorized),
            Case(TestRole.StudentA, HttpStatusCode.Forbidden),
            Case(TestRole.StudentB, HttpStatusCode.Forbidden),
            Case(TestRole.ParentLinked, HttpStatusCode.Forbidden),
            Case(TestRole.ParentStranger, HttpStatusCode.Forbidden),
            Case(TestRole.Editor, HttpStatusCode.Forbidden),
            Case(TestRole.Finance, HttpStatusCode.Forbidden),
            Case(TestRole.Admin, HttpStatusCode.OK));
    }

    [SkippableFact] // IT-AUTHZ — GET /api/catalog/subjects
    public async Task Authz_GET_catalog_subjects_is_public()
    {
        RequireDocker();
        await Matrix(c => c.GetAsync("/api/catalog/subjects"),
            Anon(HttpStatusCode.OK),
            Case(TestRole.StudentA, HttpStatusCode.OK),
            Case(TestRole.ParentStranger, HttpStatusCode.OK),
            Case(TestRole.Editor, HttpStatusCode.OK),
            Case(TestRole.Finance, HttpStatusCode.OK),
            Case(TestRole.Admin, HttpStatusCode.OK));
    }

    [SkippableFact] // IT-AUTHZ — POST /api/catalog/subjects
    public async Task Authz_POST_catalog_subjects()
    {
        RequireDocker();
        Task<HttpResponseMessage> Call(HttpClient c) => c.PostAsJsonAsync("/api/catalog/subjects",
            new { Code = $"C{Rand()}"[..8], Name = $"N {Rand()}", Slug = $"s-{Rand()}" });

        await Matrix(Call,
            Anon(HttpStatusCode.Unauthorized),
            Case(TestRole.StudentA, HttpStatusCode.Forbidden),
            Case(TestRole.ParentLinked, HttpStatusCode.Forbidden),
            Case(TestRole.Finance, HttpStatusCode.Forbidden),
            Case(TestRole.Editor, HttpStatusCode.OK),
            Case(TestRole.Admin, HttpStatusCode.OK));
    }

    // ================================================================ learn nodes

    [SkippableFact] // IT-AUTHZ — GET /api/learn/nodes/{freeNode}
    public async Task Authz_GET_free_node_is_public()
    {
        RequireDocker();
        var course = await Flow.PublishCourseAsync();
        await Matrix(c => c.GetAsync($"/api/learn/nodes/{course.FreeLessonId}"),
            Anon(HttpStatusCode.OK),
            Case(TestRole.StudentB, HttpStatusCode.OK),
            Case(TestRole.ParentStranger, HttpStatusCode.OK),
            Case(TestRole.Editor, HttpStatusCode.OK),
            Case(TestRole.Finance, HttpStatusCode.OK),
            Case(TestRole.Admin, HttpStatusCode.OK));
    }

    [SkippableFact] // IT-AUTHZ — GET /api/learn/nodes/{paidNode}
    public async Task Authz_GET_paid_node()
    {
        RequireDocker();
        var course = await Flow.PublishCourseAsync();
        var (ownerUserId, ownerStudentId) = await Flow.NewStudentAsync();
        await Flow.GrantEntitlementAsync(ownerStudentId, EntitlementScope.SubjectGrade, subjectId: 1, gradeId: 1);

        await Matrix(c => c.GetAsync($"/api/learn/nodes/{course.FirstPaidLessonId}"),
            Anon(HttpStatusCode.Forbidden),
            ("EntitledOwner", App.As(ownerUserId), HttpStatusCode.OK),
            Case(TestRole.StudentB, HttpStatusCode.Forbidden),
            Case(TestRole.ParentLinked, HttpStatusCode.Forbidden),
            Case(TestRole.ParentStranger, HttpStatusCode.Forbidden),
            Case(TestRole.Editor, HttpStatusCode.OK),
            Case(TestRole.Finance, HttpStatusCode.Forbidden),
            Case(TestRole.Admin, HttpStatusCode.OK));
    }

    // ================================================================ exercise attempts

    [SkippableFact] // IT-AUTHZ — POST /api/exercise-attempts/save-answer (attempt của Student A)
    public async Task Authz_POST_save_answer()
    {
        RequireDocker();
        var exId = await Flow.PublishExerciseAsync();
        var start = await App.AsRole(TestRole.StudentA).PostAsJsonAsync("/api/exercise-attempts/start", new { ExerciseId = exId });
        await start.ShouldBeOk();
        var attemptId = (await start.DataAsync()).GetProperty("AttemptId").GetInt32();

        Task<HttpResponseMessage> Save(HttpClient c) => c.PostAsJsonAsync("/api/exercise-attempts/save-answer",
            new { AttemptId = attemptId, QuestionId = Ids.McQuestionId, SelectedOptionId = Ids.McCorrectOptionId });

        await Matrix(Save,
            Anon(HttpStatusCode.Unauthorized),
            Case(TestRole.StudentB, HttpStatusCode.Forbidden),
            Case(TestRole.ParentLinked, HttpStatusCode.Forbidden),
            Case(TestRole.ParentStranger, HttpStatusCode.Forbidden),
            Case(TestRole.Editor, HttpStatusCode.Forbidden),
            Case(TestRole.Finance, HttpStatusCode.Forbidden),
            Case(TestRole.Admin, HttpStatusCode.Forbidden),
            Case(TestRole.StudentA, HttpStatusCode.OK));
    }

    [SkippableFact] // IT-AUTHZ — GET /api/exercise-attempts/{A}/result
    public async Task Authz_GET_attempt_result()
    {
        RequireDocker();
        var exId = await Flow.PublishExerciseAsync();
        var studentA = App.AsRole(TestRole.StudentA);
        var attemptId = (await (await studentA.PostAsJsonAsync("/api/exercise-attempts/start", new { ExerciseId = exId })).DataAsync())
            .GetProperty("AttemptId").GetInt32();
        await (await studentA.PostAsJsonAsync("/api/exercise-attempts/complete", new { AttemptId = attemptId })).ShouldBeOk();

        await Matrix(c => c.GetAsync($"/api/exercise-attempts/{attemptId}/result"),
            Anon(HttpStatusCode.Unauthorized),
            Case(TestRole.StudentA, HttpStatusCode.OK),
            Case(TestRole.StudentB, HttpStatusCode.Forbidden),
            Case(TestRole.ParentLinked, HttpStatusCode.OK),
            Case(TestRole.ParentStranger, HttpStatusCode.Forbidden),
            Case(TestRole.Editor, HttpStatusCode.Forbidden),
            Case(TestRole.Finance, HttpStatusCode.Forbidden),
            Case(TestRole.Admin, HttpStatusCode.OK));
    }

    [SkippableFact] // IT-AUTHZ — GET /api/exercise-attempts/student/{A}/history
    public async Task Authz_GET_attempt_history()
    {
        RequireDocker();
        await Matrix(c => c.GetAsync($"/api/exercise-attempts/student/{Ids.StudentAId}/history"),
            Anon(HttpStatusCode.Unauthorized),
            Case(TestRole.StudentA, HttpStatusCode.OK),
            Case(TestRole.StudentB, HttpStatusCode.Forbidden),
            Case(TestRole.ParentLinked, HttpStatusCode.OK),
            Case(TestRole.ParentStranger, HttpStatusCode.Forbidden),
            Case(TestRole.Editor, HttpStatusCode.Forbidden),
            Case(TestRole.Finance, HttpStatusCode.Forbidden),
            Case(TestRole.Admin, HttpStatusCode.OK));
    }

    [SkippableFact] // IT-AUTHZ — GET /api/students/{A}/dashboard/overview
    public async Task Authz_GET_dashboard_overview()
    {
        RequireDocker();
        await Matrix(c => c.GetAsync($"/api/students/{Ids.StudentAId}/dashboard/overview"),
            Anon(HttpStatusCode.Unauthorized),
            Case(TestRole.StudentA, HttpStatusCode.OK),
            Case(TestRole.StudentB, HttpStatusCode.Forbidden),
            Case(TestRole.ParentLinked, HttpStatusCode.OK),
            Case(TestRole.ParentStranger, HttpStatusCode.Forbidden),
            Case(TestRole.Editor, HttpStatusCode.Forbidden),
            Case(TestRole.Finance, HttpStatusCode.Forbidden),
            Case(TestRole.Admin, HttpStatusCode.Forbidden)); // KHÔNG bypass cho admin
    }

    // ================================================================ payments / subscriptions

    [SkippableFact] // IT-AUTHZ — POST /api/subscriptions (cho Student A)
    public async Task Authz_POST_subscriptions()
    {
        RequireDocker();
        Task<HttpResponseMessage> Call(HttpClient c) => c.PostAsJsonAsync("/api/subscriptions",
            new { StudentId = Ids.StudentAId, PackageId = Ids.PackageId });

        await Matrix(Call,
            Anon(HttpStatusCode.Unauthorized),
            Case(TestRole.StudentB, HttpStatusCode.Forbidden),
            Case(TestRole.ParentStranger, HttpStatusCode.Forbidden),
            Case(TestRole.Editor, HttpStatusCode.Forbidden),
            Case(TestRole.Finance, HttpStatusCode.Forbidden),   // chỉ SystemAdmin được bypass
            Case(TestRole.ParentLinked, null),                  // phụ huynh liên kết mua hộ: cho phép
            Case(TestRole.StudentA, null),
            Case(TestRole.Admin, null));
    }

    [SkippableFact] // IT-AUTHZ — PATCH /api/subscriptions/{id}/status
    public async Task Authz_PATCH_subscription_status()
    {
        RequireDocker();
        var (userId, _) = await Flow.NewStudentAsync();
        var (subId, _) = await Flow.CreatePendingSubscriptionAsync(userId, Ids.PackageId);

        Task<HttpResponseMessage> Call(HttpClient c) =>
            c.PatchAsJsonAsync($"/api/subscriptions/{subId}/status", new { Status = "Cancelled" });

        await Matrix(Call,
            Anon(HttpStatusCode.Unauthorized),
            Case(TestRole.StudentA, HttpStatusCode.Forbidden),
            Case(TestRole.ParentLinked, HttpStatusCode.Forbidden),
            Case(TestRole.Editor, HttpStatusCode.Forbidden),
            Case(TestRole.Finance, null),
            Case(TestRole.Admin, null));
    }

    [SkippableFact] // IT-AUTHZ — GET /api/payments
    public async Task Authz_GET_payments()
    {
        RequireDocker();
        await Matrix(c => c.GetAsync("/api/payments"),
            Anon(HttpStatusCode.Unauthorized),
            Case(TestRole.StudentA, HttpStatusCode.Forbidden),
            Case(TestRole.ParentLinked, HttpStatusCode.Forbidden),
            Case(TestRole.Editor, HttpStatusCode.Forbidden),
            Case(TestRole.Finance, HttpStatusCode.OK),
            Case(TestRole.Admin, HttpStatusCode.OK));
    }

    // ================================================================ refunds

    [SkippableFact] // IT-AUTHZ — POST /api/refunds (payment của owner)
    public async Task Authz_POST_refunds()
    {
        RequireDocker();
        var (payerUserId, _) = await Flow.NewStudentAsync();
        var paymentId = await Flow.SeedRefundablePaymentAsync(payerUserId);

        Task<HttpResponseMessage> Call(HttpClient c) => c.PostAsJsonAsync("/api/refunds", new
        {
            PaymentId = paymentId, Amount = 20_000m, ReasonCode = "CustomerRequest",
            BankBin = "970418", BankAccountNumber = "0071000123456", BankAccountHolderName = "Ng Van A",
        });

        await Matrix(Call,
            Anon(HttpStatusCode.Unauthorized),
            Case(TestRole.StudentB, HttpStatusCode.Forbidden),
            Case(TestRole.ParentLinked, HttpStatusCode.Forbidden),
            Case(TestRole.ParentStranger, HttpStatusCode.Forbidden),
            Case(TestRole.Editor, HttpStatusCode.Forbidden),
            ("Payer", App.As(payerUserId), null),
            Case(TestRole.Finance, null),
            Case(TestRole.Admin, null));
    }

    [SkippableFact] // IT-AUTHZ — POST /api/finance/refunds/{id}/approve
    public async Task Authz_POST_finance_approve()
    {
        RequireDocker();
        var (userId, _) = await Flow.NewStudentAsync();
        var paymentId = await Flow.SeedRefundablePaymentAsync(userId);
        var refundId = await Flow.CreateRefundRequestAsync(userId, paymentId, amount: 20_000m);

        Task<HttpResponseMessage> Call(HttpClient c) => c.PostAsJsonAsync($"/api/finance/refunds/{refundId}/approve", new { Note = "ok" });

        await Matrix(Call,
            Anon(HttpStatusCode.Unauthorized),
            Case(TestRole.StudentA, HttpStatusCode.Forbidden),
            Case(TestRole.ParentLinked, HttpStatusCode.Forbidden),
            Case(TestRole.Editor, HttpStatusCode.Forbidden),
            Case(TestRole.Finance, null),
            Case(TestRole.Admin, null));
    }

    // ================================================================ authoring / admin

    [SkippableFact] // IT-AUTHZ — POST /api/courses
    public async Task Authz_POST_courses()
    {
        RequireDocker();
        async Task<HttpResponseMessage> Call(HttpClient c)
        {
            var subjectId = await NewSubjectId();
            return await c.PostAsJsonAsync("/api/courses", new
            {
                SubjectId = subjectId, GradeLevelId = 1, Title = $"K {Rand()}", Slug = $"k-{Rand()}",
                ListPrice = 0m, IsPurchasable = true, DisplayOrder = 0,
            });
        }

        await Matrix(Call,
            Anon(HttpStatusCode.Unauthorized),
            Case(TestRole.StudentA, HttpStatusCode.Forbidden),
            Case(TestRole.ParentLinked, HttpStatusCode.Forbidden),
            Case(TestRole.Finance, HttpStatusCode.Forbidden),
            Case(TestRole.Editor, HttpStatusCode.OK),
            Case(TestRole.Admin, HttpStatusCode.OK));
    }

    [SkippableFact] // IT-AUTHZ — POST /api/courses/versions/{v}/publish
    public async Task Authz_POST_publish_version()
    {
        RequireDocker();
        var editor = App.AsRole(TestRole.Editor);
        var admin = App.AsRole(TestRole.Admin);

        var subjectId = await NewSubjectId();
        var created = await (await editor.PostAsJsonAsync("/api/courses", new
        {
            SubjectId = subjectId, GradeLevelId = 1, Title = $"K {Rand()}", Slug = $"k-{Rand()}",
            ListPrice = 0m, IsPurchasable = true, DisplayOrder = 0,
        })).DataAsync();
        var versionId = created.GetProperty("Versions").EnumerateArray().First().GetProperty("CourseVersionId").GetInt32();
        await (await editor.PostAsJsonAsync($"/api/content/versions/{versionId}/nodes", new { NodeType = "Chapter", Title = "C" })).ShouldBeOk();
        await (await editor.PostAsync($"/api/courses/versions/{versionId}/submit", null)).ShouldBeOk();
        await (await admin.PostAsJsonAsync($"/api/courses/versions/{versionId}/review", new { Decision = "Approve" })).ShouldBeOk();

        await Matrix(c => c.PostAsync($"/api/courses/versions/{versionId}/publish", null),
            Anon(HttpStatusCode.Unauthorized),
            Case(TestRole.StudentA, HttpStatusCode.Forbidden),
            Case(TestRole.ParentLinked, HttpStatusCode.Forbidden),
            Case(TestRole.Finance, HttpStatusCode.Forbidden),
            Case(TestRole.Editor, HttpStatusCode.Forbidden),   // Editor chỉ submit
            Case(TestRole.Admin, HttpStatusCode.OK));
    }

    [SkippableFact] // IT-AUTHZ — POST /api/admin/users/{id}/role
    public async Task Authz_POST_admin_role_change()
    {
        RequireDocker();
        var (targetUserId, _, _) = await Flow.NewConfirmedUserAsync(UserType.SupportStaff);

        Task<HttpResponseMessage> Call(HttpClient c) =>
            c.PostAsJsonAsync($"/api/admin/users/{targetUserId}/role", new { NewRole = "ContentEditor" });

        await Matrix(Call,
            Anon(HttpStatusCode.Unauthorized),
            Case(TestRole.StudentA, HttpStatusCode.Forbidden),
            Case(TestRole.ParentLinked, HttpStatusCode.Forbidden),
            Case(TestRole.Editor, HttpStatusCode.Forbidden),
            Case(TestRole.Finance, HttpStatusCode.Forbidden),
            Case(TestRole.Admin, HttpStatusCode.OK));
    }

    [SkippableFact] // IT-AUTHZ — POST /api/sepay/ipn (chỉ qua Apikey, không qua JWT)
    public async Task Authz_POST_sepay_ipn_rejects_jwt_and_anonymous()
    {
        RequireDocker();
        Task<HttpResponseMessage> Call(HttpClient c) => c.PostAsJsonAsync("/api/sepay/ipn", SePayIpn.In(1, 199_000, "REF-" + Rand()));

        await Matrix(Call,
            Anon(HttpStatusCode.Unauthorized),
            Case(TestRole.StudentA, HttpStatusCode.Unauthorized),
            Case(TestRole.ParentLinked, HttpStatusCode.Unauthorized),
            Case(TestRole.Editor, HttpStatusCode.Unauthorized),
            Case(TestRole.Finance, HttpStatusCode.Unauthorized),
            Case(TestRole.Admin, HttpStatusCode.Unauthorized));
    }
}
