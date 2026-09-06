# Danh mục Integration Test — ToanHocHay (bản gốc)

> **Đây là tài liệu gốc về integration test của hệ thống.** Mọi case được liệt kê **đầy đủ**
> tại đây, theo từng luồng rõ ràng. Đọc file này là đủ để biết toàn bộ integration test cần có.
>
> ⚠️ **Hạ tầng test cũ (`Tests/Infrastructure/*` và mọi file `Tests/*.cs`) sẽ bị xoá và viết
> lại từ đầu.** [§2](#2-hạ-tầng-test-xây-mới-hoàn-toàn) là đặc tả để dựng lại. Không có gì
> được coi là "đã xong".
>
> Unit test: [DANH-MUC-UNIT-TEST.md](DANH-MUC-UNIT-TEST.md) · Luồng nghiệp vụ: [KE-HOACH-KIEM-THU-HE-THONG.md](KE-HOACH-KIEM-THU-HE-THONG.md) · System test: [DANH-MUC-SYSTEM-TEST.md](DANH-MUC-SYSTEM-TEST.md).
>
> **P** (ưu tiên): P1 bắt buộc · P2 nên có · P3 khi rảnh.
> **Prior art**: cột này trỏ tới file test **cũ (sắp bị xoá)** có logic assert để tham khảo khi
> viết lại — ✅ có nhiều để port · 🟡 có một phần · 🔲 viết mới hoàn toàn.

---

## Mục lục

- [1. Định nghĩa & phạm vi](#1-định-nghĩa--phạm-vi)
- [2. Hạ tầng test (xây mới hoàn toàn)](#2-hạ-tầng-test-xây-mới-hoàn-toàn)
- [3. Quy ước](#3-quy-ước)
- [4. Danh mục theo luồng](#4-danh-mục-theo-luồng)
  - [F1 — Xác thực & tài khoản](#f1--xác-thực--tài-khoản-it-f1)
  - [F2 — Danh mục & ghi danh](#f2--danh-mục--ghi-danh-it-f2)
  - [F3 — Học bài (bài giảng)](#f3--học-bài-bài-giảng-it-f3)
  - [F4 — Làm bài tập / kiểm tra](#f4--làm-bài-tập--kiểm-tra-it-f4)
  - [F5 — Tiến độ & Dashboard học sinh](#f5--tiến-độ--dashboard-học-sinh-it-f5)
  - [F6 — Dashboard & liên kết phụ huynh](#f6--dashboard--liên-kết-phụ-huynh-it-f6)
  - [F7 — Thanh toán (SePay VA + IPN)](#f7--thanh-toán-sepay-va--ipn-it-f7)
  - [F8 — Hoàn tiền (bán tự động)](#f8--hoàn-tiền-bán-tự-động-it-f8)
  - [F9 — Trợ giúp AI & Chatbot](#f9--trợ-giúp-ai--chatbot-it-f9)
  - [F10 — Thông báo](#f10--thông-báo-it-f10)
  - [F11 — Soạn nội dung (authoring)](#f11--soạn-nội-dung-authoring-it-f11)
  - [F12 — Hợp đồng API & Vận hành](#f12--hợp-đồng-api--vận-hành-it-f12)
- [5. Ma trận phân quyền (data-driven)](#5-ma-trận-phân-quyền-data-driven)
- [6. Prior art trong code test cũ](#6-prior-art-trong-code-test-cũ)
- [7. Bố cục thư mục](#7-bố-cục-thư-mục)
- [8. Lộ trình triển khai](#8-lộ-trình-triển-khai)
- [9. Trạng thái hiện tại](#9-trạng-thái-hiện-tại)

---

## 1. Định nghĩa & phạm vi

**Integration test** = boot **API thật** (`WebApplicationFactory<Program>`) trên **PostgreSQL thật**
(Testcontainers), gọi qua **HTTP**, đi hết chuỗi controller → service → EF → DB. Khẳng định:

- HTTP status code đúng ngữ nghĩa (200/201/400/401/403/404/409/429).
- Hình dạng response (envelope `ApiResponse`, enum-string, phân trang).
- **Phân quyền thật** (JWT, `[Authorize]` fallback, ownership, role).
- **Transaction / index / concurrency** (idempotency IPN, row-lock chấm bài, unique constraint).
- Hiệu ứng phụ đúng ở DB (đọc lại trực tiếp).

**Ngoài phạm vi** (để cho unit / system): công thức thuần (→ unit), UI WebApp (→ system),
tải/hiệu năng (→ system), tích hợp Flask AI / SePay / email thật (→ system, ở đây dùng test-double).

---

## 2. Hạ tầng test (xây mới hoàn toàn)

> Hạ tầng cũ bị xoá sạch. Phần này đặc tả **toàn bộ** thứ cần dựng lại. Thứ tự xây: §2.1 → §2.9.

### 2.1 Dự án test

- Project mới `ELearning_ToanHocHay.Tests` (xUnit), `net8.0`, `<Nullable>enable</Nullable>`,
  `<IsTestProject>true</IsTestProject>`, `<IsPackable>false</IsPackable>`, `ProjectReference`
  → `ELearning_ToanHocHay_Control.csproj`.
- `Program` đã là `public class Program` → `WebApplicationFactory<Program>` dùng được. Nếu không,
  thêm `public partial class Program { }` cuối `Program.cs`.
- Gói NuGet:

```xml
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
<PackageReference Include="xunit" Version="2.9.2" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
<PackageReference Include="Xunit.SkippableFact" Version="1.5.23" />
<PackageReference Include="FluentAssertions" Version="6.12.1" />
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.*" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Relational" Version="8.0.*" />
<PackageReference Include="Testcontainers.PostgreSql" Version="3.10.0" />
<PackageReference Include="NSubstitute" Version="5.1.0" />
<PackageReference Include="coverlet.collector" Version="6.0.2" />
```

- `[assembly: CollectionBehavior(DisableTestParallelization = false)]` — vẫn cho phép song song
  **giữa các collection**, nhưng integration nằm chung 1 collection nên chạy tuần tự (§2.8).

### 2.2 `PostgresFixture` — container Postgres dùng chung

`Tests/Integration/Infrastructure/PostgresFixture.cs`:

- `IAsyncLifetime`. Một `PostgreSqlContainer` (`postgres:16-alpine`) cho **cả** collection integration.
- `InitializeAsync`:
  ```csharp
  try { await _container.StartAsync(); DockerAvailable = true; }
  catch { DockerAvailable = false; }   // máy không có Docker → test tự skip, CI không đỏ
  ```
- Phơi: `string ConnectionString => _container.GetConnectionString();`, `bool DockerAvailable`.
- `DisposeAsync`: `await _container.DisposeAsync();`

### 2.3 `ApiFactory : WebApplicationFactory<Program>`

`.../Infrastructure/ApiFactory.cs`. Nhận `PostgresFixture` (ctor). Là `ICollectionFixture` (§2.8).

**Biến môi trường set trong `ApiFactory` trước khi host build** — Program.cs đọc các key này
(xác nhận: `ConnectionStrings:MyCnn`, `JwtSettings__SecretKey`, `APP_BASE_URL`, section `SePay`,
`RateLimiting:*`, `Seed:DemoData`):

| Env | Giá trị test | Vì sao |
|---|---|---|
| `DATABASE_URL` | *(xoá — set null)* | ép Program dùng `ConnectionStrings:MyCnn` thay vì URL Railway |
| `ConnectionStrings__MyCnn` | `fixture.ConnectionString` | trỏ container |
| `JwtSettings__SecretKey` | hằng ≥ 32 ký tự (vd `"integration-test-signing-key-0123456789abc"`) | Program fail-fast nếu thiếu / < 32 |
| `JwtSettings__Issuer` / `__Audience` | `"it-issuer"` / `"it-audience"` | validation params khớp |
| `JwtSettings__ExpirationMinutes` | `"60"` | token sống đủ 1 lần chạy |
| `APP_BASE_URL` | `"https://webapp.test"` | kiểm link trong email xác nhận / reset |
| `SePay__ApiKeyValidator` | `"test-sepay-key"` | header `Authorization: Apikey <key>` của IPN |
| `SePay__LifecycleIntervalMinutes` | `"0"` | tắt hosted timer — test tự gọi `POST /api/finance/subscriptions/run-lifecycle` |
| `SePay__PendingTimeoutMinutes` | `"30"` (mặc định; test override qua factory con) | |
| `SePay__AmountToleranceVnd` | `"0"` (test overpay tự nâng) | |
| `RateLimiting__AuthPermitLimit` / `__AiPermitLimit` / `__RefundPermitLimit` | `"100000"` | tắt rate-limit **mặc định**; test rate-limit dùng `WithWebHostBuilder` hạ xuống |
| `RateLimiting__TrustedProxies__0` | `"127.0.0.1"` | test phân vùng theo `X-Client-Key` |
| `Seed__DemoData` | `"false"` | không chạy `DemoDataSeeder`; integration dùng `SeedData` riêng |
| `ASPNETCORE_ENVIRONMENT` | `"Production"` | tắt redirect HTTPS 307 (POST không bị chuyển hướng) + tắt demo seeder |

**`ConfigureWebHost(IWebHostBuilder builder)`:**
- `builder.UseEnvironment("Production");`
- `builder.ConfigureTestServices(services => { ... })` — thay dependency **ngoài hệ**:
  - `IAIService` → `FakeAiService` (singleton; §2.7-bên-dưới không, xem `FakeAiService` §2.x).
  - `IEmailService` **và** `IBackgroundEmailService` → `FakeEmailSink` (singleton, list in-memory).
  - *(tuỳ chọn)* `TimeProvider` → `FakeClock` nếu đã refactor theo `TimeProvider`.
  - **Không** thay `AppDbContext` — Postgres thật là chủ đích.

**Migrate + seed** (chạy 1 lần, trong `IAsyncLifetime.InitializeAsync` của `ApiFactory`):
```csharp
using var scope = Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
await db.Database.MigrateAsync();          // Program.Main cũng gọi — idempotent
Ids = await SeedData.EnsureAsync(db);      // §2.5
```

**API tiện dụng trên `ApiFactory`:**

```csharp
public SeededIds Ids { get; private set; }
public bool DockerAvailable => _fixture.DockerAvailable;
public FakeAiService Ai   => Services.GetRequiredService<FakeAiService>();
public FakeEmailSink Email => Services.GetRequiredService<FakeEmailSink>();

public HttpClient Anonymous() => CreateClient();
public HttpClient As(int userId) {
    var c = CreateClient();
    c.DefaultRequestHeaders.Authorization = new("Bearer", MintToken(userId));
    return c;
}
public HttpClient AsRole(TestRole role) => As(Ids.UserId(role));   // TestRole: StudentA, StudentB, ParentLinked, ParentStranger, Editor, Reviewer, Finance, Support, Admin

public string MintToken(int userId) {
    using var scope = Services.CreateScope();
    var db  = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var jwt = scope.ServiceProvider.GetRequiredService<IJwtService>();
    var u = db.Users.Single(x => x.UserId == userId);
    int? sid = db.Students.Where(s => s.UserId == userId).Select(s => (int?)s.StudentId).FirstOrDefault();
    int? pid = db.Parents .Where(p => p.UserId == userId).Select(p => (int?)p.ParentId ).FirstOrDefault();
    return jwt.GenerateToken(u, sid, pid);
}

public async Task<T> Db<T>(Func<AppDbContext, Task<T>> read) {
    using var scope = Services.CreateScope();
    return await read(scope.ServiceProvider.GetRequiredService<AppDbContext>());
}
public Task Db(Func<AppDbContext, Task> act) => Db(async db => { await act(db); return 0; });
```

### 2.4 `FakeAiService`, `FakeEmailSink`, `FakeClock`

| Fake | Thay | Khả năng |
|---|---|---|
| `FakeAiService : IAIService` | Flask AI | `Healthy` (bool, mặc định `true`), `SetHealthy(bool)`; `NextHint` / `NextFeedback` (nội dung định sẵn); `ThrowTimeout` (mô phỏng treo → ném `TaskCanceledException`); đếm số lần gọi mỗi method. `IsHealthyAsync()` trả `Healthy`. |
| `FakeEmailSink : IEmailService, IBackgroundEmailService` | SendGrid | list `Sent` gồm `(to, subject, body, kind)`; `LastLink(kind)` — trích URL đầu tiên trong body (kiểm link xác nhận / reset). Mọi `Queue*` add ngay vào list (đồng bộ). |
| `FakeClock : TimeProvider` | `DateTime.UtcNow` | `Set(DateTime utc)`, `Advance(TimeSpan)`. Chỉ dùng nếu code đã inject `TimeProvider`; nếu chưa, test tua thời gian bằng seed `CreatedAt`/`EndDate` lùi. |

### 2.5 `SeedData` — bộ dữ liệu nền (golden dataset, tối thiểu & ổn định)

`.../Infrastructure/SeedData.cs`. `EnsureAsync(AppDbContext)` **idempotent** (no-op nếu
`admin@it.test` đã tồn tại). Mọi dữ liệu khác test tự dựng qua `FlowSeed` — golden dataset
giữ nhỏ để chạy nhanh và ít giòn.

| Nhóm | Bản ghi tạo |
|---|---|
| Users — mật khẩu BCrypt `"Test!234"`, `IsEmailConfirmed=true`, `IsActive=true` | `student.a@it.test`, `student.b@it.test`, `parent.linked@it.test`, `parent.stranger@it.test`, `editor@it.test` (ContentEditor), `reviewer@it.test` (AcademicReviewer), `finance@it.test` (FinanceManager), `support@it.test` (SupportStaff), `admin@it.test` (SystemAdmin) |
| Student | A, B — `CurrentGradeLevelId` = G6 |
| Parent | linked (`ConnectionCode="LINKAAAA"`), stranger (`"LINKBBBB"`) |
| `ParentLink` | linked ↔ A: `Status=Active`, `Relationship=Father`, `IsPrimaryGuardian=true` |
| Catalog | Subject `MATH` + GradeLevel `G6` (dùng của InitialCreate seed nếu có) + 1 Framework `FW-IT` |
| `QuestionBank` + 4 `Question` (`Status=Approved`, `IsActive=true`) | MC "2 + 2 = ?" (2 option, đúng = "4"); TF "3 là số lẻ?" (`CorrectAnswer="true"` + 2 option); FillBlank "1/2 = ?" (`CorrectAnswer="1/2"`); Essay "Vì sao 0 là số chẵn?" |
| `Exercise` (`Status=Published`) | `IT Quiz` (`ExerciseType=Quiz`, 4 câu, `TotalScores=4`, `PassingScore=2`); `IT One-shot` (`ExerciseType=Test`, `MaxAttempts=1`, 1 câu) |
| `ExerciseAttempt` | A `InProgress` trên `IT Quiz`, `PlannedEndTime = now + 30′` |
| `Package` + `PackageEntitlement` | `IT Standard` `Tier=Standard` `Price=199000` `DurationDays=30`; entitlement `ScopeType=SubjectGrade` (MATH, G6) |
| `Payment` | A `Pending` 199k (gắn subscription Pending); A `Completed` 199k `TransactionId="SEED-DONE-A"` **không** subscription (để test hoàn tiền, không ảnh hưởng tier) |
| `Subscription` | A `Pending` gắn `Payment` Pending, `AmountPaid=199000` |

**`SeededIds`** (record) — mọi id + `int UserId(TestRole role)`:
`StudentAUserId, StudentAId, StudentBUserId, StudentBId, ParentLinkedUserId, ParentStrangerUserId,
EditorUserId, ReviewerUserId, FinanceUserId, SupportUserId, AdminUserId, BankId, McQuestionId,
McCorrectOptionId, TfQuestionId, TfTrueOptionId, FillBlankQuestionId, EssayQuestionId,
QuizExerciseId, OneShotExerciseId, InProgressAttemptId, PackageId, PendingPaymentId,
CompletedPaymentId, PendingSubscriptionId`.

### 2.6 `FlowSeed` — builder theo luồng (runtime, cô lập tuyệt đối)

`.../Infrastructure/FlowSeed.cs`. Nhận `ApiFactory`. **Không** đụng golden dataset. Mỗi hàm
`Guid`-hoá tên/email, trả về id/record.

```csharp
Task<CourseSeed>  PublishCourseAsync(string? slug=null, int freeChapters=1, int paidChapters=1, bool showcaseLesson=false);
Task              EnrolAsync(int studentUserId, int courseId);
Task              GrantEntitlementAsync(int studentId, EntitlementScope scope, int? subjectId=null, int? gradeId=null, DateTime? expiresAt=null);
Task<int>         StartAttemptAsync(int studentUserId, int exerciseId);
Task              SaveAnswerAsync(int studentUserId, int attemptId, int questionId, int? optionId=null, string? text=null);
Task<AttemptResultSeed> SubmitAttemptAsync(int studentUserId, int attemptId);
Task<(int subId,long amount)> CreatePendingSubscriptionAsync(int studentUserId, int packageId);
Task              ActivateSubscriptionViaIpnAsync(int subId, long amount, string? reference=null);   // gọi POST /api/sepay/ipn thật
Task<int>         SeedRefundablePaymentAsync(int payerUserId, decimal amount=199_000m, DateTime? paidAt=null);
Task<int>         CreateRefundRequestAsync(int actorUserId, int paymentId, decimal? amount=null);
Task<int>         SeedNotificationAsync(int userId, string ruleKey);
Task<(int userId,string email,string password)> NewConfirmedUserAsync(UserType type);
Task<(int userId,string email,string token)>    NewUnconfirmedUserAsync(UserType type);   // token = EmailVerificationToken để confirm
```

`CourseSeed` = `(int CourseId, int VersionId, int[] FreeChapterIds, int[] PaidChapterIds, int FirstPaidLessonId, int? ExerciseOnLessonId)`.

### 2.7 `SePayIpn` — builder payload IPN

```csharp
static class SePayIpn {
    static object In(int subId, long amount, string reference) => new {
        id = Random.Shared.Next(1, int.MaxValue),
        content = $"TKPTTS SUBSCRIPTION_{subId}",
        transferType = "in", transferAmount = amount, referenceCode = reference
    };
    static object Out(long amount, string reference)  => /* transferType = "out" */;
    static object BadContent(long amount, string reference) => /* content không có SUBSCRIPTION_x */;
    static HttpClient Client(ApiFactory f, string key = "test-sepay-key") {
        var c = f.CreateClient();
        c.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Apikey {key}");
        return c;
    }
}
```

### 2.8 Collection + base class

```csharp
[CollectionDefinition(Name)]
public class IntegrationCollection
    : ICollectionFixture<PostgresFixture>, ICollectionFixture<ApiFactory>
{ public const string Name = "integration"; }

public abstract class IntegrationTest
{
    protected readonly ApiFactory App;
    protected SeededIds Ids => App.Ids;
    protected FlowSeed  Flow => new(App);
    protected IntegrationTest(ApiFactory app) => App = app;
    protected void RequireDocker() => Skip.IfNot(App.DockerAvailable, "Docker không khả dụng — bỏ qua integration test.");
}
```

- Mọi class integration: `[Collection(IntegrationCollection.Name)] : IntegrationTest`.
- **Chạy tuần tự** trong collection này (container + env-var wiring dùng chung, không an toàn song song).
- Dòng đầu mỗi `[SkippableFact]`: `RequireDocker();`.

### 2.9 Envelope assertions

`.../Infrastructure/Envelope.cs` — extension trên `HttpResponseMessage`, bọc kiểu `ApiResponse`
(`{ StatusCode, Message, Data }`):

```csharp
Task<JsonElement> RootAsync(this HttpResponseMessage res);
Task<JsonElement> DataAsync(this HttpResponseMessage res);                    // Root().GetProperty("Data")
Task ShouldBeOk(this HttpResponseMessage res);                               // 200 && có "Data"
Task ShouldBeCreated(this HttpResponseMessage res);                          // 201
Task ShouldBeError(this HttpResponseMessage res, HttpStatusCode code, string? messageContains = null);
Task<(int Total,int Page,int PageSize,JsonElement Items)> ShouldBePaged(this HttpResponseMessage res);
```

### 2.10 Chạy

```bash
dotnet test --filter "Level=Integration"                     # cần Docker; không có → skip toàn bộ
dotnet test --filter "Level=Integration&Flow=Payment"
dotnet test --filter "Level=Integration&Priority=P1"
```

CI: runner có Docker chạy đầy đủ; runner không Docker → integration self-skip (không fail build).

---

## 3. Quy ước

- **Class:** `IT_F{n}_{TenLuong}Tests : IntegrationTest` trong `namespace ELearning_ToanHocHay.Tests.Integration`.
- **Test:** `[SkippableFact]` tên `IT_F{n}_{NN}_{mo_ta}` khớp mã trong tài liệu này. Ví dụ `IT_F7_05_Wrong_amount_keeps_subscription_pending`.
- **Trait:** `[Trait("Level","Integration")]` (đặt ở class) + `[Trait("Flow","Payment")]` + `[Trait("Priority","P1")]`.
- **Khung:** `[Collection(IntegrationCollection.Name)]`, ctor `: base(app)`, dòng đầu mỗi test `RequireDocker();`.
- **Cô lập:** mỗi test tự dựng dữ liệu riêng qua `Flow.*` (`Guid`-hoá). Chỉ **đọc** golden dataset (`Ids`), không sửa.
- **Assert 2 lớp:** (1) HTTP status + envelope (`res.ShouldBe*`); (2) đọc lại DB `await App.Db(db => ...)` xác nhận hiệu ứng.

---

## 4. Danh mục theo luồng

> Cột **Prior art** trỏ tới file test **cũ sắp bị xoá** — chỉ để tham khảo logic assert khi
> viết lại: ✅ có nhiều để port · 🟡 một phần · 🔲 viết mới hoàn toàn. Không hàng nào "đã xong".
> "Tiền đề" viết theo API hạ tầng mới ở [§2](#2-hạ-tầng-test-xây-mới-hoàn-toàn) (`Ids.*`, `Flow.*`,
> `App.As(...)`, `App.Db(...)`).

### F1 — Xác thực & tài khoản (`IT-F1`)

| Mã | Method + Route | Tiền đề | Kỳ vọng | Prior art | P |
|---|---|---|---|---|---|
| IT-F1-01 | `POST /api/auth/register` | email mới, `UserType=Student` | `200`; DB có `User(IsEmailConfirmed=false)` + `Student` + `EmailVerificationToken`; **không** trả token đăng nhập | 🔲 | P1 |
| IT-F1-02 | `POST /api/auth/register` | email đã tồn tại (đã xác nhận) | `400`/`409` "Email đã được đăng ký" | 🔲 | P1 |
| IT-F1-03 | `POST /api/auth/register` | `UserType=SystemAdmin` | `400` "Không cho phép đăng ký role này"; DB không tạo user | 🔲 | P1 |
| IT-F1-04 | `GET /api/auth/confirm-email?token=` | token hợp lệ vừa tạo | `200`; DB `User.IsEmailConfirmed=true` | 🔲 | P1 |
| IT-F1-05 | `GET /api/auth/confirm-email?token=` | token sai / đã dùng / hết hạn | `400` envelope "Liên kết không hợp lệ" | 🔲 | P1 |
| IT-F1-06 | `POST /api/auth/resend-confirmation` | email không tồn tại | `200` mờ ("Nếu email tồn tại…"); không gửi mail | 🔲 | P2 |
| IT-F1-07 | `POST /api/auth/resend-confirmation` | email chưa xác nhận | `200`; token cũ vô hiệu, token mới trong DB | 🔲 | P2 |
| IT-F1-08 | `POST /api/auth/login` | email + mật khẩu đúng, đã xác nhận | `200`; `Data.Token` + `Data.RefreshToken` + `Data.PackageTier` | ✅ `P1AuthTests::Login_issues_a_refresh_token...` | P1 |
| IT-F1-09 | `POST /api/auth/login` | mật khẩu sai | `400`/`401` "Email hoặc mật khẩu không đúng"; DB `FailedLoginCount` +1 | 🔲 | P1 |
| IT-F1-10 | `POST /api/auth/login` | email chưa xác nhận | `400` "Vui lòng xác nhận email trước khi đăng nhập" | 🔲 | P1 |
| IT-F1-11 | `POST /api/auth/login` × 5 sai liên tiếp | cùng tài khoản | lần 6 → `400` "Tài khoản tạm khoá … Thử lại sau"; DB `LockoutEndsAt` set | ✅ `P1AuthTests::Five_failed_logins_lock...` | P1 |
| IT-F1-12 | `POST /api/auth/login` | sau khi `LockoutEndsAt` đã qua | đăng nhập lại được, counter reset | 🟡 `P1AuthTests` (phần này mỏng) | P2 |
| IT-F1-13 | `POST /api/auth/refresh-token` | refresh token hợp lệ | `200`; cặp mới; DB token cũ `RevokedAt` + `ReplacedByTokenHash` | ✅ `P1AuthTests` | P1 |
| IT-F1-14 | `POST /api/auth/refresh-token` | refresh token **đã bị xoay** (dùng lại) | `400`/`401`; DB **mọi** refresh token của user bị revoke (reuse detection) | 🔲 | P1 |
| IT-F1-15 | `POST /api/auth/refresh-token` | chuỗi rác | `401`, không `500` | 🔲 | P2 |
| IT-F1-16 | `POST /api/auth/change-password` | `currentPassword` đúng | `200`; DB `PasswordHash` đổi + `SecurityStamp` đổi; mọi refresh token revoke | ✅ `P1AuthTests::Change_password_revokes...` | P1 |
| IT-F1-17 | dùng access token **cũ** sau đổi mật khẩu | gọi `GET /api/auth/me` | `401` (SecurityStamp mismatch) | ✅ `P7Tests::Locking_a_user_invalidates...` (tương tự) | P1 |
| IT-F1-18 | `POST /api/auth/forgot-password` | email tồn tại đã xác nhận | `200`; DB `PasswordResetToken` mới (1h) | ✅ `P1AuthTests::Forgot_password_is_always_ok...` | P1 |
| IT-F1-19 | `POST /api/auth/forgot-password` | email không tồn tại | `200` **cùng** message (không lộ tồn tại); DB không tạo token | ✅ `P1AuthTests` | P1 |
| IT-F1-20 | `POST /api/auth/reset-password` | token hợp lệ + mật khẩu mới | `200`; đăng nhập bằng mật khẩu mới OK; token cũ 1 lần → lần 2 `400` | ✅ `P1AuthTests` | P1 |
| IT-F1-21 | `POST /api/auth/logout` (có refresh token) | đã đăng nhập | `200`; DB chỉ token đó revoke | 🔲 | P2 |
| IT-F1-22 | `GET /api/auth/me` | Student A | `200`; `Data.Email`, `Data.UserType="Student"`, `Data.StudentId` | ✅ `A1AuthorizationMatrixTests::A1_11_Me...` | P2 |
| IT-F1-23 | `GET /api/auth/me` | ẩn danh | `401` (không `302`) | ✅ rải rác | P1 |
| IT-F1-24 | `POST /api/admin/users/{id}/lock` rồi `POST /api/auth/login` | admin khoá user | login → `400` "vô hiệu hóa"; sau `unlock` → login OK | ✅ `P1AuthTests::Admin_lock_blocks_login...` | P1 |
| IT-F1-25 | `POST /api/admin/users/{id}/role` | actor = admin | `200`; DB `AuditLog` có dòng; actor = student → `403` | ✅ `P1AuthTests::Role_change_is_audited...` | P1 |
| IT-F1-26 | `POST /api/auth/login` × spam từ client A (header `X-Client-Key`) | `RateLimiting__AuthPermitLimit` đặt thấp cho test này | client A → `429` **có** vỏ `ApiResponse` tiếng Việt; client B (`X-Client-Key` khác) **không** bị chặn | 🔲 | P2 |
| IT-F1-27 | `POST /api/auth/validate-token` | token hợp lệ / hết hạn / rác | `200 true` / `200 false` / `200 false` | 🔲 | P3 |

### F2 — Danh mục & ghi danh (`IT-F2`)

| Mã | Method + Route | Tiền đề | Kỳ vọng | Prior art | P |
|---|---|---|---|---|---|
| IT-F2-01 | `GET /api/catalog/subjects` | — | `200` public | ✅ `A3ContentLayerTests` | P1 |
| IT-F2-02 | `GET /api/catalog/grade-levels` · `/frameworks` | — | `200` public | 🔲 | P2 |
| IT-F2-03 | `POST /api/catalog/subjects` | caller = Student | `403` | ✅ `A3ContentLayerTests` | P1 |
| IT-F2-04 | `GET /api/courses` · `GET /api/courses/{id}` · `GET /api/courses/by-slug/{slug}` | có khoá Published | `200` public; slug sai → `404` envelope | 🟡 | P2 |
| IT-F2-05 | `GET /api/learn/courses/{id}/content` | khoá Published, chương 1 free, bài 1.1 trả phí; **ẩn danh** | `200`; `Tree` chỉ có chương free; `Children` của bài trả phí bị cắt (`length=0`) | ✅ `A3ContentLayerTests::Non_entitled_viewers...` | P1 |
| IT-F2-06 | `GET /api/learn/nodes/{lessonId}` | bài trả phí; Student B chưa quyền | `403` | ✅ `A3ContentLayerTests` | P1 |
| IT-F2-07 | `POST /api/enrollments/courses/{courseId}` | Student A, khoá free Published | `200`; DB `StudentCourse`; `GET /api/enrollments/me` liệt kê | ✅ `A3ContentLayerTests::Enrolled_student...` | P1 |
| IT-F2-08 | `GET /api/learn/courses/{id}/content` | Student A đã ghi danh | `200`; `Data.AccessLevel="Full"`; mở được `GET /api/learn/nodes/{paidLessonId}` → `200` | ✅ `A3ContentLayerTests` | P1 |
| IT-F2-09 | `GET /api/learn/nodes/{paidLessonId}` | Student có `PackageEntitlement(SubjectGrade, MATH, G6)` còn hạn, **không** ghi danh | `200` (tầng entitlement theo môn-lớp) | 🔲 | P1 |
| IT-F2-10 | `GET /api/learn/nodes/{paidLessonId}` | Student có entitlement **môn khác** | `403` | 🔲 | P2 |
| IT-F2-11 | `POST /api/enrollments/courses/{courseId}` | khoá `Draft` (chưa publish) | `400`/`404` "chưa được xuất bản" | 🔲 | P2 |
| IT-F2-12 | `POST /api/enrollments/courses/{courseId}` × 2 | cùng student, cùng khoá | lần 2 `200` "Already enrolled"; DB **không** thêm bản ghi | 🔲 | P2 |
| IT-F2-13 | `POST /api/courses` · `GET /api/question-banks` | caller = Student | `403` cả hai | ✅ `A3ContentLayerTests` | P1 |

### F3 — Học bài (bài giảng) (`IT-F3`)

| Mã | Method + Route | Tiền đề | Kỳ vọng | Prior art | P |
|---|---|---|---|---|---|
| IT-F3-01 | `GET /api/learn/nodes/{id}` | bài "showcase" đủ 12 loại block, student có quyền | `200`; payload có mảng blocks/resources/flashcards đủ | 🔲 | P2 |
| IT-F3-02 | `GET /api/learn/nodes/{id}` | bài khoá, student Free | `403` + envelope (thông điệp gợi ý nâng gói) | 🟡 (403 có, nội dung chưa assert) | P1 |
| IT-F3-03 | `POST /api/progress/lessons/{nodeId}/complete` | student ghi danh, view time ≥ 20s | `200`; DB `NodeProgress` bài = `100%` | ✅ `P4ProgressTests::Mark_lesson_complete_needs_view_time...` | P1 |
| IT-F3-04 | `POST /api/progress/lessons/{nodeId}/complete` | view time < 20s | không set `100%` / trả lỗi rõ | 🟡 | P2 |
| IT-F3-05 | sau khi hoàn thành bài | đọc DB | roll-up `MaterializedPath` lên chương + version (dùng `StartsWith`, path gồm chính node) | ✅ `P4ProgressTests::Submitting_an_attempt_writes_NodeProgress...` | P1 |
| IT-F3-06 | `GET /api/progress/versions/{courseVersionId}` | student có tiến độ | `200`; % theo cây khớp DB | 🔲 | P2 |
| IT-F3-07 | `POST /api/progress/lessons/{nodeId}/complete` | bài **không** thuộc khoá student ghi danh | `403` | 🔲 | P2 |
| IT-F3-08 | `GET /api/progress/students/{studentId}/heatmap` | Student B đọc của Student A | `403` (owner guard) | ✅ `P4ProgressTests::Heatmap_is_owner_guarded` | P1 |
| IT-F3-09 | `GET /api/progress/students/{studentId}/heatmap` | chủ sở hữu, có `DailyActivitySnapshot` | `200`; chuỗi ngày + streak khớp | 🟡 | P2 |

### F4 — Làm bài tập / kiểm tra (`IT-F4`)

| Mã | Method + Route | Tiền đề | Kỳ vọng | Prior art | P |
|---|---|---|---|---|---|
| IT-F4-01 | `POST /api/exercise-attempts/start` | exercise Quiz Published, student đủ quyền | `200`; DB `ExerciseAttempt(InProgress)` + `PlannedEndTime` | ✅ helper `A2BusinessLogicTests` | P1 |
| IT-F4-02 | `POST /api/exercise-attempts/start` | exercise Test/Exam cần gói, student **Free** | `403` (không phải `400`) | 🟡 gián tiếp | P1 |
| IT-F4-03 | `POST /api/exercise-attempts/start` | exercise Exam, student có gói Premium | `200` | 🔲 | P2 |
| IT-F4-04 | `POST /api/exercise-attempts/start-random` (`DurationMinutes=null` và `=15`) | ngân hàng có câu Approved | `200`; lưu & complete OK; **không** "timeout ảo" | ✅ `A2BusinessLogicTests::A2_03_RandomExercise...` | P1 |
| IT-F4-05 | `POST /api/exercise-attempts/save-answer` × 2 cùng câu | attempt InProgress | `200` cả 2; DB 1 bản ghi `StudentAnswer` (ghi đè) | 🔲 | P1 |
| IT-F4-06 | `POST /api/exercise-attempts/save-answer` | attempt của **người khác** | `403` | ✅ `A1AuthorizationMatrixTests::A1_02_OtherStudent...` | P1 |
| IT-F4-07 | `POST /api/exercise-attempts/save-answer` | ẩn danh | `401` | ✅ `A1AuthorizationMatrixTests` | P1 |
| IT-F4-08 | `POST /api/exercise-attempts/complete` | có 1 câu MC trả lời sai | `200` trong < 10s; response **không** có `FullSolution` (AI điền sau); job feedback được đẩy | ✅ `A2BusinessLogicTests::A2_04_Complete_returns_fast...` | P1 |
| IT-F4-09 | `POST /api/exercise-attempts/complete` | trả lời đủ 4 loại: MC đúng, TF đúng, FillBlank "0.5" (đáp án "1/2"), Essay | `200`; `TotalScore=3`, `CorrectAnswers=3`, `WrongAnswers=0`, `HasPendingManualGrading=true`; Essay `NeedsManualGrading=true` | ✅ `A2BusinessLogicTests::A2_09_Grades_every_question_type` | P1 |
| IT-F4-10 | `POST /api/exercise-attempts/start` lần 2 | exercise `MaxAttempts=1`, đã complete lần 1 | `400` message chứa "attempt" | ✅ `A2BusinessLogicTests::A2_08_MaxAttempts...` | P1 |
| IT-F4-11 | `POST /api/exercise-attempts/complete` × 2 **song song** | cùng attempt | chấm **đúng 1 lần**; không `5xx`; `TotalScore` ổn định | ✅ `P3P4RemainingTests::Completing_an_attempt_twice_in_parallel...` | P1 |
| IT-F4-12 | `GET /api/exercise-attempts/{attemptId}/result` | chủ / student khác / phụ huynh liên kết / phụ huynh lạ | `200` / `403` / `200` / `403` | ✅ `A1AuthorizationMatrixTests` | P1 |
| IT-F4-13 | `GET /api/exercise-attempts/student/{studentId}/history` | cùng ma trận quyền như IT-F4-12 | `200`/`403`/`200`/`403` | ✅ `A1AuthorizationMatrixTests::A1_02_OwnerAndLinkedParent...` | P1 |
| IT-F4-14 | `GET /api/exercise-attempts/{attemptId}/feedback-status` | sau complete có câu sai | `200`; `TotalWrong > 0`; poll tới khi job xong → `/result` có `FullSolution` | 🟡 `A2_04` | P2 |
| IT-F4-15 | `POST /api/exercise-attempts/{attemptId}/report-tab-switch` × nhiều lần nhanh | attempt InProgress | debounce (không ghi trùng dồn dập) + trần email; DB `TabSwitchLog` | 🔲 | P2 |
| IT-F4-16 | `POST /api/exercise-attempts/complete` | sau `PlannedEndTime` | vẫn chấm (không mất bài); đánh dấu quá giờ nếu có field | 🔲 | P2 |
| IT-F4-17 | `POST /api/exercise-attempts/submit` · `/submit-answer` (route cũ) | — | `404` | ✅ `A2BusinessLogicTests::A2_07_Removed_submit...` | P3 |
| IT-F4-18 | `POST /api/exercise-attempts/start` × burst đồng thời | 1 student | không `5xx` | ✅ `ContractTests::Concurrent_exercise_starts...` | P2 |
| IT-F4-19 | `GET /api/ai-hints/by-attempt/{attemptId}` | student khác | `403` | ✅ `A1AuthorizationMatrixTests::A1_06_OtherStudent...` | P1 |

### F5 — Tiến độ & Dashboard học sinh (`IT-F5`)

| Mã | Method + Route | Tiền đề | Kỳ vọng | Prior art | P |
|---|---|---|---|---|---|
| IT-F5-01 | `GET /api/students/{id}/dashboard/overview` | Student A có ~5 attempt seed | `200`; số bài học / điểm TB / streak **khớp** dữ liệu seed (assert giá trị, không chỉ status) | 🟡 (chỉ test quyền + tier) | P1 |
| IT-F5-02 | `GET /api/students/{id}/dashboard/chapter-score-comparison` | attempt tuần này + tuần trước | `200`; `TrendDirection` đúng chiều (Up/Down/Flat) | ✅ `P3P4RemainingTests::Weekly_stats_compare...` | P1 |
| IT-F5-03 | `GET /api/students/{id}/dashboard/ai-assessment` · `ai-roadmap` | có `NodeProgress`/attempt yếu, `FakeAiService` bật | `200`; điểm yếu thật (không rỗng) | 🔲 (cần `FakeAiService`) | P2 |
| IT-F5-04 | `GET /api/students/{id}/dashboard/overview` | tier lấy từ `Package.Tier` (gói tên "Standard" nhưng `Tier=Premium`…) | dùng `Tier`, không `Contains("premium")` | ✅ `P4ProgressTests::Dashboard_tier_comes_from_Package_Tier...` | P1 |
| IT-F5-05 | `GET /api/students/{id}/dashboard/*` | Student **Free** gọi endpoint cần gói | `403` `upgradeRequired` (không `500`) | 🟡 | P1 |
| IT-F5-06 | `GET /api/students/{A}/dashboard/{overview\|chapter-score-comparison\|ai-assessment\|ai-roadmap}` | Student B | `403` **mọi** endpoint | ✅ `A1AuthorizationMatrixTests::A1_05...` | P1 |
| IT-F5-07 | `GET /api/students/{id}/dashboard-stats` | chủ sở hữu / người khác | `200` / `403`; hình dạng ổn định | 🔲 | P2 |
| IT-F5-08 | `GET /api/students/{id}/subscription/current` | student có sub Active / không | `200` trả gói / trả Free | 🔲 | P2 |
| IT-F5-09 | `GET /api/progress/students/{id}/heatmap` | 90 ngày `DailyActivitySnapshot` với ngày trống | `200`; streak dừng đúng ở ngày trống | 🟡 | P2 |

### F6 — Dashboard & liên kết phụ huynh (`IT-F6`)

| Mã | Method + Route | Tiền đề | Kỳ vọng | Prior art | P |
|---|---|---|---|---|---|
| IT-F6-01 | `POST /api/parents/{id}/invites` | phụ huynh mời con bằng email | `200`; DB có lời mời | 🔲 | P1 |
| IT-F6-02 | `POST /api/parents/link` | student nhập connection code hợp lệ | `200`; DB `ParentLink(Active)` | ✅ `P6Tests::Parent_links_a_child_by_code...` | P1 |
| IT-F6-03 | `POST /api/parents/link` | code sai | `400`/`404` | 🔲 | P2 |
| IT-F6-04 | `GET /api/parents/{id}/children` · `/children/overview` | phụ huynh có 1 con liên kết | `200`; liệt kê con + tiến độ tóm tắt | 🟡 (overview gián tiếp) | P1 |
| IT-F6-05 | `GET /api/exercise-attempts/student/{A}/history` + `/api/students/{A}/dashboard/overview` | phụ huynh **liên kết** với A | `200` cả hai | ✅ `A1AuthorizationMatrixTests` | P1 |
| IT-F6-06 | như IT-F6-05 | phụ huynh **không** liên kết | `403` cả hai | ✅ `A1AuthorizationMatrixTests` | P1 |
| IT-F6-07 | `DELETE /api/parents/{id}/children/{studentId}` | đang liên kết | `200`; ngay sau đó phụ huynh gọi dashboard/history của con → `403` | ✅ `P6Tests::...revoke_drops_dashboard_access` | P1 |
| IT-F6-08 | `PUT /api/parents/{otherId}` · `DELETE /api/parents/{otherId}` | phụ huynh khác / không phải admin | `403`; xoá cần `SystemAdmin` | 🔲 | P2 |
| IT-F6-09 | `POST /api/parents/link` (nhiều phụ huynh) | 1 student liên kết 2 phụ huynh, cờ `IsPrimaryGuardian`, `Relationship` | DB đúng; chỉ 1 primary | 🔲 | P2 |
| IT-F6-10 | `GET /api/students/{id}/parents` | (backend còn thiếu) | thêm endpoint rồi test: chủ sở hữu thấy phụ huynh liên kết | 🔴 chờ backend | P3 |

### F7 — Thanh toán (SePay VA + IPN) (`IT-F7`)

| Mã | Method + Route | Tiền đề | Kỳ vọng | Prior art | P |
|---|---|---|---|---|---|
| IT-F7-01 | `POST /api/subscriptions` `{StudentId, PackageId}` | Student A, package 199k | `200`; `Data.amount=199000`, `Data.qrUrl` chứa `amount=199000`; DB `Payment(Pending)` + `Subscription(Pending)` với `AmountPaid=199000` | ✅ `A2BusinessLogicTests::A2_02_CreateSubscription_uses_package_price` | P1 |
| IT-F7-02 | `POST /api/subscriptions` `{..., amount: 1}` | client cố ép giá | server bỏ qua, dùng `Package.Price` | ✅ `A2BusinessLogicTests::A2_02` | P1 |
| IT-F7-03 | `POST /api/sepay/ipn` (header `Authorization: Apikey test-sepay-key`) | IPN `in`, đúng tiền, `content="SUBSCRIPTION_{id}"` | `200` `outcome="Processed"`; DB `Subscription=Active`, `Payment=Completed`, `TransactionId=ref`, `EndDate-StartDate ≈ 30d` | ✅ `P5PaymentTests::Valid_IPN_activates...` | P1 |
| IT-F7-04 | `POST /api/sepay/ipn` × 2 cùng `referenceCode` | | lần 2 `200` `outcome="Duplicate"`; DB **1** dòng `SePayIpnLog` | ✅ `P5PaymentTests::Replaying_the_same_referenceCode...` | P1 |
| IT-F7-05 | `POST /api/sepay/ipn` | số tiền lệch (ngoài `AmountToleranceVnd`) | `200` `outcome="AmountMismatch"`; DB `Subscription` giữ `Pending` | ✅ `P5PaymentTests::Wrong_amount_does_not_activate` | P1 |
| IT-F7-06 | `POST /api/sepay/ipn` | ref mới nhưng subscription đã `Active` | `200` `outcome="Duplicate"` — không kích hoạt lại | 🔲 | P1 |
| IT-F7-07 | `POST /api/sepay/ipn` | `transferType="out"` / subscription không tồn tại | `200` `outcome="Ignored"` | ✅ `P5PaymentTests::Out_transfer_and_unknown...` | P1 |
| IT-F7-08 | `POST /api/sepay/ipn` (header `Apikey wrong-key`) | | `401` | ✅ `P5PaymentTests::IPN_rejects_a_bad_api_key` | P1 |
| IT-F7-09 | 2× `POST /api/subscriptions` + 2× IPN | Student A mua gói thứ 2 khi đang có gói Active | sub mới `Active`, sub cũ tự `Expired` | ✅ `P5PaymentTests::Activating_a_second_subscription...` | P1 |
| IT-F7-10 | `POST /api/sepay/ipn` | overpay/underpay **trong** `AmountToleranceVnd` (đặt tolerance > 0 cho test) | `200` `Processed` — vẫn kích hoạt | 🔲 | P2 |
| IT-F7-11 | `POST /api/finance/subscriptions/run-lifecycle` | seed: 1 `Active` quá `EndDate`, 1 `Pending` quá `PendingTimeoutMinutes` | `200`; DB → `Expired` và `Cancelled` (+ Payment → `Failed`) | ✅ `P5PaymentTests::Lifecycle_sweep_expires...` | P1 |
| IT-F7-12 | `POST /api/sepay/ipn` × 2 cùng ref **song song** | | unique index serialize; cuối cùng đúng 1 `Processed`, không `Subscription` kích hoạt 2 lần | 🔲 | P2 |
| IT-F7-13 | `GET /api/subscriptions/me` · `GET /api/payments/me` | Student A | `200`; chỉ dữ liệu của mình | ✅ `P5PaymentTests` | P1 |
| IT-F7-14 | `PUT /api/subscriptions/cancel/{id}` | chủ sở hữu huỷ `Pending`/`Active` / người khác | `200` / `403` | 🔲 | P2 |
| IT-F7-15 | `GET /api/finance/subscriptions/reconciliation` | Finance / Student | `200` (`Balanced` khi không drift) / `403` | ✅ `P5PaymentTests::Reconciliation_and_my_endpoints...` | P1 |
| IT-F7-16 | `GET /api/payments` · `GET /api/subscriptions` | Finance (phân trang + `?status=`) / Student | `200` paged / `403` | ✅ `A1...` + `P7Tests::Subscription_list_is_paged...` | P1 |
| IT-F7-17 | `PATCH /api/subscriptions/{id}/status` · `PUT /api/payments/update-status/{id}` | Student / ẩn danh | `403` / `401` | ✅ `A1AuthorizationMatrixTests::A1_03...` | P1 |
| IT-F7-18 | `GET /api/packages` · `GET /api/packages/{id}` | ẩn danh | `200` (public); `POST /api/packages` bởi Student → `403` | ✅ `A1AuthorizationMatrixTests` | P1 |

### F8 — Hoàn tiền (bán tự động) (`IT-F8`)

| Mã | Method + Route | Tiền đề | Kỳ vọng | Prior art | P |
|---|---|---|---|---|---|
| IT-F8-01 | `POST /api/refunds` | Student, payment `Completed` của **chính mình** | `201`; DB `RefundRequest(PendingReview)` + `RefundEvent(Created)` | ✅ `RefundWorkflowTests::Student_creates_a_refund_request...` | P1 |
| IT-F8-02 | `POST /api/refunds` | payment của **người khác** | `403` (`CanAccessPaymentAsync`) | ✅ `RefundWorkflowTests::Student_cannot_refund_someone_elses...` | P1 |
| IT-F8-03 | `POST /api/refunds` | payment `Pending` / `amount` > số còn hoàn | `400` | ✅ `RefundWorkflowTests::Cannot_refund_a_pending_payment...` | P1 |
| IT-F8-04 | `POST /api/refunds` | payment cũ hơn `refund.maxPaymentAgeDays` | `400` | 🔲 | P2 |
| IT-F8-05 | `POST /api/refunds` × 2 cho cùng payment | yêu cầu 1 còn mở | lần 2 `409` | ✅ `RefundWorkflowTests::A_second_open_request...` | P1 |
| IT-F8-06 | `POST /api/refunds` × (max+1) | cùng beneficiary, 30 ngày | `409` sau khi chạm `refund.maxRequestsPerUserPer30d`; `SystemAdmin` tạo hộ → bỏ qua | ✅ `RefundWorkflowTests::Per_user_30_day_limit...` | P1 |
| IT-F8-07 | `POST /api/finance/refunds/{id}/approve` | Finance, dưới trần | `200` `Approved`; DB tiêu `refund.dailyCapVnd` | ✅ `RefundWorkflowTests::Approve_moves_to_Approved...` | P1 |
| IT-F8-08 | `POST /api/finance/refunds/{id}/approve` | tổng duyệt trong ngày vượt `dailyCapVnd` | `400` "Vượt trần" | ✅ `RefundWorkflowTests::Daily_cap_blocks...` | P1 |
| IT-F8-09 | `approve` lần 1 → `approve` lần 2 | `refund.dualControlThresholdVnd > 0`, `amount ≥ ngưỡng` | lần 1 → `PendingSecondApproval`; cùng người lần 2 → `409`; người Finance khác → `Approved` | ✅ `RefundWorkflowTests::Dual_control_needs_two_distinct_approvers` | P1 |
| IT-F8-10 | `reject` → `approve` | | `approve` sau reject → `409` (state machine) | ✅ `RefundWorkflowTests::Rejected_request_cannot_then_be_approved` | P1 |
| IT-F8-11 | full batch: `approve` → `POST /api/finance/refund-batches` → `GET .../export` → `mark-disbursed` → `confirm-all` | | request `Completed`; DB `Payment.Status=Refunded`, `RefundAmount` = full, `Subscription.Status=Cancelled`; đủ `RefundEvent` timeline + `AuditLog` | ✅ `RefundWorkflowTests::Full_batch_flow_completes...` | P1 |
| IT-F8-12 | hoàn **một phần** (`amount < payment.Amount`) | | `Payment.Status=PartiallyRefunded`; cho phép yêu cầu tiếp phần còn lại | ✅ `RefundWorkflowTests::Partial_refund_leaves...` | P1 |
| IT-F8-13 | `POST /api/finance/refund-batches/{id}/cancel` | lô có thành viên | request thành viên quay về `Approved` | ✅ `RefundWorkflowTests::Cancelling_a_batch...` | P2 |
| IT-F8-14 | `POST /api/finance/refunds/{id}/mark-failed` → `/retry` | | `Failed` → `retry` → `Approved` (kiểm lại trần/ngày), rời lô | 🔲 | P2 |
| IT-F8-15 | `POST /api/finance/refunds/{id}/confirm` | từ `Approved` (không qua lô), body `{BankTransactionRef}` bắt buộc | `200` → `Completed`; thiếu `BankTransactionRef` → `400` | 🔲 | P2 |
| IT-F8-16 | `/api/finance/refunds/*` | caller non-finance / ẩn danh gọi `/api/refunds` | `403` / `401` | ✅ `RefundWorkflowTests::Finance_endpoints_are_role_gated` + `A_non_finance_user...` | P1 |
| IT-F8-17 | `GET /api/finance/refund-batches/{id}/export` | tên chủ TK = `"=cmd|..."` | CSV trả file `text/csv`; ô tên có tiền tố `'` (chống formula-injection); `Draft → Exported` | ✅ `RefundWorkflowTests::Csv_export_neutralises_a_formula_injection` | P1 |
| IT-F8-18 | `GET /api/refunds/{id}` + DB dump | | API chỉ trả **4 số cuối** số TK; cột `BankAccountNumberProtected` trong DB là ciphertext | 🟡 | P1 |
| IT-F8-19 | `GET /api/refunds/me` · `GET /api/refunds/{id}` | chủ sở hữu / người lạ | chỉ thấy của mình / người lạ `403` | ✅ `RefundWorkflowTests::Owner_sees_their_request_in_me...` | P1 |
| IT-F8-20 | `GET /api/finance/refunds/daily-usage` · `/reconciliation` | Finance | `200`; `RemainingVnd = Cap - Used`; `CompletedRefundTotal == PaymentRefundedTotal` → `Balanced` | 🔲 | P2 |
| IT-F8-21 | `POST /api/payments/{id}/refund` (route cũ 1 bước) | | `404` (đã xoá) | 🔲 | P3 |

### F9 — Trợ giúp AI & Chatbot (`IT-F9`)

| Mã | Method + Route | Tiền đề | Kỳ vọng | Prior art | P |
|---|---|---|---|---|---|
| IT-F9-01 | `POST /api/ai-hints` × (limit+1) | Student Free, `AI:FreeDailyHintLimit` | request thứ (limit+1) → `429`; sang ngày mới (seed `AiUsageDaily` hôm qua) → cho lại | ✅ `P6Tests::Free_student_runs_out_of_AI_hints...` (reset mỏng) | P1 |
| IT-F9-02 | `POST /api/ai-hints` × nhiều | Student gói Premium/Yearly (unlimited) | không bao giờ `429` | ✅ `P6Tests::Unlimited_package_is_never_hint_rate_limited` | P1 |
| IT-F9-03 | `GET /api/ai-hints/quota` | Student đã dùng 2/3 | `200`; `used=2`, `limit=3`, `remaining=1` | 🔲 | P2 |
| IT-F9-04 | `POST /api/ai-hints` · `POST /api/ai-feedback` | attempt của người khác / ẩn danh | `403` / `401` | ✅ `A1AuthorizationMatrixTests::A1_06` | P1 |
| IT-F9-05 | `POST /api/exercise-attempts/complete` rồi `GET .../feedback-status` | có câu sai, `FakeAiService` bật | job feedback chạy nền, `/result` sau đó có `FullSolution` | 🟡 `A2_04` | P1 |
| IT-F9-06 | `POST /api/chatbot/message` `{text}` | `FakeAiService.SetHealthy(false)` (Flask down) | `200` fallback message; DB `ChatConversation` + `ChatMessage` vẫn được lưu | ✅ `P6Tests::Chatbot_persists_the_turn_even_when_the_AI_is_down` | P1 |
| IT-F9-07 | `GET /api/chatbot/health` | AI down / up | `503` / `200` | ✅ `P6Tests::Chatbot_health_is_503...` | P2 |
| IT-F9-08 | `POST /api/chatbot/request-human` (hoặc escalation) | hội thoại đang mở | chuyển sang `SupportStaff`; xuất hiện trong `GET /api/chatbot/staff/queue` | ✅ `RemainingFeaturesTests::Chat_escalation_moves...` | P2 |
| IT-F9-09 | `POST /api/chatbot/staff/conversations/{id}/assign` · `/reply` · `/close` | caller = SupportStaff / non-staff | `200` / `403` | 🔲 | P2 |
| IT-F9-10 | `POST /api/chatbot/message` | ẩn danh | `401` | ✅ `A1AuthorizationMatrixTests` | P1 |

### F10 — Thông báo (`IT-F10`)

| Mã | Method + Route | Tiền đề | Kỳ vọng | Prior art | P |
|---|---|---|---|---|---|
| IT-F10-01 | hoàn thành attempt điểm thấp | student có phụ huynh liên kết | DB `Notification` cho **cả** student và phụ huynh (rule `low-score`) | ✅ `P6Tests::Low_score_notifies_the_student_and_the_linked_parent` | P1 |
| IT-F10-02 | `POST /api/exercise-attempts/{id}/report-tab-switch` nhiều lần | attempt InProgress | rule `tab-switch` sinh `Notification` (sau ngưỡng) | 🔲 | P2 |
| IT-F10-03 | `POST /api/admin/notifications/run-inactivity-check` | student không hoạt động ≥ 3 ngày | `Notification` rule `inactivity` | 🟡 rà `P6`/`RemainingFeatures` | P2 |
| IT-F10-04 | `PUT /api/notifications/preferences` opt-out `low-score` | 1 user opt-out | user đó **không** nhận `low-score`; user khác vẫn nhận | ✅ `P6Tests::Opting_out_stops_that_rule_for_that_user_only` | P1 |
| IT-F10-05 | `GET /api/notifications` · `unread-count` · `POST /{id}/read` · `POST /read-all` | user có vài thông báo | `200`; đếm & đánh dấu đọc đúng; chỉ của mình | ✅ `P6Tests::Notification_endpoints_list_count_and_mark_read` | P1 |
| IT-F10-06 | `GET /api/notifications/preferences` | user | `200`; hình dạng ổn định | 🔲 | P2 |
| IT-F10-07 | `POST /api/notifications/{id}/read` | thông báo của người khác | `404`/`403` (không thấy) | 🔲 | P2 |

### F11 — Soạn nội dung (authoring) (`IT-F11`)

| Mã | Method + Route | Tiền đề | Kỳ vọng | Prior art | P |
|---|---|---|---|---|---|
| IT-F11-01 | `POST /api/catalog/{subjects\|grade-levels\|frameworks}` | Editor / Student | `200` / `403` | 🟡 (subjects có) | P1 |
| IT-F11-02 | `POST /api/courses` → `.../versions/{v}/submit` → `.../review {Approve}` → `.../publish` | Editor tạo, Admin review+publish | mỗi bước `200`; DB `CourseVersion.State=Published`, `Course.Status=Published` | ✅ `A3ContentLayerTests::Version_lifecycle_draft_to_published...` | P1 |
| IT-F11-03 | `.../versions/{v}/review {Reject}` | version đã submit | về `Draft`/`Rejected`; `publish` sau đó → `400` | 🔲 | P2 |
| IT-F11-04 | `POST /api/content/versions/{v}/nodes` | version đã `Published` | `400` message chứa "Draft" | ✅ `A3ContentLayerTests::Content_cannot_be_edited_after...` | P1 |
| IT-F11-05 | `POST /api/content/versions/{v}/nodes` `{NodeType=Lesson}` (dưới root) | | `400` (lesson không nằm trực tiếp dưới root) | ✅ `A3ContentLayerTests::A_lesson_cannot_be_created_directly...` | P2 |
| IT-F11-06 | `PUT /api/content/nodes/{id}` → `GET .../revisions` → `POST .../revisions/{n}/restore` | node draft | DB `NodeRevision` được ghi; restore khôi phục | ✅ `RemainingFeaturesTests::Updating_a_node_records_a_revision...` | P2 |
| IT-F11-07 | `PATCH /api/content/nodes/{id}/move` | node có subtree | `MaterializedPath` của **cả subtree** viết lại | ✅ `RemainingFeaturesTests::Moving_a_node_rewrites_its_subtree_path` | P1 |
| IT-F11-08 | `POST /api/content/versions/{v}/nodes/reorder` | node anh em | thứ tự cập nhật | 🔲 | P2 |
| IT-F11-09 | `POST .../review` đính comment → `POST /api/courses/reviews/comments/{id}/resolve` | | comment tạo & resolve được | ✅ `RemainingFeaturesTests::Review_can_attach_comments...` | P2 |
| IT-F11-10 | `POST /api/questions` → `.../questions/{id}/submit` → `.../questions/{id}/review {Approve}` | Editor tạo, reviewer duyệt | DB `Question.Status=Approved`; reviewer role gate | ✅ `A3ContentLayerTests::Question_review_workflow...` | P1 |
| IT-F11-11 | CRUD `POST/PUT/DELETE /api/content/nodes/{id}/blocks` (+ resources, flashcard-decks, flashcards) | node draft, Editor | `200`; Student → `403` | 🔲 | P2 |
| IT-F11-12 | `POST /api/exercises` → `.../{id}/questions` → `.../{id}/publish` / `unpublish` | Editor | workflow `200`; DB `Exercise.Status` đổi; Student → `403` | 🟡 (authz có, workflow chưa) | P1 |
| IT-F11-13 | `POST /api/exercises` | ContentEditor | **không** `401`/`403` (qua `[AuthorizeContentRole]`) | ✅ `A1AuthorizationMatrixTests::A1_04_ContentEditorPassesAuthorization` | P1 |

### F12 — Hợp đồng API & Vận hành (`IT-F12`)

| Mã | Method + Route | Tiền đề | Kỳ vọng | Prior art | P |
|---|---|---|---|---|---|
| IT-F12-01 | bất kỳ endpoint thành công | | body = `{ StatusCode, Message, Data }` | ✅ `A5NormalizationTests` + `ContractTests::Success_envelope...` | P1 |
| IT-F12-02 | `GET /api/courses/{idKhôngTồnTại}` (và các lookup khác) | | `404` + envelope, nhất quán giữa các lookup | ✅ `A5NormalizationTests::Missing_resource_is_404...` | P1 |
| IT-F12-03 | endpoint bị chặn quyền | Student gọi endpoint Admin | `403` + envelope (không body rỗng) | ✅ `A5NormalizationTests::Forbidden_is_403...` | P1 |
| IT-F12-04 | `POST` body thiếu field bắt buộc | | `400` + `Errors` trong envelope | ✅ `A5NormalizationTests::Model_validation_failure...` | P1 |
| IT-F12-05 | endpoint trả enum (vd `AttemptStatus`, `PackageTier`) | | serialize thành **tên chuỗi** (`"Submitted"`, `"Premium"`) | ✅ `ContractTests::Enum_fields_serialise_as_their_string_name` | P1 |
| IT-F12-06 | query param enum (`?status=Active`) | | bind đúng từ chuỗi | ✅ `ContractTests::Enum_query_parameters_still_bind...` | P1 |
| IT-F12-07 | endpoint phân trang | | `{ Items, Page, PageSize, Total }` ổn định | ✅ `ContractTests::Paged_result_shape_is_stable` | P1 |
| IT-F12-08 | route PascalCase cũ (`/api/Subscription`, `/api/User`…) | | `404` (route đã kebab-số nhiều) | ✅ `A5NormalizationTests::Old_PascalCase_routes_are_gone` | P1 |
| IT-F12-09 | bất kỳ response | | header `X-Correlation-Id`; nếu request gửi vào thì echo lại | ✅ `P7Tests::Every_response_carries_a_correlation_id...` | P1 |
| IT-F12-10 | `GET /health` · `GET /health/ready` | | `200` (`ready` kiểm DB) | ✅ `P7Tests::Health_endpoints_report_live_and_ready` | P1 |
| IT-F12-11 | sửa field nhạy cảm (role user, status refund) qua API | | DB `AuditLog` có dòng (`AuditSaveChangesInterceptor`) | ✅ `P7Tests::Sensitive_field_change_is_written_to_the_audit_log` | P1 |
| IT-F12-12 | endpoint ném lỗi chưa bắt (dùng test-hook) | | `500` + envelope; **không** chứa `ex.Message` / stack | 🔲 | P1 |
| IT-F12-13 | preflight `OPTIONS` với `Origin` ngoài allowlist | | bị chặn (không có `Access-Control-Allow-Origin`) | 🔲 | P2 |
| IT-F12-14 | endpoint `[EnableRateLimiting("auth"/"ai"/"refund")]` vượt hạn | đặt limit thấp cho test | `429` + body vỏ `ApiResponse` (không rỗng) | 🔲 | P1 |
| IT-F12-15 | `GET /api/users` (endpoint không gắn attribute) | ẩn danh | `401` ([Authorize] fallback toàn cục) | ✅ `A1AuthorizationMatrixTests::A1_01_Anonymous...` | P1 |
| IT-F12-16 | list user/subscription/payment | `?page=&pageSize=&search=` | phân trang + tìm kiếm hoạt động; `pageSize` clamp | ✅ `P7Tests::User_list_is_paged_and_searchable` | P1 |

---

## 5. Ma trận phân quyền (data-driven)

Một class `IT_AuthorizationMatrixTests` chạy **một** `[SkippableTheory]` lặp qua bảng
`(role, method, route, expectedStatus)` thay vì viết tay từng cặp. `role` phân giải qua
`App.AsRole(...)` / `App.Anonymous()`; route dùng `Ids.*` + `Flow.*` để dựng free/paid node.
Bảng nguồn (rút gọn; mỗi dòng × mỗi role = 1 case `IT-AUTHZ-*`):

| Route | Anonymous | Student (chủ) | Student (khác) | Parent (link) | Parent (lạ) | Editor | Finance | Admin |
|---|---|---|---|---|---|---|---|---|
| `GET /api/users` | 401 | 403 | 403 | 403 | 403 | 403 | 403 | 200 |
| `GET /api/catalog/subjects` | 200 | 200 | 200 | 200 | 200 | 200 | 200 | 200 |
| `POST /api/catalog/subjects` | 401 | 403 | 403 | 403 | 403 | 200 | 403 | 200 |
| `GET /api/learn/nodes/{freeNode}` | 200 | 200 | 200 | 200 | 200 | 200 | 200 | 200 |
| `GET /api/learn/nodes/{paidNode}` | 403 | 200¹ | 403 | 403 | 403 | 200 | 403 | 200 |
| `POST /api/exercise-attempts/save-answer` (attempt A) | 401 | 200 | 403 | 403 | 403 | 403 | 403 | 403² |
| `GET /api/exercise-attempts/{A}/result` | 401 | 200 | 403 | 200 | 403 | 403 | 403 | 200² |
| `GET /api/exercise-attempts/student/{A}/history` | 401 | 200 | 403 | 200 | 403 | 403 | 403 | 200² |
| `GET /api/students/{A}/dashboard/overview` | 401 | 200 | 403 | 200 | 403 | 403 | 403 | 200² |
| `POST /api/subscriptions` (cho student A) | 401 | 200 | 403 | 403³ | 403 | 403 | 200 | 200 |
| `PATCH /api/subscriptions/{id}/status` | 401 | 403 | 403 | 403 | 403 | 403 | 200 | 200 |
| `GET /api/payments` | 401 | 403 | 403 | 403 | 403 | 403 | 200 | 200 |
| `POST /api/refunds` (payment của A) | 401 | 200⁴ | 403 | 403 | 403 | 403 | 200 | 200 |
| `POST /api/finance/refunds/{id}/approve` | 401 | 403 | 403 | 403 | 403 | 403 | 200 | 200 |
| `POST /api/courses` | 401 | 403 | 403 | 403 | 403 | 200 | 403 | 200 |
| `POST /api/courses/versions/{v}/publish` | 401 | 403 | 403 | 403 | 403 | 403⁵ | 403 | 200 |
| `POST /api/admin/users/{id}/role` | 401 | 403 | 403 | 403 | 403 | 403 | 403 | 200 |
| `POST /api/sepay/ipn` | 401⁶ | 401⁶ | 401⁶ | 401⁶ | 401⁶ | 401⁶ | 401⁶ | 401⁶ |

¹ khi student chủ đã ghi danh / có entitlement · ² Admin thường vẫn qua ownership guard nhờ `SystemAdmin` bypass — chốt theo `ResourceAccessService` · ³ trừ khi resource-access cho phép phụ huynh mua hộ (chốt theo code) · ⁴ chỉ payment mình sở hữu · ⁵ publish cần `AcademicReviewer`/`SystemAdmin`, Editor chỉ `submit` · ⁶ chỉ qua header `Apikey`, không qua JWT.

---

## 6. Prior art trong code test cũ

> Các file dưới đây **sẽ bị xoá** cùng hạ tầng cũ. Không "port nguyên" — chỉ mở lại để lấy
> **logic assert** (chuỗi so khớp, thứ tự bước, tên property JSON) khi viết case mới trên
> hạ tầng [§2](#2-hạ-tầng-test-xây-mới-hoàn-toàn).

| File cũ (sẽ xoá) | ~Số test | Có logic tham khảo cho |
|---|---|---|
| `A1AuthorizationMatrixTests.cs` | 20 | ma trận phân quyền §5 (F1/F4/F5/F6/F7/F9/F11) |
| `A2BusinessLogicTests.cs` | 10 | F4 (chấm điểm, `complete` nhanh), F7 (giá subscription) |
| `A3ContentLayerTests.cs` | 10 | F2 (content gate, ghi danh), F11 (version workflow, duyệt câu hỏi) |
| `A5NormalizationTests.cs` | 5 | F12 (envelope, status, route kebab) |
| `ContractTests.cs` | 8 | F12 (shape, enum-string, concurrency smoke) |
| `P1AuthTests.cs` | 6 | F1 (refresh rotation, lockout, forgot/reset, admin lock/role) |
| `P3P4RemainingTests.cs` | 2 | F4 (concurrent submit), F5 (so tuần) |
| `P4ProgressTests.cs` | 4 | F3 (roll-up, view-time), F5 (heatmap guard, tier) |
| `P5PaymentTests.cs` | 8 | F7 (ma trận IPN, sweep, reconciliation) |
| `P6Tests.cs` | 9 | F6 (parent link/revoke), F9 (hint quota, chatbot), F10 (notif rules) |
| `P7Tests.cs` | 6 | F12 (health, correlation, pagination, audit, token invalidation) |
| `RefundWorkflowTests.cs` | 16 | F8 (toàn bộ máy trạng thái hoàn tiền + CSV) |
| `RemainingFeaturesTests.cs` | 6 | F8, F9, F11, F12 (revisions, re-parent, escalation, system config) |

Code cũ ~**110 test**. Danh mục mới này liệt kê **~180** case F1–F12 + ma trận §5.

---

## 7. Bố cục thư mục

Dự án test **mới** `ELearning_ToanHocHay.Tests` (xem [§2.1](#21-dự-án-test)):

```
ELearning_ToanHocHay.Tests/
├─ Integration/
│  ├─ Infrastructure/
│  │  ├─ PostgresFixture.cs          (§2.2)
│  │  ├─ ApiFactory.cs               (§2.3)
│  │  ├─ FakeAiService.cs            (§2.4)
│  │  ├─ FakeEmailSink.cs            (§2.4)
│  │  ├─ FakeClock.cs                (§2.4, tuỳ chọn)
│  │  ├─ SeedData.cs + SeededIds.cs  (§2.5)
│  │  ├─ FlowSeed.cs                 (§2.6)
│  │  ├─ SePayIpn.cs                 (§2.7)
│  │  ├─ IntegrationCollection.cs + IntegrationTest.cs  (§2.8)
│  │  └─ Envelope.cs                 (§2.9)
│  ├─ IT_F1_AuthTests.cs
│  ├─ IT_F2_CatalogEnrolmentTests.cs
│  ├─ IT_F3_LearningTests.cs
│  ├─ IT_F4_ExerciseAttemptTests.cs
│  ├─ IT_F5_StudentDashboardTests.cs
│  ├─ IT_F6_ParentLinkTests.cs
│  ├─ IT_F7_PaymentTests.cs
│  ├─ IT_F8_RefundTests.cs
│  ├─ IT_F9_AiChatbotTests.cs
│  ├─ IT_F10_NotificationTests.cs
│  ├─ IT_F11_AuthoringTests.cs
│  ├─ IT_F12_ContractOpsTests.cs
│  └─ IT_AuthorizationMatrixTests.cs   (§5)
└─ Unit/                                (xem DANH-MUC-UNIT-TEST.md §5)
```

`[Trait("Level","Integration")]` + `[Trait("Flow","<tên>")]` + `[Trait("Priority","Pn")]` cho mọi class/test.

---

## 8. Lộ trình triển khai

| Bước | Nội dung | Chặn bởi |
|---|---|---|
| **I0** | Dựng hạ tầng [§2](#2-hạ-tầng-test-xây-mới-hoàn-toàn) từ đầu: dự án test, `PostgresFixture`, `ApiFactory` (+ env), fakes, `SeedData`/`SeededIds`, `FlowSeed`, `SePayIpn`, `Envelope`, collection + base. Chạy được 1 test khói (`GET /health` → 200). | — |
| **I1** | F7 + F8 (luồng tiền) — toàn bộ `IT-F7-*`, `IT-F8-*` | I0 |
| **I2** | F1 — toàn bộ `IT-F1-*` (vòng đời tài khoản) | I0 |
| **I3** | F2 + F3 + F4 — `IT-F2/F3/F4-*` | I0 |
| **I4** | F5 + F6 — assert **số liệu** dashboard, `IT-F5/F6-*` | I0 |
| **I5** | F9 + F10 + F11 — `IT-F9/F10/F11-*` | I0 |
| **I6** | F12 + §5 — `IT-F12-*` + ma trận phân quyền data-driven | I0 |

---

## 9. Trạng thái hiện tại

> Cập nhật 2026-09-06.

- ✅ **Unit test B0–B6 đã xong** (433 test — xem DANH-MUC-UNIT-TEST §7). Integration bắt đầu.
- ✅ **I0 — hạ tầng integration (§2): XONG.** `dotnet test --filter "Level=Integration"` —
  **18/18 xanh** với Docker, tự skip khi không có Docker.
  - Gói: `Microsoft.AspNetCore.Mvc.Testing`, `Testcontainers.PostgreSql` 3.10, `Xunit.SkippableFact`,
    `Microsoft.EntityFrameworkCore.Relational`.
  - `Program` đã là `public class Program` → `WebApplicationFactory<Program>` dùng trực tiếp (không cần `partial`).
  - `Integration/Infrastructure/`: `ApiFactory` (gộp luôn `PostgreSqlContainer` + `IAsyncLifetime`;
    env wiring; `As`/`AsRole`/`MintToken`/`Db` helpers; swap `IAIService`→`FakeAiService`,
    `IEmailService`+`IBackgroundEmailService`→`FakeEmailSink`, gỡ mọi `IHostedService`),
    `Fakes` (`FakeAiService`, `FakeEmailSink` + `LastLink`/`LastToken`), `SeedData` (golden dataset
    §2.5 + `SeededIds` + `TestRole`), `FlowSeed` (mới có nhóm auth: `NewConfirmedUserAsync`,
    `NewUnconfirmedUserAsync`, `SeedRefundablePaymentAsync` — bổ sung dần), `SePayIpn`,
    `IntegrationCollection`/`IntegrationTest`, `Envelope`.
  - `IT_I0_HarnessTests` — 3 test khói (seed, /health, /api/auth/me).
- ✅ **F1 — Xác thực & tài khoản: 15 case** (`IT_F1_AuthTests`): IT-F1-01..05, 08..11, 13/14, 15,
  18, 19, 20, 23. Login thất bại thực tế trả **401** (không phải 400 như bảng) — test nhận cả hai.
- ✅ **F2 — Danh mục & ghi danh: 12 case** (`IT_F2_CatalogEnrollmentTests`): IT-F2-01, 03..13.
  `FlowSeed` thêm `PublishCourseAsync` / `EnrolAsync` / `GrantEntitlementAsync`.
- ✅ **F3 — Học bài & tiến độ: 8 case** (`IT_F3_LearnProgressTests`): IT-F3-02..09.
  mark-complete (cổng ghi danh, ngưỡng 20s), roll-up bài→chương→cache khoá, version-progress,
  bài ngoài khoá đã ghi danh → 403, heatmap owner-guard.
- ✅ **F4 — Làm bài tập: 11 case** (`IT_F4_ExerciseAttemptTests`): IT-F4-01, 02, 05..12, 17.
  Gồm chấm đủ 4 loại câu, cổng gói 403, ghi đè answer, ma trận quyền `/result`, và
  **complete song song → chấm đúng 1 lần** (row-lock). `FlowSeed` thêm `NewStudentAsync` /
  `PublishExerciseAsync`.
- ✅ **F5 — Dashboard học sinh: 6 case** (`IT_F5_DashboardTests`): IT-F5-01, 04..08.
  tier lấy từ `Package.Tier` (gói tên "Standard" nhưng Tier=Premium), Free → 403 "cần gói",
  Student B → 403 mọi endpoint, subscription/current trả gói/Free.
- ✅ **F6 — Liên kết phụ huynh: 8 case** (`IT_F6_ParentTests`): IT-F6-01..08.
  tạo invite, link bằng connection code, code sai → 400/404, list children, phụ huynh liên kết
  đọc được history/dashboard con, thu hồi link → mất quyền ngay, phụ huynh không sửa được phụ
  huynh khác, xoá cần admin.
- ✅ **F7 — Thanh toán SePay + IPN: 13 case** (`IT_F7_PaymentTests`): IT-F7-01..09, 11, 13, 15..18.
  Giá lấy từ `Package.Price` (bỏ qua `amount` client gửi), IPN hợp lệ → Active + Completed + 30d,
  **replay cùng referenceCode → Duplicate, 1 dòng `SePayIpnLog`**, sai tiền → giữ Pending,
  ref mới trên sub Active → Duplicate, out/unknown → Ignored, sai API key → 401,
  **gói thứ 2 Active → gói cũ tự Expired**, lifecycle sweep, các endpoint Finance-only.
  `FlowSeed` thêm `CreatePendingSubscriptionAsync` / `ActivateSubscriptionViaIpnAsync`.
- ✅ **F8 — Hoàn tiền: 15 case** (`IT_F8_RefundTests`): IT-F8-01..03, 05..12, 16..19.
  student tạo yêu cầu (201 + `RefundEvent`), payment người khác → 403, payment Pending → 400,
  yêu cầu mở thứ 2 → 409, giới hạn 30 ngày (+ Admin bỏ qua), approve → Approved,
  **trần ngày → "Vượt trần"**, **dual-control 2 người** (config set/reset + bust cache),
  reject→approve → 409, **full batch flow** (Payment=Refunded, RefundAmount đủ, Subscription=Cancelled),
  hoàn một phần → PartiallyRefunded, **CSV formula-injection** (`'=cmd`, Draft→Exported),
  **API chỉ trả 4 số cuối, DB là ciphertext** (Data Protection thật).
  `FlowSeed` thêm `CreateRefundRequestAsync` / `SetConfigAsync` / `SeedActiveSubscriptionAsync`;
  `ApiFactory.BustCache`.
- ✅ **F9 — Trợ giúp AI & Chatbot: 7 case** (`IT_F9_AiChatTests`): IT-F9-01..04, 06, 07, 10.
  Free hết lượt gợi ý → 429, gói unlimited không bao giờ 429, `/quota` used/limit/remaining,
  hint attempt người khác → 403 / ẩn danh → 401, chatbot lưu turn khi AI down + `/health` 503,
  chatbot ẩn danh → 401. (IT-F9-05 feedback nền cần hosted service — đã gỡ ở harness; để lại.)
- ⏳ **Còn lại:** F10–F12; ma trận §5.
- **Tổng:** 531 test xanh (433 unit + 98 integration).
