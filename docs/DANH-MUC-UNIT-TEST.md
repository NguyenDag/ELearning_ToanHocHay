# Danh mục Unit Test — ToanHocHay (bản gốc)

> **Đây là tài liệu gốc về unit test của hệ thống.** Mọi case unit test được liệt kê
> **đầy đủ** tại đây, theo từng mục rõ ràng. Không tham chiếu "mở rộng từ file khác" —
> nếu file test nào đó đã có sẵn một phần, phần đó vẫn được ghi lại đầy đủ ở đây và coi
> như đặc tả chuẩn.
>
> ⚠️ **Hạ tầng test cũ (`Tests/*` — kể cả `Tests/AnswerGradingTests.cs`) đã bị xoá.**
> [§1.3](#13-hạ-tầng-test-xây-mới) đặc tả hạ tầng mới — **B0 đã hoàn thành** (xem [§7](#7-trạng-thái-hiện-tại));
> các case nghiệp vụ B1–B6 chưa viết.
>
> Integration: [DANH-MUC-INTEGRATION-TEST.md](DANH-MUC-INTEGRATION-TEST.md) · System: [DANH-MUC-SYSTEM-TEST.md](DANH-MUC-SYSTEM-TEST.md) · Luồng: [KE-HOACH-KIEM-THU-HE-THONG.md](KE-HOACH-KIEM-THU-HE-THONG.md).
> Ký hiệu ưu tiên **P**: P1 bắt buộc · P2 nên có · P3 khi rảnh.
> Ký hiệu tầng **T**: **U1** = thuần (không hạ tầng) · **U2** = sociable (SQLite in-memory + NSubstitute).

---

## Mục lục

- [1. Nguyên tắc & phân tầng](#1-nguyên-tắc--phân-tầng)
  - [1.3 Hạ tầng test (xây mới)](#13-hạ-tầng-test-xây-mới)
- [2. Refactor mở đường (làm trước)](#2-refactor-mở-đường-làm-trước)
- [3. Danh mục unit test](#3-danh-mục-unit-test)
  - [3.1 AuthService — Đăng nhập](#31-authservice--đăng-nhập-ut-auth-login)
  - [3.2 AuthService — Refresh token](#32-authservice--refresh-token-ut-auth-refresh)
  - [3.3 AuthService — Đăng xuất & đổi mật khẩu](#33-authservice--đăng-xuất--đổi-mật-khẩu-ut-auth-pwd)
  - [3.4 AuthService — Xác nhận email](#34-authservice--xác-nhận-email-ut-auth-confirm)
  - [3.5 AuthService — Quên / đặt lại mật khẩu](#35-authservice--quên--đặt-lại-mật-khẩu-ut-auth-reset)
  - [3.6 AuthService — Đăng ký](#36-authservice--đăng-ký-ut-auth-register)
  - [3.7 AuthService — Validate token](#37-authservice--validate-token-ut-auth-validate)
  - [3.8 LoginThrottlePolicy](#38-loginthrottlepolicy-ut-throttle)
  - [3.9 JwtService](#39-jwtservice-ut-jwt)
  - [3.10 PasswordHasher](#310-passwordhasher-ut-pwd)
  - [3.11 SecureTokens](#311-securetokens-ut-token)
  - [3.12 ClaimsPrincipalExtensions](#312-claimsprincipalextensions-ut-claims)
  - [3.13 AnswerGrading — chấm điểm](#313-answergrading--chấm-điểm-ut-grade)
  - [3.14 SePayContentParser](#314-sepaycontentparser-ut-sepay-parse)
  - [3.15 SePayAmountMatcher](#315-sepayamountmatcher-ut-sepay-amount)
  - [3.16 SePayIpnEvaluator](#316-sepayipnevaluator-ut-sepay-eval)
  - [3.17 SePayService — QR & API key](#317-sepayservice--qr--api-key-ut-sepay-svc)
  - [3.18 RateLimitPartitioning](#318-ratelimitpartitioning-ut-rlp)
  - [3.19 RefundCsvWriter](#319-refundcsvwriter-ut-csv)
  - [3.20 RefundCompletion](#320-refundcompletion-ut-rfc)
  - [3.21 RefundDayWindow](#321-refunddaywindow-ut-rfp-window)
  - [3.22 RefundService — chính sách duyệt](#322-refundservice--chính-sách-duyệt-ut-rfp)
  - [3.23 RefundFieldProtector](#323-refundfieldprotector-ut-prot)
  - [3.24 ContentAccessService — Covers](#324-contentaccessservice--covers-ut-gate-covers)
  - [3.25 ContentAccessService — GetCourseAccess](#325-contentaccessservice--getcourseaccess-ut-gate)
  - [3.26 EnrollmentService](#326-enrollmentservice-ut-enrol)
  - [3.27 PagedRequest / PagingExtensions](#327-pagedrequest--pagingextensions-ut-page)
  - [3.28 AiQuotaService](#328-aiquotaservice-ut-quota)
  - [3.29 SubscriptionLifecycleService](#329-subscriptionlifecycleservice-ut-life)
  - [3.30 PackageTierResolver](#330-packagetierresolver-ut-tier)
  - [3.31 ProgressProjectionService](#331-progressprojectionservice-ut-prog)
  - [3.32 ResourceAccessService — quyền truy cập](#332-resourceaccessservice--quyền-truy-cập-ut-res)
  - [3.33 SystemConfigService](#333-systemconfigservice-ut-cfg)
  - [3.34 NotificationService & NotificationRules](#334-notificationservice--notificationrules-ut-notif)
  - [3.35 ExerciseAttemptService](#335-exerciseattemptservice-ut-att)
  - [3.36 Authorization attributes](#336-authorization-attributes-ut-attr)
  - [3.37 CorrelationIdMiddleware & GlobalExceptionHandler](#337-correlationidmiddleware--globalexceptionhandler-ut-mw)
  - [3.38 Validation DTO](#338-validation-dto-ut-dto)
- [4. Ánh xạ Unit → Integration](#4-ánh-xạ-unit--integration)
- [5. Bố cục thư mục](#5-bố-cục-thư-mục)
- [6. Thứ tự triển khai](#6-thứ-tự-triển-khai)
- [7. Trạng thái hiện tại](#7-trạng-thái-hiện-tại)

---

## 1. Nguyên tắc & phân tầng

### 1.1 Hai tầng unit

"Unit test" ở dự án này = **không Docker, không `WebApplicationFactory`, không Postgres thật.**

| Tầng | Là gì | Hạ tầng | Tốc độ |
|---|---|---|---|
| **U1 — Pure** | Class/hàm tĩnh thuần: chấm điểm, regex, chuẩn hoá, chính sách khoá, sinh/băm token, ánh xạ claim, CSV, so tiền | Không | < 1ms |
| **U2 — Sociable** | Một service cô lập: EF Core **SQLite in-memory** cho DB + test double (`NSubstitute`) cho email / AI / JWT / config / repository | SQLite in-memory + NSubstitute | 5–20ms |

### 1.2 Vì sao SQLite in-memory (không `EFCore.InMemory`)

Giữ khoá ngoại, `UNIQUE`, transaction, `LIKE`/`StartsWith` — sát Postgres, tránh "chỉ xanh trên InMemory".

### 1.3 Hạ tầng test (xây mới)

> Không có gì để tái sử dụng. Dựng từ dự án test trắng.

**Dự án** `ELearning_ToanHocHay.Tests` — dùng chung cho cả unit và integration
(xem [DANH-MUC-INTEGRATION-TEST.md §2.1](DANH-MUC-INTEGRATION-TEST.md)). `net8.0`,
`ProjectReference` → `ELearning_ToanHocHay_Control.csproj`. Gói cho tầng unit:

```xml
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
<PackageReference Include="xunit" Version="2.9.2" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
<PackageReference Include="FluentAssertions" Version="6.12.1" />
<PackageReference Include="NSubstitute" Version="5.1.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="8.0.*" />
<PackageReference Include="Microsoft.AspNetCore.DataProtection" Version="8.0.*" />  <!-- UT-PROT -->
```

**Cho phép test truy cập `internal`** — thêm vào project chính `ELearning_ToanHocHay_Control.csproj`:

```xml
<ItemGroup>
  <InternalsVisibleTo Include="ELearning_ToanHocHay.Tests" />
</ItemGroup>
```

**File hạ tầng cần tạo** (`Tests/Unit/Infrastructure/`):

| File | Vai trò |
|---|---|
| `SqliteDb.cs` | `static SqliteDb.New()` → mở `SqliteConnection("DataSource=:memory:")` (giữ mở suốt vòng đời test), `new DbContextOptionsBuilder<AppDbContext>().UseSqlite(conn)`, `db.Database.EnsureCreated()`. Trả `AppDbContext` + `IDisposable` gom connection. Mỗi test một instance mới. |
| `TestConfig.cs` | `static IConfiguration Build(params (string key, string val)[] overrides)` — `ConfigurationBuilder().AddInMemoryCollection(...)`. Preset `TestConfig.Jwt` (SecretKey ≥ 32, Issuer, Audience, ExpirationMinutes). |
| `Claims.cs` | `static ClaimsPrincipal For(UserType type, int? userId = null, int? studentId = null, int? parentId = null)` — dựng `ClaimsPrincipal` với đúng custom claim (`CustomJwtClaims.*`). `static ClaimsPrincipal Anonymous()`. |
| `Entities.cs` | Object mother: `NewUser(email?, UserType, bool confirmed = true)`, `NewStudent(userId, gradeId?)`, `NewParent(userId, code?)`, `NewPayment(payerUserId, amount, PaymentStatus)`, `NewSubscription(...)`, `NewCourse(status, subjectId, gradeId)`, `NewQuestion(type, correct?)`, `NewRefundRequest(paymentId, amount, status)`. Chỉ set field bắt buộc; nhận override qua `Action<T>? tweak`. |
| `Fakes.cs` | `FakeRefundFieldProtector : IRefundFieldProtector` (`Protect(x)=>"enc:"+x`, `Unprotect("enc:"+x)=>x`, ném nếu không prefix — hoặc cờ `ThrowOnUnprotect`); `FakeClock : TimeProvider`; `FakeSystemConfig : ISystemConfigService` (dict + fallback); `RecordingEmail : IEmailService, IBackgroundEmailService`. |
| `DataProtection.cs` | `static IDataProtectionProvider Ephemeral()` — `DataProtectionProvider.Create(new DirectoryInfo(Path.GetTempPath()))` hoặc `EphemeralDataProtectionProvider` cho `UT-PROT`. |

**Quy ước:**

- Class: `{ClassUnderTest}Tests` trong `namespace ELearning_ToanHocHay.Tests.Unit.{Nhóm}`.
- Test: `MethodUnderTest_Condition_ExpectedResult` **và/hoặc** khớp mã `UT-...` của tài liệu này (đặt trong tên hoặc `[Trait("Case","UT-AUTH-LOGIN-06")]`).
- `[Trait("Level","Unit")]` + `[Trait("Tier","U1"|"U2")]` cho mọi class → `dotnet test --filter "Level=Unit"` chạy **không cần Docker**.
- Mỗi test một hành vi; `// Arrange / Act / Assert`.
- U2: mỗi test một `AppDbContext` SQLite mới (`using var db = SqliteDb.New();`), seed tối thiểu bằng `Entities.*`.
- U1: không `new AppDbContext`, không I/O; chỉ hàm/entity trong bộ nhớ.

---

## 2. Refactor mở đường (làm trước) ✅ B2 — XONG (2026-09-06)

Một số logic đang chôn trong `private` hoặc dính `AppDbContext` — tách ra để nâng lên **U1**:

| Hiện tại | Tách thành (mới) | Trạng thái |
|---|---|---|
| `AuthService.RegisterFailedLoginAsync` (công thức khoá leo thang) | `static LoginThrottlePolicy.NextLockout(int failedCount) : TimeSpan?` | ✅ `Services/Helpers/LoginThrottlePolicy.cs`; AuthService gọi vào |
| `AuthService.ResolvePackageTierAsync` (query `_context.Subscriptions`) | `IPackageTierResolver.ResolveAsync(int studentId) : Task<PackageTier>` | ✅ `Services/{Interfaces/IPackageTierResolver,Implementations/PackageTierResolver}.cs`; DI scoped |
| `AuthService.IssueTokenPairAsync` (ghi `_context` + đọc config) | `IRefreshTokenIssuer.IssueAsync(user, studentId, parentId, ip) : Task<TokenPairDto>` | ✅ `Services/{Interfaces/IRefreshTokenIssuer,Implementations/RefreshTokenIssuer}.cs`; DI scoped |
| `SePayIpnService` — regex `SUBSCRIPTION[\-_]?(\d+)` | `static SePayContentParser.TryParseSubscriptionId(string?) : int?` | ✅ `Services/Helpers/SePayContentParser.cs`; `SePayService.ExtractSubscriptionId` gọi vào |
| `SePayIpnService` — so tiền ± dung sai | `static SePayAmountMatcher.Matches(decimal expected, decimal actual, decimal toleranceVnd) : bool` | ✅ `Services/Helpers/SePayAmountMatcher.cs` (làm tròn `expected` away-from-zero; tolerance âm → 0) |
| `SePayIpnService.EvaluateAsync` — chuỗi kiểm tra | `static SePayIpnEvaluator.Evaluate(SePayIpnRequest req, Subscription? sub, decimal toleranceVnd) : IpnOutcome` | ✅ `Services/Helpers/SePayIpnEvaluator.cs`; `SePayIpnService.EvaluateAsync` dùng cho toàn bộ ma trận, chỉ ghi khi `Processed` |
| `RefundService.CheckDailyCapAsync` — mốc 00:00 theo offset | `static RefundDayWindow.StartOfDayUtc(DateTime nowUtc, int offsetHours) : DateTime` | ✅ `Services/Helpers/RefundDayWindow.cs`; `RefundService.DayWindowAsync` gọi vào |
| `RefundCompletion.ApplyAsync(AppDbContext, RefundRequest)` | `static RefundCompletion.Apply(Payment paymentWithSubscription, RefundRequest req)` | ✅ thêm overload thuần `Apply`; `ApplyAsync` giữ nguyên (load Payment rồi gọi `Apply`) — 2 caller cũ không đổi |
| `ContentAccessService.Covers` (`private static`) | `internal static` | ✅ đổi `private`→`internal` (project chính đã khai `<InternalsVisibleTo>` — §1.3) |
| `ProgressProjectionService` hằng `LessonCompleteScorePct`, `MinViewSeconds` + công thức % | `static ProgressRollup` (tính % từ danh sách con; ngưỡng hoàn thành) | ✅ `Services/Helpers/ProgressRollup.cs`; service tham chiếu hằng + `PercentComplete` (2 chỗ) |
| Rải rác `DateTime.UtcNow` | Inject `TimeProvider` (net8) | ✅ đã inject vào `AuthService`, `RefundService`, `PackageTierResolver`, `RefreshTokenIssuer` (qua `private DateTime Now`); `TimeProvider.System` đăng ký singleton trong `Program.cs`. Các service khác (ProgressProjection, AiQuota, SubscriptionLifecycle) giữ `DateTime.UtcNow` — sẽ inject khi viết test B5 nếu cần |

**Build sạch, 143/143 test B0+B1 vẫn xanh sau refactor** (không đổi hành vi).

> `RefundCompletion.Apply` giữ đúng chữ ký 2 tham số như đặc tả §3.20; `RefundedAt` vẫn dùng
> `DateTime.UtcNow` nội bộ (test chỉ khẳng định "đã set").

---

## 3. Danh mục unit test

### 3.1 AuthService — Đăng nhập (`UT-AUTH-LOGIN`) ✅ B4

**Hàm:** `Task<ApiResponse<LoginResponseDto>> LoginAsync(LoginRequestDto request, string? ip)`
**Fake:** `IUserRepository`, `IStudentRepository`, `IParentRepository`, `IPasswordHasher`,
`IPackageTierResolver`, `IRefreshTokenIssuer`, `IMemoryCache`, `IConfiguration`, `IOptions<AppSettings>`.
**Tầng:** U1 (sau [§2](#2-refactor-mở-đường-làm-trước)).

| Mã | Tình huống | Dữ liệu vào | Kỳ vọng | P |
|---|---|---|---|---|
| UT-AUTH-LOGIN-01 | Email không tồn tại | `GetByEmailAsync → null` | `Success=false`, message = `"Email hoặc mật khẩu không đúng"`; **không** gọi `VerifyPassword` | P1 |
| UT-AUTH-LOGIN-02 | Sai mật khẩu | user hợp lệ, `VerifyPassword → false` | `Success=false`, cùng message mờ; `RegisterFailedLogin` chạy (`FailedLoginCount` +1, `UpdateUserAsync` gọi) | P1 |
| UT-AUTH-LOGIN-03 | Sai mật khẩu lần thứ 5 | `FailedLoginCount = 4` trước đó | sau đó `LockoutEndsAt ≈ now + 1′` được set | P1 |
| UT-AUTH-LOGIN-04 | Đang bị khoá tạm | `LockoutEndsAt = now + 10′` | message chứa `"tạm khoá"` + `"Thử lại sau"`; **không** kiểm mật khẩu | P1 |
| UT-AUTH-LOGIN-05 | Hết hạn khoá + mật khẩu đúng | `LockoutEndsAt = now - 1′`, `VerifyPassword → true` | đăng nhập OK; `FailedLoginCount` về 0, `LockoutEndsAt` về null | P1 |
| UT-AUTH-LOGIN-06 | Email chưa xác nhận | `IsEmailConfirmed = false` | message = `"Vui lòng xác nhận email trước khi đăng nhập"` | P1 |
| UT-AUTH-LOGIN-07 | Tài khoản `IsActive = false` | | message = `"Tài khoản đã bị vô hiệu hóa"` | P1 |
| UT-AUTH-LOGIN-08 | Admin đã khoá (`LockedAt != null`) | `IsActive = true`, `LockedAt = now` | message = `"Tài khoản đã bị vô hiệu hóa"` | P1 |
| UT-AUTH-LOGIN-09 | Student không có bản ghi `Student` | `UserType = Student`, `GetByUserIdAsync → null` | message = `"Không tìm thấy thông tin học sinh"` | P2 |
| UT-AUTH-LOGIN-10 | Parent không có bản ghi `Parent` | `UserType = Parent`, `GetByUserIdAsync → null` | message = `"Không tìm thấy thông tin phụ huynh"` | P2 |
| UT-AUTH-LOGIN-11 | Student có gói Premium Active | `PackageTierResolver → Premium` | `Data.PackageTier = Premium`, `Data.StudentId` điền | P1 |
| UT-AUTH-LOGIN-12 | Student không gói | `PackageTierResolver → Free` | `Data.PackageTier = Free` | P1 |
| UT-AUTH-LOGIN-13 | Đăng nhập thành công | mọi thứ hợp lệ | `Data.Token` & `Data.RefreshToken` khác rỗng; `UpdateLastLoginAsync` gọi; `IssueTokenPair` gọi **1 lần** | P1 |
| UT-AUTH-LOGIN-14 | Thành công sau nhiều lần sai | `FailedLoginCount = 3` | counter reset trước khi phát token | P2 |
| UT-AUTH-LOGIN-15 | Repository ném exception | `GetByEmailAsync` throw | `Success=false`, message = `"Đã xảy ra lỗi"`; **không** rò exception ra ngoài | P2 |
| UT-AUTH-LOGIN-16 | Parent đăng nhập thành công | `UserType = Parent`, có `Parent` | `Data.ParentId` điền, `Data.StudentId = null`, `PackageTier = Free` | P2 |
| UT-AUTH-LOGIN-17 | `ip` được truyền vào token issuer | `ip = "1.2.3.4"` | `IssueTokenPair` nhận đúng `ip` | P3 |

### 3.2 AuthService — Refresh token (`UT-AUTH-REFRESH`) ✅ B4

**Hàm:** `Task<ApiResponse<TokenPairDto>> RefreshTokenAsync(string refreshToken, string? ip)`
**Fake:** `IRefreshTokenRepository`, `IUserRepository`, `IStudentRepository`, `IParentRepository`, `IRefreshTokenIssuer`.
**Tầng:** U1.

| Mã | Tình huống | Kỳ vọng | P |
|---|---|---|---|
| UT-AUTH-REFRESH-01 | `refreshToken` rỗng / null / khoảng trắng | message = `"Refresh token không hợp lệ"` | P1 |
| UT-AUTH-REFRESH-02 | Hash không có trong kho (`GetByHashAsync → null`) | message = `"Refresh token không hợp lệ"` | P1 |
| UT-AUTH-REFRESH-03 | Token đã bị thu hồi (`RevokedAt != null`) — nghi tái sử dụng | `RevokeAllForUserAsync(userId)` được gọi; trả error | P1 |
| UT-AUTH-REFRESH-04 | Token hết hạn (`ExpiresAt < now`, `RevokedAt == null`) | error; **không** gọi `RevokeAllForUserAsync` | P1 |
| UT-AUTH-REFRESH-05 | Token active, user `IsActive = false` hoặc `LockedAt != null` | message = `"Tài khoản không tồn tại hoặc bị khóa"` | P1 |
| UT-AUTH-REFRESH-06 | Token active, user hợp lệ | phát cặp mới; token cũ `RevokedAt` set + `ReplacedByTokenHash = Hash(new refresh)`; `SaveAsync` gọi | P1 |
| UT-AUTH-REFRESH-07 | User là Student | token mới được phát kèm `studentId` đúng | P2 |
| UT-AUTH-REFRESH-08 | User là Parent | token mới kèm `parentId` đúng | P2 |
| UT-AUTH-REFRESH-09 | `ip` truyền vào token issuer | issuer nhận đúng `ip` | P3 |

### 3.3 AuthService — Đăng xuất & đổi mật khẩu (`UT-AUTH-PWD`) ✅ B4

**Hàm:** `LogoutAsync(int userId, string? refreshToken)`, `ChangePasswordAsync(int userId, ChangePasswordDto request)`
**Fake:** `IRefreshTokenRepository`, `IUserRepository`, `IPasswordHasher`, `IMemoryCache`.
**Tầng:** U1.

| Mã | Tình huống | Kỳ vọng | P |
|---|---|---|---|
| UT-AUTH-PWD-01 | Logout có `refreshToken` của chính user, chưa revoke | chỉ token đó `RevokedAt` set; `SaveAsync` gọi | P1 |
| UT-AUTH-PWD-02 | Logout **không** truyền `refreshToken` | `RevokeAllForUserAsync(userId)` được gọi | P1 |
| UT-AUTH-PWD-03 | Logout với `refreshToken` thuộc user khác | không revoke gì; vẫn trả success | P2 |
| UT-AUTH-PWD-04 | Logout với token đã revoke sẵn | không revoke lại; trả success | P3 |
| UT-AUTH-PWD-05 | ChangePassword user không tồn tại | message = `"Tài khoản không tồn tại"` | P1 |
| UT-AUTH-PWD-06 | ChangePassword `CurrentPassword` sai | message = `"Mật khẩu hiện tại không đúng"`; hash **không** đổi | P1 |
| UT-AUTH-PWD-07 | ChangePassword thành công | `PasswordHash` = giá trị mới từ `HashPassword`; `SecurityStamp` đổi; `RevokeAllForUserAsync` gọi; `_cache.Remove("sstamp:{userId}")` gọi | P1 |
| UT-AUTH-PWD-08 | ChangePassword thành công — message | chứa `"đăng nhập lại"` | P3 |

### 3.4 AuthService — Xác nhận email (`UT-AUTH-CONFIRM`) ✅ B5

**Hàm:** `ConfirmEmailAsync(string token)`, `ResendConfirmationEmailAsync(string email)`
**Fake / hạ tầng:** `AppDbContext` (SQLite — bảng `EmailVerificationTokens`, `Users`), `IUserRepository`, `IBackgroundEmailService`, `IOptions<AppSettings>`.
**Tầng:** U2.

| Mã | Tình huống | Kỳ vọng | P |
|---|---|---|---|
| UT-AUTH-CONFIRM-01 | `ConfirmEmail` token không tồn tại | message = `"Liên kết không hợp lệ"` | P1 |
| UT-AUTH-CONFIRM-02 | `ConfirmEmail` token `IsUsed = true` | message = `"Liên kết không hợp lệ"` | P1 |
| UT-AUTH-CONFIRM-03 | `ConfirmEmail` token `ExpiredAt < now` | message = `"Liên kết không hợp lệ"` | P1 |
| UT-AUTH-CONFIRM-04 | `ConfirmEmail` token hợp lệ | `User.IsEmailConfirmed = true`, `EmailConfirmedAt` set, token `IsUsed = true` | P1 |
| UT-AUTH-CONFIRM-05 | `Resend` email không tồn tại | `Success = true`, message mờ (`"Nếu email tồn tại…"`); **không** queue email | P1 |
| UT-AUTH-CONFIRM-06 | `Resend` tài khoản đã xác nhận | `Success = false`, message = `"Tài khoản này đã được xác nhận trước đó"` | P2 |
| UT-AUTH-CONFIRM-07 | `Resend` hợp lệ | mọi token cũ chưa dùng → `IsUsed = true`; token mới `ExpiredAt ≈ now + 24h`; `QueueConfirmationEmail` gọi với link chứa `/api/auth/confirm-email?token=` | P1 |
| UT-AUTH-CONFIRM-08 | `Resend` — link dùng `AppSettings.BaseUrl` (TrimEnd `/`) | không có `//` thừa | P3 |

### 3.5 AuthService — Quên / đặt lại mật khẩu (`UT-AUTH-RESET`) ✅ B5

**Hàm:** `ForgotPasswordAsync(string email)`, `ResetPasswordAsync(string token, string newPassword)`
**Hạ tầng:** `AppDbContext` (SQLite — `PasswordResetTokens`, `Users`), `IUserRepository`, `IPasswordHasher`, `IRefreshTokenRepository`, `IBackgroundEmailService`, `IMemoryCache`, `IOptions<AppSettings>`.
**Tầng:** U2.

| Mã | Tình huống | Kỳ vọng | P |
|---|---|---|---|
| UT-AUTH-RESET-01 | `Forgot` email không tồn tại | `Success = true` mờ; **không** tạo token; **không** queue email | P1 |
| UT-AUTH-RESET-02 | `Forgot` user chưa xác nhận email | như trên (không lộ, không gửi) | P1 |
| UT-AUTH-RESET-03 | `Forgot` user `IsActive = false` | như trên | P1 |
| UT-AUTH-RESET-04 | `Forgot` hợp lệ | mọi token mở cũ → `IsUsed = true`; token mới `ExpiredAt ≈ now + 1h`; `QueuePasswordResetEmail` với link `{BaseUrl}/reset-password?token=` | P1 |
| UT-AUTH-RESET-05 | `Reset` token không tồn tại | message = `"Liên kết không hợp lệ hoặc đã hết hạn"` | P1 |
| UT-AUTH-RESET-06 | `Reset` token `IsUsed = true` | như trên | P1 |
| UT-AUTH-RESET-07 | `Reset` token `ExpiredAt < now` | như trên | P1 |
| UT-AUTH-RESET-08 | `Reset` user không tồn tại | message = `"Tài khoản không tồn tại"` | P2 |
| UT-AUTH-RESET-09 | `Reset` hợp lệ | `PasswordHash` mới; `FailedLoginCount = 0`; `LockoutEndsAt = null`; `SecurityStamp` đổi; token `IsUsed = true`; `RevokeAllForUserAsync` gọi; `_cache.Remove("sstamp:{userId}")` | P1 |
| UT-AUTH-RESET-10 | `Reset` — mật khẩu mới **được hash** | `PasswordHash != newPassword` | P2 |

### 3.6 AuthService — Đăng ký (`UT-AUTH-REGISTER`) ✅ B5

**Hàm:** `RegisterAsync(RegisterRequestDto request)`
**Hạ tầng:** `AppDbContext` (SQLite — transaction), `IUserRepository`, `IStudentRepository`, `IParentRepository`, `IPasswordHasher`, `IBackgroundEmailService`, `IOptions<AppSettings>`.
**Tầng:** U2.

| Mã | Tình huống | Kỳ vọng | P |
|---|---|---|---|
| UT-AUTH-REGISTER-01 | Email tồn tại, **chưa** xác nhận | message = `"Email đã được đăng ký và chờ xác nhận"` | P1 |
| UT-AUTH-REGISTER-02 | Email tồn tại, **đã** xác nhận | message = `"Email đã được đăng ký"` | P1 |
| UT-AUTH-REGISTER-03 | `UserType` = ContentEditor / Reviewer / Finance / Admin | message = `"Không cho phép đăng ký role này"`; transaction **rollback** (không tạo `User`) | P1 |
| UT-AUTH-REGISTER-04 | Student hợp lệ | tạo `User` + `Student{ CurrentGradeLevelId, SchoolName }` + `EmailVerificationToken` (`ExpiredAt ≈ now + 24h`); `QueueConfirmationEmail` gọi; commit | P1 |
| UT-AUTH-REGISTER-05 | Parent hợp lệ | tạo `Parent{ Job, ConnectionCode }` — `ConnectionCode` **đúng 8 ký tự** và **IN HOA** | P1 |
| UT-AUTH-REGISTER-06 | Mật khẩu **được hash** | `User.PasswordHash != request.Password` | P1 |
| UT-AUTH-REGISTER-07 | `SaveChanges` ném giữa chừng | `RollbackAsync` gọi; message = `"Đăng ký thất bại, vui lòng thử lại sau"`; không rò nội bộ | P2 |
| UT-AUTH-REGISTER-08 | Student — email xác nhận gửi **sau** commit | `QueueConfirmationEmail` gọi sau `CommitAsync` | P3 |

### 3.7 AuthService — Validate token (`UT-AUTH-VALIDATE`) ✅ B4

**Hàm:** `Task<bool> ValidateTokenAsync(string token)`
**Fake:** `IJwtService`, `IUserRepository`.
**Tầng:** U1.

| Mã | Tình huống | Kỳ vọng | P |
|---|---|---|---|
| UT-AUTH-VALIDATE-01 | `ValidateToken → null` (chữ ký sai) | `false` | P2 |
| UT-AUTH-VALIDATE-02 | Token OK nhưng `GetUserIdFromToken → null` | `false` | P2 |
| UT-AUTH-VALIDATE-03 | User không tồn tại | `false` | P2 |
| UT-AUTH-VALIDATE-04 | User `IsActive = false` hoặc `LockedAt != null` | `false` | P2 |
| UT-AUTH-VALIDATE-05 | Mọi thứ hợp lệ | `true` | P2 |
| UT-AUTH-VALIDATE-06 | `_jwtService` ném exception | `false` (nuốt lỗi) | P3 |

### 3.8 LoginThrottlePolicy (`UT-THROTTLE`) ✅ B3

**Hàm:** `static TimeSpan? NextLockout(int failedCount)` — leo thang 1, 2, 4, 8… phút, cap 30, bắt đầu từ lần thứ 5.
**Tầng:** U1.

| Mã | `failedCount` | Kỳ vọng | P |
|---|---|---|---|
| UT-THROTTLE-01 | 1 | `null` | P1 |
| UT-THROTTLE-02 | 4 | `null` | P1 |
| UT-THROTTLE-03 | 5 | `TimeSpan.FromMinutes(1)` | P1 |
| UT-THROTTLE-04 | 6 | `2` phút | P1 |
| UT-THROTTLE-05 | 7 | `4` phút | P1 |
| UT-THROTTLE-06 | 8 | `8` phút | P2 |
| UT-THROTTLE-07 | 9 | `16` phút | P2 |
| UT-THROTTLE-08 | 10 | `30` phút (cap, không phải 32) | P1 |
| UT-THROTTLE-09 | 50 | `30` phút | P1 |
| UT-THROTTLE-10 | 0 hoặc số âm | `null` | P3 |

### 3.9 JwtService (`UT-JWT`) ✅ B1

**Hàm:** `GenerateToken(User, int? studentId, int? parentId)`, `ValidateToken(string)`, `GetUserIdFromToken(string)`
**Hạ tầng:** `IConfiguration` in-memory (`JwtSettings:SecretKey/Issuer/Audience/ExpirationMinutes`).
**Tầng:** U1.

| Mã | Tình huống | Kỳ vọng | P |
|---|---|---|---|
| UT-JWT-01 | `SecretKey` rỗng | `GenerateToken` ném `InvalidOperationException` | P1 |
| UT-JWT-02 | Student (`studentId = 7`, `parentId = null`) | token chứa claim `sub`, `email`, `role`, `UserId`, `UserType`, `SecurityStamp`, **`StudentId = 7`**; **không** có `ParentId` | P1 |
| UT-JWT-03 | Parent (`parentId = 9`) | token chứa **`ParentId = 9`**; **không** có `StudentId` | P1 |
| UT-JWT-04 | `User.SecurityStamp = null` | claim `SecurityStamp` = `""` (không văng) | P2 |
| UT-JWT-05 | `role` claim | = `user.UserType.ToString()` (vd `"SystemAdmin"`) | P2 |
| UT-JWT-06 | `ValidateToken` với token do chính service phát | trả `ClaimsPrincipal` khác null | P1 |
| UT-JWT-07 | `ValidateToken` token ký bằng secret khác | `null` | P1 |
| UT-JWT-08 | `ValidateToken` token hết hạn (`ExpirationMinutes = -5`) | `null` | P1 |
| UT-JWT-09 | `ValidateToken` sai `Issuer` | `null` | P2 |
| UT-JWT-10 | `ValidateToken` sai `Audience` | `null` | P2 |
| UT-JWT-11 | `ValidateToken` chuỗi rác (`"abc.def"`) | `null` (không throw) | P2 |
| UT-JWT-12 | `GetUserIdFromToken` token hợp lệ | đúng `UserId` | P2 |
| UT-JWT-13 | `GetUserIdFromToken` token rác | `null` | P3 |
| UT-JWT-14 | `ExpirationMinutes` không parse được | default 60 phút; token có `exp ≈ now + 60′` | P3 |

### 3.10 PasswordHasher (`UT-PWD`) ✅ B1

**Hàm:** `string HashPassword(string)`, `bool VerifyPassword(string, string)` — bọc BCrypt.
**Tầng:** U1.

| Mã | Tình huống | Kỳ vọng | P |
|---|---|---|---|
| UT-PWD-01 | Hash cùng mật khẩu 2 lần | 2 chuỗi **khác nhau** (salt ngẫu nhiên) | P1 |
| UT-PWD-02 | `Verify(pwd, Hash(pwd))` | `true` | P1 |
| UT-PWD-03 | `Verify("sai", Hash("dung"))` | `false` | P1 |
| UT-PWD-04 | `Verify(pwd, "không-phải-bcrypt")` | `false` (không throw) | P1 |
| UT-PWD-05 | `Verify(pwd, "")` / `Verify(pwd, null)` | `false` (không throw) | P2 |
| UT-PWD-06 | Hash mật khẩu Unicode/emoji dài | Verify vẫn `true` | P3 |

### 3.11 SecureTokens (`UT-TOKEN`) ✅ B1

**Hàm:** `static string NewToken()`, `static string Hash(string raw)`
**Tầng:** U1.

| Mã | Tình huống | Kỳ vọng | P |
|---|---|---|---|
| UT-TOKEN-01 | `NewToken()` ký tự | không chứa `+`, `/`, `=` (URL-safe) | P2 |
| UT-TOKEN-02 | `NewToken()` độ dài | ≥ 43 ký tự (256 bit base64url) | P2 |
| UT-TOKEN-03 | `NewToken()` gọi 2 lần | 2 giá trị khác nhau | P2 |
| UT-TOKEN-04 | `Hash(x)` gọi 2 lần | cùng kết quả (ổn định) | P1 |
| UT-TOKEN-05 | `Hash("a") != Hash("b")` | khác nhau | P1 |
| UT-TOKEN-06 | `Hash(x)` không chứa `x` | output ≠ plaintext | P2 |
| UT-TOKEN-07 | `Hash("")` | không throw, trả chuỗi base64 hợp lệ | P3 |

### 3.12 ClaimsPrincipalExtensions (`UT-CLAIMS`) ✅ B1

**Hàm:** `GetUserId`, `GetStudentId`, `GetParentId`, `GetUserType`, `GetEmail`, `HasUserType`, `IsSystemAdmin`
**Tầng:** U1 (dựng `ClaimsPrincipal` thủ công).

| Mã | Tình huống | Kỳ vọng | P |
|---|---|---|---|
| UT-CLAIMS-01 | `GetUserId` có claim `CustomJwtClaims.UserId = "42"` | `42` | P1 |
| UT-CLAIMS-02 | `GetUserId` chỉ có `NameIdentifier = "42"` | `42` (fallback) | P2 |
| UT-CLAIMS-03 | `GetUserId` không claim | `null` | P1 |
| UT-CLAIMS-04 | `GetUserId` claim = `"abc"` | `null` | P2 |
| UT-CLAIMS-05 | `GetStudentId` có / không claim | `id` / `null` | P1 |
| UT-CLAIMS-06 | `GetParentId` có / không claim | `id` / `null` | P1 |
| UT-CLAIMS-07 | `GetUserType` = `"SystemAdmin"` | `UserType.SystemAdmin` | P1 |
| UT-CLAIMS-08 | `GetUserType` = `"NotAType"` | `null` | P1 |
| UT-CLAIMS-09 | `GetEmail` từ `ClaimTypes.Email` hoặc JWT `email` | đúng email | P2 |
| UT-CLAIMS-10 | `HasUserType(Editor, Admin)` khi type = Admin | `true` | P1 |
| UT-CLAIMS-11 | `HasUserType(Editor, Admin)` khi type = Student | `false` | P1 |
| UT-CLAIMS-12 | `IsSystemAdmin` khi type = Admin / Student | `true` / `false` | P2 |
| UT-CLAIMS-13 | `principal == null` — mọi hàm | `null` / `false`, không throw | P1 |

### 3.13 AnswerGrading — chấm điểm (`UT-GRADE`) ✅ B1

**Hàm:** `static (bool isCorrect, bool needsManual) GradeAnswer(Question?, StudentAnswer?)`,
`static bool IsFillBlankCorrect(string? studentText, string? correctAnswer)`,
`static string Normalize(string?)`, `static decimal? TryParseNumeric(string?)`
**Tầng:** U1.

**`GradeAnswer` — MultipleChoice**

| Mã | `Question` | `StudentAnswer` | Kỳ vọng | P |
|---|---|---|---|---|
| UT-GRADE-01 | MC, option đúng `OptionId=1` | `SelectedOptionId = 1` | `(true, false)` | P1 |
| UT-GRADE-02 | MC, option đúng `OptionId=1` | `SelectedOptionId = 2` | `(false, false)` | P1 |
| UT-GRADE-03 | MC | `SelectedOptionId = null` (không chọn) | `(false, false)` | P1 |
| UT-GRADE-04 | MC không có option nào `IsCorrect` | `SelectedOptionId = 1` | `(false, false)` | P2 |

**`GradeAnswer` — TrueFalse**

| Mã | `Question` | `StudentAnswer` | Kỳ vọng | P |
|---|---|---|---|---|
| UT-GRADE-10 | TF, có option, `True`=`OptionId 10` (correct) | `SelectedOptionId = 10` | `(true, false)` | P1 |
| UT-GRADE-11 | TF, có option | `SelectedOptionId = 11` (False) | `(false, false)` | P1 |
| UT-GRADE-12 | TF, có option | `SelectedOptionId = null`, `AnswerText = "true"` | ưu tiên option → `(false, false)` (không có option chọn) | P2 |
| UT-GRADE-13 | TF, **không** option, `CorrectAnswer = "true"` | `AnswerText = "true"` | `(true, false)` | P1 |
| UT-GRADE-14 | TF, không option, `CorrectAnswer = "true"` | `AnswerText = "đúng"` | `(true, false)` | P1 |
| UT-GRADE-15 | TF, không option, `CorrectAnswer = "true"` | `AnswerText = "1"` | `(true, false)` | P1 |
| UT-GRADE-16 | TF, không option, `CorrectAnswer = "true"` | `AnswerText = "yes"` / `"y"` / `"t"` / `"d"` | `(true, false)` | P2 |
| UT-GRADE-17 | TF, không option, `CorrectAnswer = "true"` | `AnswerText = "false"` / `"sai"` / `"0"` / `"n"` / `"s"` | `(false, false)` | P1 |
| UT-GRADE-18 | TF, không option, `CorrectAnswer = "true"` | `AnswerText = "maybe"` (không parse được) | `(false, false)` | P2 |

**`GradeAnswer` — FillBlank & Essay & null**

| Mã | Tình huống | Kỳ vọng | P |
|---|---|---|---|
| UT-GRADE-20 | Essay, `AnswerText` bất kỳ | `(false, needsManual=true)` | P1 |
| UT-GRADE-21 | `question == null` | `(false, false)` | P1 |
| UT-GRADE-22 | `answer == null` | `(false, false)` | P1 |
| UT-GRADE-23 | `QuestionType` ngoài enum (`(QuestionType)99`) | `(false, false)` | P2 |

**`IsFillBlankCorrect(studentText, correctAnswer)`**

| Mã | `correctAnswer` | `studentText` | Kỳ vọng | P |
|---|---|---|---|---|
| UT-GRADE-30 | `"1/2"` | `"0.5"` | `true` | P1 |
| UT-GRADE-31 | `"0.5"` | `"1/2"` | `true` | P1 |
| UT-GRADE-32 | `"1/2"` | `"0,5"` (dấu phẩy thập phân) | `true` | P1 |
| UT-GRADE-33 | `"1/2"` | `" 0.5 "` (khoảng trắng) | `true` | P1 |
| UT-GRADE-34 | `"2"` | `"2.0"` | `true` | P1 |
| UT-GRADE-35 | `"0.5"` | `"0.50"` | `true` | P2 |
| UT-GRADE-36 | `"abc"` | `"abc"` | `true` | P1 |
| UT-GRADE-37 | `"abc"` | `"ABC"` (hoa-thường) | `true` | P1 |
| UT-GRADE-38 | `"1/2"` | `"0.6"` | `false` | P1 |
| UT-GRADE-39 | `"1/2"` | `""` / `null` | `false` | P1 |
| UT-GRADE-40 | `null` / `""` | `"0.5"` | `false` | P1 |
| UT-GRADE-41 | `"2\|two"` | `"two"` (nhiều đáp án, phân tách `\|`) | `true` | P1 |
| UT-GRADE-42 | `"2;hai"` | `"hai"` (phân tách `;`) | `true` | P1 |
| UT-GRADE-43 | `"1.234,56"` (vi) | `"1234.56"` | `true` | P2 |
| UT-GRADE-44 | `"1,234.56"` (en) | `"1234.56"` | `true` | P2 |
| UT-GRADE-45 | `"1/0"` (mẫu số 0) | `"5"` | `false` (không chia-cho-0, không throw) | P2 |
| UT-GRADE-46 | `"3/6"` | `"1/2"` | `true` (cùng giá trị 0.5) | P2 |

**`Normalize` / `TryParseNumeric` trực tiếp**

| Mã | Hàm | Vào | Kỳ vọng | P |
|---|---|---|---|---|
| UT-GRADE-50 | `Normalize` | `"  Hello   World  "` | `"hello world"` | P2 |
| UT-GRADE-51 | `Normalize` | `"1.234,56"` | `"1234.56"` | P2 |
| UT-GRADE-52 | `Normalize` | `null` / `""` | `""` | P2 |
| UT-GRADE-53 | `TryParseNumeric` | `"3/4"` | `0.75m` | P2 |
| UT-GRADE-54 | `TryParseNumeric` | `"abc"` | `null` | P2 |
| UT-GRADE-55 | `TryParseNumeric` | `"5/0"` | `null` | P2 |

### 3.14 SePayContentParser (`UT-SEPAY-PARSE`) ✅ B3

**Hàm:** `static int? TryParseSubscriptionId(string? content)` — regex `SUBSCRIPTION[\-_]?(\d+)`.
**Tầng:** U1.

| Mã | `content` | Kỳ vọng | P |
|---|---|---|---|
| UT-SEPAY-PARSE-01 | `"TKPTTS SUBSCRIPTION_12"` | `12` | P1 |
| UT-SEPAY-PARSE-02 | `"SUBSCRIPTION-7"` | `7` | P1 |
| UT-SEPAY-PARSE-03 | `"SUBSCRIPTION7"` | `7` | P1 |
| UT-SEPAY-PARSE-04 | `"abc SUBSCRIPTION_3 xyz"` | `3` | P1 |
| UT-SEPAY-PARSE-05 | `"subscription_5"` (thường) | `5` (nếu regex `IgnoreCase`) hoặc `null` — chốt hành vi | P2 |
| UT-SEPAY-PARSE-06 | `"chuyen tien hoc phi"` | `null` | P1 |
| UT-SEPAY-PARSE-07 | `"SUBSCRIPTION_"` | `null` | P1 |
| UT-SEPAY-PARSE-08 | `""` / `null` / khoảng trắng | `null` | P1 |
| UT-SEPAY-PARSE-09 | `"SUBSCRIPTION_99999999999999999999"` (tràn `int`) | `null` (không throw) | P2 |
| UT-SEPAY-PARSE-10 | `"SUBSCRIPTION_012"` (số 0 đầu) | `12` | P3 |
| UT-SEPAY-PARSE-11 | 2 lần xuất hiện `"SUBSCRIPTION_1 SUBSCRIPTION_2"` | `1` (khớp đầu tiên) | P3 |

### 3.15 SePayAmountMatcher (`UT-SEPAY-AMOUNT`) ✅ B3

**Hàm:** `static bool Matches(decimal expected, decimal actual, decimal toleranceVnd)`
**Tầng:** U1.

| Mã | (`expected`, `actual`, `tolerance`) | Kỳ vọng | P |
|---|---|---|---|
| UT-SEPAY-AMOUNT-01 | (199000, 199000, 0) | `true` | P1 |
| UT-SEPAY-AMOUNT-02 | (199000, 198999, 0) | `false` | P1 |
| UT-SEPAY-AMOUNT-03 | (199000, 198950, 100) | `true` | P1 |
| UT-SEPAY-AMOUNT-04 | (199000, 198899, 100) | `false` | P1 |
| UT-SEPAY-AMOUNT-05 | (199000, 199100, 100) | `true` (overpay trong dung sai) | P2 |
| UT-SEPAY-AMOUNT-06 | (199000.4, 199000, 0) | `true` (làm tròn `expected`) | P2 |
| UT-SEPAY-AMOUNT-07 | (199000, 0, 0) | `false` | P2 |
| UT-SEPAY-AMOUNT-08 | (199000, 199000, -1) | coi như tolerance 0 → `true` | P3 |

### 3.16 SePayIpnEvaluator (`UT-SEPAY-EVAL`) ✅ B3

**Hàm:** `static IpnOutcome Evaluate(SePayIpnRequest req, Subscription? sub, decimal toleranceVnd)`
**Tầng:** U1 (nhận entity đã nạp, không DB).

| Mã | Tình huống | Kỳ vọng `IpnOutcome` | P |
|---|---|---|---|
| UT-SEPAY-EVAL-01 | `transferType = "out"` | `Ignored` | P1 |
| UT-SEPAY-EVAL-02 | `transferType = "in"`, nội dung không có `SUBSCRIPTION_x` | `Ignored` | P1 |
| UT-SEPAY-EVAL-03 | nội dung hợp lệ nhưng `sub == null` | `Ignored` | P1 |
| UT-SEPAY-EVAL-04 | `sub.Status = Active` | `Duplicate` | P1 |
| UT-SEPAY-EVAL-05 | `sub.Status = Cancelled` | `Ignored` ("no longer payable") | P1 |
| UT-SEPAY-EVAL-06 | `sub.Status = Expired` | `Ignored` | P1 |
| UT-SEPAY-EVAL-07 | `sub.Status = Pending`, số tiền lệch ngoài dung sai | `AmountMismatch` | P1 |
| UT-SEPAY-EVAL-08 | `sub.Status = Pending`, số tiền khớp (trong dung sai) | `Processed` (nên kích hoạt) | P1 |
| UT-SEPAY-EVAL-09 | `transferType` = `"IN"` (hoa) | xử lý như `"in"` — chốt hành vi | P2 |

### 3.17 SePayService — QR & API key (`UT-SEPAY-SVC`) ✅ B3

**Hàm:** `string GenerateQrUrl(int subscriptionId, decimal amount)`, `bool ValidateApiKey(string? apiKey)`
**Hạ tầng:** `IOptions<SePayOptions>` (`BaseUrl`, `VA`, `BankName`, `ApiKeyValidator`), `NullLogger`.
**Tầng:** U1.

| Mã | Tình huống | Kỳ vọng | P |
|---|---|---|---|
| UT-SEPAY-SVC-01 | `GenerateQrUrl(12, 199000)` | URL chứa `acc={VA}`, `bank={BankName}`, `amount=199000` | P1 |
| UT-SEPAY-SVC-02 | `GenerateQrUrl(12, 199000)` — mô tả | `des=` = URL-escape của `"TKPTTS SUBSCRIPTION_12"` | P1 |
| UT-SEPAY-SVC-03 | `GenerateQrUrl(1, 199000.7m)` | `amount=199000` (ép `long`, không `.7`) | P2 |
| UT-SEPAY-SVC-04 | `GenerateQrUrl` prefix | bắt đầu bằng `{BaseUrl}/img?` | P3 |
| UT-SEPAY-SVC-05 | `ValidateApiKey(null)` | `false` | P1 |
| UT-SEPAY-SVC-06 | `ValidateApiKey("")` | `false` | P1 |
| UT-SEPAY-SVC-07 | `ValidateApiKey("sai-key")` | `false` | P1 |
| UT-SEPAY-SVC-08 | `ValidateApiKey("<đúng ApiKeyValidator>")` | `true` | P1 |
| UT-SEPAY-SVC-09 | `ApiKeyValidator` không cấu hình (null/empty) | mọi key → `false` (fail-closed) | P2 |

### 3.18 RateLimitPartitioning (`UT-RLP`) ✅ B1

**Hàm:** `static string ResolveClientKey(HttpContext ctx, IConfiguration config)`
**Hạ tầng:** `DefaultHttpContext` + `ConfigurationBuilder().AddInMemoryCollection(...)`.
**Tầng:** U1.

| Mã | Tình huống | Kỳ vọng | P |
|---|---|---|---|
| UT-RLP-01 | `RemoteIpAddress = 127.0.0.1`, header `X-Client-Key: 1.2.3.4` | `"ck:1.2.3.4"` | P1 |
| UT-RLP-02 | `RemoteIpAddress = 127.0.0.1`, **không** header | `"ip:127.0.0.1"` | P1 |
| UT-RLP-03 | `RemoteIpAddress = 203.0.113.9` (không tin cậy), có header | `"ip:203.0.113.9"` (bỏ qua header) | P1 |
| UT-RLP-04 | header dài 150 ký tự | prefix `"ck:"` + cắt còn 100 ký tự | P2 |
| UT-RLP-05 | `RateLimiting:TrustedProxies = ["10.0.0.5"]`, remote `10.0.0.5`, có header | tin → `"ck:..."` | P1 |
| UT-RLP-06 | `RateLimiting:TrustedProxies = ["10.0.0.5"]`, remote `10.0.0.6`, có header | không tin → `"ip:10.0.0.6"` | P1 |
| UT-RLP-07 | remote `::ffff:127.0.0.1` (IPv4-mapped IPv6) | nhận là loopback → tin header | P2 |
| UT-RLP-08 | `RemoteIpAddress = null` | `"ip:unknown"` | P3 |
| UT-RLP-09 | header tồn tại nhưng giá trị rỗng | fallback `"ip:..."` | P2 |
| UT-RLP-10 | `TrustedProxies` không cấu hình | mặc định chỉ loopback được tin | P2 |

### 3.19 RefundCsvWriter (`UT-CSV`) ✅ B1

**Hàm:** `static byte[] Build(IReadOnlyList<RefundRequest> items, IRefundFieldProtector protector)`
**Fake:** `IRefundFieldProtector` (bản test: `Unprotect(x) => x` hoặc ném theo cấu hình).
**Tầng:** U1.

| Mã | Tình huống | Kỳ vọng | P |
|---|---|---|---|
| UT-CSV-01 | 1 request bình thường | dòng 1 = header `STT,SoTaiKhoan,TenNguoiHuong,MaNganHang,SoTien,NoiDung`; dòng 2 đúng thứ tự cột | P1 |
| UT-CSV-02 | `Amount = 199000.6m` | `SoTien = "199001"` (làm tròn away-from-zero, kiểu nguyên) | P1 |
| UT-CSV-03 | `Amount = 199000.4m` | `SoTien = "199000"` | P2 |
| UT-CSV-04 | `BankAccountHolderName = "=SUM(A1)"` | ô tên = `"'=SUM(A1)"` (thêm `'` đầu — chống formula-injection) | P1 |
| UT-CSV-05 | tên bắt đầu `+`, `-`, `@`, TAB, CR | đều được thêm `'` | P1 |
| UT-CSV-06 | tên `"Nguyen, Van A"` (có dấu phẩy) | ô được bọc `"Nguyen, Van A"` | P1 |
| UT-CSV-07 | tên `"Nguyen \"Tai\" A"` (có nháy kép) | trong ô bọc, `"` → `""` | P2 |
| UT-CSV-08 | tên `"Nguyễn Văn Đức"` (có dấu) | ASCII bỏ dấu: `"Nguyen Van Duc"`; `đ/Đ → d/D` | P1 |
| UT-CSV-09 | `ReasonCode = "CustomerRequest"`, `PublicId` guid | `NoiDung = "HOAN TIEN CustomerRequest REF {publicId:N}"` (ASCII) | P2 |
| UT-CSV-10 | `protector.Unprotect` ném exception | ô `SoTaiKhoan` = `"DECRYPT_ERROR"` (không làm hỏng cả file) | P2 |
| UT-CSV-11 | 3 requests | cột `STT` = `1`, `2`, `3` | P2 |
| UT-CSV-12 | kết thúc dòng | mọi dòng kết thúc `\r\n` (CRLF) | P2 |
| UT-CSV-13 | mã hoá byte | UTF-8 **không BOM** (byte đầu ≠ `0xEF`) | P2 |
| UT-CSV-14 | `items` rỗng | chỉ có dòng header | P3 |

### 3.20 RefundCompletion (`UT-RFC`) ✅ B3

**Hàm (sau refactor):** `static void Apply(Payment paymentWithSubscription, RefundRequest request)`
**Tầng:** U1 (thao tác entity trong bộ nhớ).

| Mã | Payment | Request | Kỳ vọng | P |
|---|---|---|---|---|
| UT-RFC-01 | `Amount=199k`, `RefundAmount=null`, có `Subscription(Active)` | `Amount=199k` | `RefundAmount=199k`; `Status=Refunded`; `RefundedAt` set; `Subscription.Status=Cancelled` | P1 |
| UT-RFC-02 | `Amount=199k`, `RefundAmount=null` | `Amount=100k` | `RefundAmount=100k`; `Status=PartiallyRefunded`; `Subscription` **giữ** `Active` | P1 |
| UT-RFC-03 | `Amount=199k`, `RefundAmount=100k` | `Amount=99k` | tích luỹ `199k` → `Status=Refunded` | P1 |
| UT-RFC-04 | full refund, `Subscription(Pending)` | `Amount=199k` | `Subscription.Status=Cancelled` | P2 |
| UT-RFC-05 | full refund, `Subscription(Expired)` | `Amount=199k` | `Subscription` **không đổi** (`Expired`) | P2 |
| UT-RFC-06 | full refund, `Subscription = null` | `Amount=199k` | không throw; Payment vẫn `Refunded` | P2 |
| UT-RFC-07 | `Amount=199k`, `RefundAmount=null` | `Amount=250k` (vượt) | `RefundAmount=250k`, `Status=Refunded` (chốt: hàm không tự chặn — validate ở service) | P3 |

### 3.21 RefundDayWindow (`UT-RFP-WINDOW`) ✅ B3

**Hàm (sau refactor):** `static DateTime StartOfDayUtc(DateTime nowUtc, int offsetHours)` — mốc 00:00 theo giờ địa phương, trả về UTC.
**Tầng:** U1.

| Mã | `nowUtc` | `offsetHours` | Kỳ vọng | P |
|---|---|---|---|---|
| UT-RFP-WINDOW-01 | `2026-09-05T02:00:00Z` | 7 | `2026-09-04T17:00:00Z` (00:00 ngày 05 giờ VN) | P1 |
| UT-RFP-WINDOW-02 | `2026-09-05T20:00:00Z` | 7 | `2026-09-05T17:00:00Z` | P1 |
| UT-RFP-WINDOW-03 | `2026-09-05T16:59:00Z` | 7 | `2026-09-04T17:00:00Z` (chưa qua nửa đêm VN) | P1 |
| UT-RFP-WINDOW-04 | `2026-09-05T17:00:00Z` | 7 | `2026-09-05T17:00:00Z` (đúng nửa đêm VN) | P2 |
| UT-RFP-WINDOW-05 | bất kỳ | 0 | mốc UTC 00:00 cùng ngày | P2 |

### 3.22 RefundService — chính sách duyệt (`UT-RFP`) ✅ B5

**Hàm:** `CreateAsync`, `ApproveAsync` (các nhánh kiểm soát)
**Hạ tầng:** `AppDbContext` (SQLite — `RefundRequests`, `Payments`), `ISystemConfigService` (fake trả cấu hình), `IRefundEventWriter` (fake).
**Tầng:** U2.

| Mã | Tình huống | Kỳ vọng | P |
|---|---|---|---|
| UT-RFP-01 | `usedToday + amount ≤ dailyCapVnd` | duyệt OK, `Status=Approved`, `ApprovedAt` set | P1 |
| UT-RFP-02 | `usedToday + amount > dailyCapVnd` | lỗi `400`, message chứa `"Vượt trần"`; `Status` không đổi | P1 |
| UT-RFP-03 | `usedToday` chỉ cộng request có `Status ∈ {Approved, Batched, Disbursed, Completed}` và `ApprovedAt ≥ mốc ngày` | request `PendingReview` / hôm qua **không** tính | P1 |
| UT-RFP-04 | per-user: đếm `RefundRequest` theo **beneficiary** trong 30 ngày trượt, trừ `Rejected`/`Cancelled` | đúng số | P1 |
| UT-RFP-05 | `count ≥ maxRequestsPerUserPer30d` khi `CreateAsync` | `409` | P1 |
| UT-RFP-06 | người tạo là `SystemAdmin` | bỏ qua giới hạn per-user | P2 |
| UT-RFP-07 | Finance tạo hộ | **vẫn** tính vào giới hạn của beneficiary | P2 |
| UT-RFP-08 | `dualControlThresholdVnd = 0` | duyệt 1 lần → `Approved` | P1 |
| UT-RFP-09 | `amount ≥ dualControlThresholdVnd > 0`, duyệt lần 1 | `Status=PendingSecondApproval`, `FirstApprovedByUserId` set, **chưa** tiêu trần/ngày | P1 |
| UT-RFP-10 | duyệt lần 2 bởi **cùng người** | `409` | P1 |
| UT-RFP-11 | duyệt lần 2 bởi người Finance khác | `Approved`, lúc này mới kiểm trần/ngày | P1 |
| UT-RFP-12 | `CreateAsync` cho payment `Status != Completed` | `400` "Chỉ hoàn tiền được giao dịch đã Completed" | P1 |
| UT-RFP-13 | `amount` > (Payment.Amount − RefundAmount hiện có) | `400` | P1 |
| UT-RFP-14 | payment cũ hơn `maxPaymentAgeDays` | `400` | P2 |
| UT-RFP-15 | đã có RefundRequest đang mở cho payment đó | `409` | P1 |
| UT-RFP-16 | `reject` rồi `approve` | state machine chặn → `409` | P1 |

### 3.23 RefundFieldProtector (`UT-PROT`) ✅ B1

**Hàm:** `string Protect(string)`, `string Unprotect(string)`, `string Last4(string)`
**Hạ tầng:** `Last4` = U1 (thuần); `Protect/Unprotect` = U1 với `DataProtectionProvider.Create("tests")` (ephemeral, in-memory).
**Tầng:** U1.

| Mã | Hàm | Vào | Kỳ vọng | P |
|---|---|---|---|---|
| UT-PROT-01 | `Last4` | `"0071000123456"` | `"3456"` | P1 |
| UT-PROT-02 | `Last4` | `"12-34 56 78"` (có ký tự lạ) | `"5678"` (chỉ giữ chữ số) | P1 |
| UT-PROT-03 | `Last4` | `"123"` (< 4 chữ số) | `"123"` | P2 |
| UT-PROT-04 | `Last4` | `""` / `null` | `""` (không throw) | P2 |
| UT-PROT-05 | `Unprotect(Protect(x))` | `"0071000123456"` | `"0071000123456"` (round-trip) | P1 |
| UT-PROT-06 | `Protect(x)` | `"12345"` | ciphertext ≠ plaintext | P1 |
| UT-PROT-07 | `Unprotect` chuỗi rác | ném exception (caller bắt) | P2 |
| UT-PROT-08 | 2 protector khác purpose/key | không giải mã chéo được | P3 |

### 3.24 ContentAccessService — Covers (`UT-GATE-COVERS`) ✅ B3

**Hàm (sau refactor `internal`):** `static bool Covers(PackageEntitlement e, Course course)`
**Tầng:** U1.

| Mã | `e.ScopeType` | Thiết lập | Kỳ vọng | P |
|---|---|---|---|---|
| UT-GATE-COVERS-01 | `AllContent` | course bất kỳ | `true` | P1 |
| UT-GATE-COVERS-02 | `Subject` | `e.SubjectId == course.SubjectId` | `true` | P1 |
| UT-GATE-COVERS-03 | `Subject` | `e.SubjectId != course.SubjectId` | `false` | P1 |
| UT-GATE-COVERS-04 | `Grade` | `e.GradeLevelId == course.GradeLevelId` | `true` | P1 |
| UT-GATE-COVERS-05 | `Grade` | khác grade | `false` | P1 |
| UT-GATE-COVERS-06 | `SubjectGrade` | khớp cả `SubjectId` và `GradeLevelId` | `true` | P1 |
| UT-GATE-COVERS-07 | `SubjectGrade` | khớp subject, lệch grade | `false` | P1 |
| UT-GATE-COVERS-08 | `SubjectGrade` | lệch subject, khớp grade | `false` | P1 |

### 3.25 ContentAccessService — GetCourseAccess (`UT-GATE`) ✅ B5

**Hàm:** `Task<ContentAccessLevel> GetCourseAccessAsync(ClaimsPrincipal user, Course course)`
**Fake:** `IEnrollmentRepository` (`GetActiveEnrolmentAsync`, `GetActiveEntitlementsAsync`).
**Tầng:** U2 (không DB thật, chỉ fake repo + `ClaimsPrincipal` dựng tay).

| Mã | `course.Status` | `user` | Enrolment / Entitlement | Kỳ vọng | P |
|---|---|---|---|---|---|
| UT-GATE-01 | `Draft` | ContentEditor | — | `Full` | P1 |
| UT-GATE-02 | `Draft` | Student | — | `None` | P1 |
| UT-GATE-03 | `Draft` | ẩn danh | — | `None` | P2 |
| UT-GATE-04 | `Published` | ContentEditor / AcademicReviewer / SystemAdmin | — | `Full` | P1 |
| UT-GATE-05 | `Published` | ẩn danh (không `studentId`) | — | `FreeOnly` | P1 |
| UT-GATE-06 | `Published` | Parent | — | `FreeOnly` | P2 |
| UT-GATE-07 | `Published` | Student | có active enrolment | `Full` | P1 |
| UT-GATE-08 | `Published` | Student | entitlement `SubjectGrade` khớp | `Full` | P1 |
| UT-GATE-09 | `Published` | Student | entitlement `AllContent` | `Full` | P2 |
| UT-GATE-10 | `Published` | Student | không enrolment, entitlement môn khác | `FreeOnly` | P1 |
| UT-GATE-11 | `Published` | Student | entitlement đã hết hạn (repo không trả) | `FreeOnly` | P2 |

### 3.26 EnrollmentService (`UT-ENROL`) ✅ B4

**Hàm:** `Task<ApiResponse<EnrolmentDto>> EnrollAsync(int studentId, int courseId)`, `GetMyEnrolmentsAsync`
**Fake:** `IEnrollmentRepository`, `ICourseRepository`, `IContentAccessService`.
**Tầng:** U2 (hoặc U1 nếu repo hoàn toàn fake được).

| Mã | Tình huống | Kỳ vọng | P |
|---|---|---|---|
| UT-ENROL-01 | Course không tồn tại | `Success=false`, `"Không tìm thấy khoá học"` | P1 |
| UT-ENROL-02 | Course `Status != Published` | `"Khoá học chưa được xuất bản"` | P1 |
| UT-ENROL-03 | Course không có `CourseVersion` state `Published` | `"Khoá học chưa có phiên bản xuất bản"` | P1 |
| UT-ENROL-04 | Đã có active enrolment | `Success=true`, message `"Already enrolled"`, **không** tạo bản ghi mới | P1 |
| UT-ENROL-05 | Ghi danh mới hợp lệ | tạo enrolment; trả `EnrolmentDto` | P1 |
| UT-ENROL-06 | Ghi danh khi đã có entitlement (gói) | vẫn tạo enrolment (chốt hành vi theo code) | P2 |
| UT-ENROL-07 | `GetMyEnrolmentsAsync` | map đúng danh sách từ repo | P2 |

### 3.27 PagedRequest / PagingExtensions (`UT-PAGE`) ✅ B1

**Hàm:** `PagedRequest.NormPage`, `NormPageSize`; `PagingExtensions.Map`; `ToPagedResultAsync`
**Tầng:** U1 (`NormPage/NormPageSize/Map`), U2 (`ToPagedResultAsync` với SQLite hoặc list `AsQueryable`).

| Mã | Tình huống | Kỳ vọng | P |
|---|---|---|---|
| UT-PAGE-01 | `Page = 0` | `NormPage = 1` | P1 |
| UT-PAGE-02 | `Page = -3` | `NormPage = 1` | P1 |
| UT-PAGE-03 | `Page = 5` | `NormPage = 5` | P2 |
| UT-PAGE-04 | `PageSize = 500` | `NormPageSize = 100` | P1 |
| UT-PAGE-05 | `PageSize = 0` | `NormPageSize = 1` | P1 |
| UT-PAGE-06 | `PageSize = 20` (mặc định) | `NormPageSize = 20` | P3 |
| UT-PAGE-07 | `Map` | `Items` biến đổi qua `map`; `Total`, `Page`, `PageSize` giữ nguyên | P2 |
| UT-PAGE-08 | `ToPagedResultAsync` với 25 phần tử, page 2, size 10 | `Items.Count = 10`, `Total = 25`, `Page = 2` | P2 |
| UT-PAGE-09 | `ToPagedResultAsync` page 3, size 10, 25 phần tử | `Items.Count = 5` | P2 |

### 3.28 AiQuotaService (`UT-QUOTA`) ✅ B5

**Hàm:** `PeekHintAsync(int studentId)`, `TryConsumeHintAsync(int studentId)`, `RecordFeedbackAsync(int studentId)`
**Hạ tầng:** `AppDbContext` (SQLite — `AiUsageDaily`, `Subscriptions`, `Packages`), fake `IPackageRepository`, `ISystemConfigService`, `IConfiguration` (`AI:FreeDailyHintLimit`).
**Tầng:** U2.

| Mã | Tình huống | Kỳ vọng | P |
|---|---|---|---|
| UT-QUOTA-01 | Free, chưa dùng hôm nay, limit 3 | `PeekHint`: `Allowed=true`, `Used=0`, `Limit=3`, `Unlimited=false` | P1 |
| UT-QUOTA-02 | Free, `HintCount = 2`, limit 3 | `PeekHint.Allowed = true` | P1 |
| UT-QUOTA-03 | Free, `HintCount = 3`, limit 3 | `PeekHint.Allowed = false` | P1 |
| UT-QUOTA-04 | Gói unlimited (Premium/Yearly) | `PeekHint.Allowed = true` bất kể `Used` | P1 |
| UT-QUOTA-05 | `TryConsumeHint` khi còn hạn | `HintCount + 1` được lưu; trả `Allowed=true` | P1 |
| UT-QUOTA-06 | `TryConsumeHint` khi `HintCount == limit` | `Allowed=false`, `HintCount` **không** tăng | P1 |
| UT-QUOTA-07 | `TryConsumeHint` unlimited | luôn `Allowed=true`, vẫn tăng `HintCount` (để thống kê) | P2 |
| UT-QUOTA-08 | Có bản ghi `AiUsageDaily` của **hôm qua** | hôm nay tạo bản ghi mới, đếm từ 0 | P1 |
| UT-QUOTA-09 | `SystemConfig` không có key giới hạn | dùng `AI:FreeDailyHintLimit` từ config | P2 |
| UT-QUOTA-10 | `AI:FreeDailyHintLimit` cũng không parse được | fallback = 3 | P2 |
| UT-QUOTA-11 | `RecordFeedbackAsync` | `FeedbackCount + 1` được lưu | P3 |

### 3.29 SubscriptionLifecycleService (`UT-LIFE`) ✅ B5

**Hàm:** `Task<LifecycleSweepResult> RunSweepAsync()`
**Hạ tầng:** `AppDbContext` (SQLite — `Subscriptions`, `Payments`), `IOptions<SePayOptions>` (`PendingTimeoutMinutes`), `NullLogger`.
**Tầng:** U2.

| Mã | Tình huống | Kỳ vọng | P |
|---|---|---|---|
| UT-LIFE-01 | `Subscription(Active)`, `EndDate = now - 1h` | → `Expired` | P1 |
| UT-LIFE-02 | `Subscription(Active)`, `EndDate = now + 10d` | giữ `Active` | P1 |
| UT-LIFE-03 | `Subscription(Pending)`, `CreatedAt = now - 45'`, timeout 30' | → `Cancelled`; `Payment` liên quan → `Failed` | P1 |
| UT-LIFE-04 | `Subscription(Pending)`, `CreatedAt = now - 5'` | giữ `Pending` | P1 |
| UT-LIFE-05 | không có gì cần xử lý | `LifecycleSweepResult` = (0, 0); không lỗi | P2 |
| UT-LIFE-06 | nhiều bản ghi mỗi loại | `result` đếm đúng số expired / cancelled | P2 |
| UT-LIFE-07 | `Subscription(Cancelled/Expired)` sẵn | không bị đụng | P3 |

### 3.30 PackageTierResolver (`UT-TIER`) ✅ B5

**Hàm (sau refactor):** `Task<PackageTier> ResolveAsync(int studentId)`
**Hạ tầng:** `AppDbContext` (SQLite — `Subscriptions` + `Packages`).
**Tầng:** U2.

| Mã | Tình huống | Kỳ vọng | P |
|---|---|---|---|
| UT-TIER-01 | Không subscription | `Free` | P1 |
| UT-TIER-02 | 1 `Active`, `EndDate > now`, gói `Standard` | `Standard` | P1 |
| UT-TIER-03 | 2 `Active` cùng lúc: `Standard` + `Premium` | `Premium` (tier cao nhất) | P1 |
| UT-TIER-04 | `Active` nhưng `EndDate = now - 1d` | `Free` (đã hết hạn thực tế) | P1 |
| UT-TIER-05 | chỉ có `Expired` / `Cancelled` / `Pending` | `Free` | P1 |
| UT-TIER-06 | 2 `Active` cùng tier, khác `EndDate` | chọn cái `EndDate` xa hơn (không ảnh hưởng tier nhưng chốt thứ tự) | P3 |

### 3.31 ProgressProjectionService (`UT-PROG`) ✅ B3 (U1 `ProgressRollup`) · B5 (U2: PROG-06..09; PROG-05 phủ gián tiếp qua PROG-08)

**Hằng số:** `LessonCompleteScorePct = 70`, `MinViewSeconds = 20`.
**Hàm tính (sau refactor `ProgressRollup`):** % hoàn thành từ danh sách con; ngưỡng đánh dấu bài hoàn thành.
**Tầng:** U1 (phần tính) · U2 (`ProjectAttemptAsync`, `MarkLessonCompleteAsync` với SQLite).

| Mã | Tình huống | Kỳ vọng | T | P |
|---|---|---|---|---|
| UT-PROG-01 | Attempt điểm `70%` gắn với bài học | bài → hoàn thành | U1 | P1 |
| UT-PROG-02 | Attempt điểm `69%` | bài **chưa** hoàn thành | U1 | P1 |
| UT-PROG-03 | 3/4 node con hoàn thành | % cha = `75` | U1 | P1 |
| UT-PROG-04 | 0/4 node con | % cha = `0` | U1 | P2 |
| UT-PROG-05 | `MaterializedPath` của node = `"/1/5/"` (gồm **chính id** `5`) | truy vấn con dùng `StartsWith("/1/5/")` — **không** ghép `+ "5/"` | U1 | P1 |
| UT-PROG-06 | `MarkLessonComplete` với view time `< 20s` | không set `100%`, trả lỗi/không đổi | U2 | P2 |
| UT-PROG-07 | `MarkLessonComplete` với view time `≥ 20s` | `NodeProgress = 100%` + roll-up | U2 | P1 |
| UT-PROG-08 | roll-up leo 3 cấp (bài → chương → version) | mỗi cấp % đúng | U2 | P1 |
| UT-PROG-09 | `ProjectAttemptAsync` với attempt chưa `Submitted` | không ghi progress | U2 | P2 |

### 3.32 ResourceAccessService — quyền truy cập (`UT-RES`) ✅ B4

**Hàm:** `CanAccessStudentAsync(int studentId, int userId, UserType?)`, `CanAccessAttemptAsync`, `CanAccessPaymentAsync`, `CanAccessSubscriptionAsync`
**Fake:** `IStudentRepository`, `IParentRepository`, `IParentLinkRepository`, `IExerciseAttemptRepository`, `ISubscriptionRepository`, `IPaymentRepository`.
**Tầng:** U1 (repo fake hoàn toàn).

| Mã | Tình huống | Kỳ vọng | P |
|---|---|---|---|
| UT-RES-01 | `userType = SystemAdmin` | `CanAccessStudent → true` ngay (không tra repo) | P1 |
| UT-RES-02 | Student không tồn tại | `false` | P1 |
| UT-RES-03 | Chính học sinh đó (`student.UserId == userId`) | `true` | P1 |
| UT-RES-04 | Phụ huynh có `ParentLink(Active)` tới học sinh | `true` | P1 |
| UT-RES-05 | Phụ huynh có link nhưng `Status = Revoked/Pending` | `false` | P1 |
| UT-RES-06 | Phụ huynh **không** link | `false` | P1 |
| UT-RES-07 | Học sinh khác (`userId` là student B) | `false` | P1 |
| UT-RES-08 | `CanAccessPaymentAsync` — người trả (`PaidByUserId == userId`) | `true` | P1 |
| UT-RES-09 | `CanAccessPaymentAsync` — beneficiary student (qua `StudentId`) | `true` | P1 |
| UT-RES-10 | `CanAccessPaymentAsync` — người lạ | `false` | P1 |
| UT-RES-11 | `CanAccessAttemptAsync` — chủ attempt / phụ huynh link / người lạ | `true` / `true` / `false` | P1 |
| UT-RES-12 | `CanAccessSubscriptionAsync` — chủ / Finance / người lạ | `true` / `true` / `false` | P2 |

### 3.33 SystemConfigService (`UT-CFG`) ✅ B5

**Hàm:** `GetIntAsync(key, fallback)`, `GetDecimalAsync`, `GetBoolAsync`, `GetStringAsync`
**Hạ tầng:** `AppDbContext` (SQLite — `SystemConfigs`), `IMemoryCache` (`MemoryCache` thật).
**Tầng:** U2.

| Mã | Tình huống | Kỳ vọng | P |
|---|---|---|---|
| UT-CFG-01 | Key có giá trị `"42"` | `GetIntAsync → 42` | P1 |
| UT-CFG-02 | Key không tồn tại | trả `fallback` | P1 |
| UT-CFG-03 | Key có giá trị không parse được (`"abc"` cho `GetIntAsync`) | trả `fallback` | P1 |
| UT-CFG-04 | `GetDecimalAsync` với `"20000000.50"` (InvariantCulture) | `20000000.50m` | P1 |
| UT-CFG-05 | `GetDecimalAsync` với `"20.000.000"` (định dạng vi) | `fallback` (không dùng culture vi) | P2 |
| UT-CFG-06 | `GetBoolAsync` với `"true"` / `"True"` / `"1"` | `true` / `true` / `fallback` (`bool.TryParse` không nhận `"1"`) | P2 |
| UT-CFG-07 | `GetStringAsync` key rỗng | `fallback` | P2 |
| UT-CFG-08 | Gọi 2 lần cùng key | lần 2 lấy từ cache (không query DB) — verify qua context tracking hoặc bộ đếm | P2 |
| UT-CFG-09 | Cache hết hạn (TTL 5') | query lại DB | P3 |

### 3.34 NotificationService & NotificationRules (`UT-NOTIF`) ✅ B5

**Hàm:** `GetMineAsync`, `GetUnreadCountAsync`, `MarkReadAsync`, `MarkAllReadAsync`, `NotificationRules.All`
**Hạ tầng:** `AppDbContext` (SQLite — `Notifications`).
**Tầng:** U2 (service) · U1 (`NotificationRules`).

| Mã | Tình huống | Kỳ vọng | P |
|---|---|---|---|
| UT-NOTIF-01 | `NotificationRules.All` | chứa đúng `"tab-switch"`, `"low-score"`, `"inactivity"` | P2 |
| UT-NOTIF-02 | `GetMineAsync` chỉ trả thông báo của `userId` | không lẫn của user khác | P1 |
| UT-NOTIF-03 | `GetMineAsync` `unreadOnly = true` | chỉ `IsRead = false` | P1 |
| UT-NOTIF-04 | `GetMineAsync` sắp xếp | `CreatedAt` giảm dần | P2 |
| UT-NOTIF-05 | `GetMineAsync` phân trang | `page`/`pageSize` clamp `[1..100]`; `Total` đúng | P2 |
| UT-NOTIF-06 | `GetUnreadCountAsync` | đếm đúng số `IsRead = false` của user | P1 |
| UT-NOTIF-07 | `MarkReadAsync` id không thuộc user | `"Không tìm thấy thông báo"` | P1 |
| UT-NOTIF-08 | `MarkReadAsync` hợp lệ, đang unread | `IsRead = true`, `ReadAt` set, `SaveChanges` gọi | P1 |
| UT-NOTIF-09 | `MarkReadAsync` đã read sẵn | không đổi `ReadAt`, vẫn success | P2 |
| UT-NOTIF-10 | `MarkAllReadAsync` | mọi thông báo unread của user → read; message chứa số lượng | P1 |

### 3.35 ExerciseAttemptService (`UT-ATT`) ✅ B5

**Hàm:** `StartAttemptAsync`, `SaveAnswerAsync`, `CompleteAttemptAsync`, `StartRandomAsync`
**Hạ tầng:** SQLite + nhiều repo fake + `IAiFeedbackQueue` fake + `IProgressProjectionService` fake + `INotificationRuleEngine` fake.
**Tầng:** U2.

| Mã | Tình huống | Kỳ vọng | P |
|---|---|---|---|
| UT-ATT-01 | `Start` exercise `Status != Published` | lỗi | P1 |
| UT-ATT-02 | `Start` khi tier không đủ (Test/Exam cần gói) | **403** (không phải 400) | P1 |
| UT-ATT-03 | `Start` khi đã đạt `MaxAttempts` | `400`, message chứa `"attempt"` | P1 |
| UT-ATT-04 | `Start` hợp lệ | tạo `ExerciseAttempt(InProgress)`, `PlannedEndTime` set nếu có `DurationMinutes` | P1 |
| UT-ATT-05 | `SaveAnswer` cùng câu 2 lần | bản ghi `StudentAnswer` được **ghi đè**, không nhân đôi | P1 |
| UT-ATT-06 | `SaveAnswer` cho attempt không phải `InProgress` | lỗi | P2 |
| UT-ATT-07 | `Complete` — chấm tự động MC/TF/FillBlank | `TotalScore`, `CorrectAnswers` đúng | P1 |
| UT-ATT-08 | `Complete` — có câu Essay | `HasPendingManualGrading = true`, Essay `NeedsManualGrading = true`, **không** tính là sai | P1 |
| UT-ATT-09 | `Complete` — đẩy job AI feedback | `IAiFeedbackQueue.Enqueue` được gọi cho câu sai; response **không** chờ AI | P1 |
| UT-ATT-10 | `Complete` — gọi `IProgressProjectionService.ProjectAttemptAsync` | được gọi 1 lần | P2 |
| UT-ATT-11 | `Complete` 2 lần | lần 2 không chấm lại (idempotent ở tầng service — bổ trợ cho row-lock DB) | P2 |
| UT-ATT-12 | `StartRandom` từ ngân hàng, `NumberOfQuestions = 2` | attempt có đúng 2 câu; không "timeout ảo" | P1 |
| UT-ATT-13 | `Complete` sau `PlannedEndTime` | vẫn chấm (không mất bài) | P2 |

### 3.36 Authorization attributes (`UT-ATTR`)

**Lớp:** `AuthorizeUserTypeAttribute`, `AuthorizeContentRoleAttribute`, `SePayApiKeyAttribute`
**Hạ tầng:** dựng `AuthorizationFilterContext` / `ActionExecutingContext` với `DefaultHttpContext` + `ClaimsPrincipal` giả.
**Tầng:** U1.

| Mã | Tình huống | Kỳ vọng | P |
|---|---|---|---|
| UT-ATTR-01 | `AuthorizeUserType(Finance, Admin)` — user là Finance | không set `Result` (cho qua) | P1 |
| UT-ATTR-02 | `AuthorizeUserType(Finance, Admin)` — user là Student | `Result` = 403 `ForbidResult`/envelope | P1 |
| UT-ATTR-03 | `AuthorizeUserType` — chưa đăng nhập | 401 | P1 |
| UT-ATTR-04 | `AuthorizeContentRole` — ContentEditor / Reviewer / Admin | cho qua | P1 |
| UT-ATTR-05 | `AuthorizeContentRole` — Student / Parent | 403 | P1 |
| UT-ATTR-06 | `SePayApiKey` — header `Authorization: Apikey <đúng>` | cho qua | P1 |
| UT-ATTR-07 | `SePayApiKey` — header sai / thiếu | 401 | P1 |
| UT-ATTR-08 | `SePayApiKey` — sai scheme (`Bearer xxx`) | 401 | P2 |

### 3.37 CorrelationIdMiddleware & GlobalExceptionHandler (`UT-MW`)

**Lớp:** `CorrelationIdMiddleware`, `GlobalExceptionHandler`
**Hạ tầng:** `DefaultHttpContext`, `RequestDelegate` giả.
**Tầng:** U1.

| Mã | Tình huống | Kỳ vọng | P |
|---|---|---|---|
| UT-MW-01 | Request **không** có header `X-Correlation-Id` | response có header `X-Correlation-Id` (GUID mới); `HttpContext.Items`/`TraceIdentifier` mang giá trị | P1 |
| UT-MW-02 | Request **có** `X-Correlation-Id: abc-123` | response echo lại đúng `abc-123` | P1 |
| UT-MW-03 | `GlobalExceptionHandler` bắt exception thường | trả 500 + envelope `ApiResponse`; **không** chứa `ex.Message` / stack trace | P1 |
| UT-MW-04 | `GlobalExceptionHandler` — body là JSON hợp lệ, `StatusCode = 500` | `Content-Type: application/json` | P2 |
| UT-MW-05 | `GlobalExceptionHandler` — có correlation id | id xuất hiện trong body hoặc header để trace | P2 |

### 3.38 Validation DTO (`UT-DTO`)

**Cách test:** `Validator.TryValidateObject(dto, ctx, results, validateAllProperties: true)`.
**Tầng:** U1.

| Mã | DTO | Tình huống | Kỳ vọng | P |
|---|---|---|---|---|
| UT-DTO-01 | `LoginRequestDto` | `Email = ""` | invalid | P2 |
| UT-DTO-02 | `LoginRequestDto` | `Email = "not-an-email"` | invalid (nếu có `[EmailAddress]`) | P2 |
| UT-DTO-03 | `LoginRequestDto` | `Password = ""` | invalid | P2 |
| UT-DTO-04 | `RegisterRequestDto` | mật khẩu ngắn hơn `MinLength` | invalid | P2 |
| UT-DTO-05 | `RegisterRequestDto` | đủ field bắt buộc + `UserType` hợp lệ | valid | P2 |
| UT-DTO-06 | `ChangePasswordDto` | thiếu `CurrentPassword` hoặc `NewPassword` | invalid | P2 |
| UT-DTO-07 | `CreateRefundRequestDto` | thiếu `PaymentId` | invalid | P2 |
| UT-DTO-08 | `CreateRefundRequestDto` | `BankBin` sai định dạng (không phải 6 số) nếu có regex | invalid | P3 |
| UT-DTO-09 | `CreateSubscriptionDto` | thiếu `PackageId` / `StudentId` | invalid | P3 |
| UT-DTO-10 | `ResetPasswordDto` | thiếu `Token` / `NewPassword` | invalid | P3 |

---

## 4. Ánh xạ Unit → Integration

Unit test khoá **logic quyết định**; integration test khoá **hành vi tích hợp** (HTTP status,
phân quyền thật, transaction, unique index, EF provider Postgres). Không cái nào thay được cái nào.

| Luồng | Unit đã khoá | Integration **vẫn** phải khẳng định |
|---|---|---|
| F1 Auth | nhánh `LoginAsync`, throttle math, xoay/tái dùng token, hash mật khẩu, JWT claim | 401 vs 302, envelope 429 tiếng Việt, rate-limit theo `X-Client-Key`, `SecurityStamp` chặn access token đang bay, email thật qua SendGrid sandbox |
| F2/F3 Nội dung | `Covers`, `GetCourseAccess`, roll-up %, `EnrollAsync` nhánh | cắt cây thật qua `/api/learn`, khoá sửa sau publish, `MaterializedPath` sau move, 3 tầng entitlement end-to-end |
| F4 Bài tập | ma trận chấm điểm, `MaxAttempts`, feedback enqueue, ghi đè answer | row-lock chống chấm 2 lần (Postgres), quyền chủ/phụ huynh, feedback nền hoàn tất, tab-switch debounce |
| F5 Dashboard | (ít logic thuần) | số liệu roll-up thật, 403 `upgradeRequired`, owner guard, N+1 |
| F6 Phụ huynh | `ResourceAccessService` (link Active) | mời qua email, thu hồi mất quyền ngay, connection code |
| F7 Thanh toán | parse nội dung, so tiền, ma trận outcome, QR URL | unique index `ReferenceCode` (idempotency), transaction lồng, guard "1 Active", luôn 200, `[SePayApiKey]` 401, sweep |
| F8 Hoàn tiền | CSV injection, `RefundCompletion`, mốc trần/ngày, `Last4` | dual-control 2 người thật, state machine qua API, `RefundEvent` + `AuditLog`, giải mã số TK, role gate |
| F9 AI/Chat | `AiQuotaService` hạn mức/reset | 429 envelope, feedback nền, chatbot lưu khi Flask down, health 503 |
| F10 Thông báo | `NotificationService` list/read, `NotificationRules` | rule engine sinh notif (tab-switch/low-score/inactivity), gửi cả phụ huynh, opt-out |
| F12 Hợp đồng | middleware correlation id, exception handler, `PagedRequest` clamp | envelope toàn hệ, enum-string, route kebab, health, audit interceptor |

---

## 5. Bố cục thư mục

Dự án test **mới** `ELearning_ToanHocHay.Tests` (§1.3). Tầng unit:

```
ELearning_ToanHocHay.Tests/
├─ Unit/
│  ├─ Infrastructure/
│  │  ├─ SqliteDb.cs            (§1.3 — AppDbContext SQLite in-memory, connection giữ mở)
│  │  ├─ TestConfig.cs          (IConfiguration in-memory + preset JWT)
│  │  ├─ Claims.cs              (dựng ClaimsPrincipal theo role/id)
│  │  ├─ Entities.cs            (object mother: NewUser/NewStudent/NewPayment/NewCourse…)
│  │  ├─ Fakes.cs               (FakeRefundFieldProtector, FakeClock, FakeSystemConfig, RecordingEmail)
│  │  └─ DataProtection.cs      (IDataProtectionProvider ephemeral cho UT-PROT)
│  ├─ Auth/
│  │  ├─ AuthServiceLoginTests.cs            (UT-AUTH-LOGIN-*)
│  │  ├─ AuthServiceRefreshTokenTests.cs     (UT-AUTH-REFRESH-*)
│  │  ├─ AuthServicePasswordTests.cs         (UT-AUTH-PWD-*)
│  │  ├─ AuthServiceConfirmEmailTests.cs     (UT-AUTH-CONFIRM-*)   [U2]
│  │  ├─ AuthServiceResetPasswordTests.cs    (UT-AUTH-RESET-*)     [U2]
│  │  ├─ AuthServiceRegisterTests.cs         (UT-AUTH-REGISTER-*)  [U2]
│  │  ├─ AuthServiceValidateTokenTests.cs    (UT-AUTH-VALIDATE-*)
│  │  ├─ LoginThrottlePolicyTests.cs         (UT-THROTTLE-*)
│  │  ├─ JwtServiceTests.cs                  (UT-JWT-*)
│  │  ├─ PasswordHasherTests.cs              (UT-PWD-*)
│  │  ├─ SecureTokensTests.cs                (UT-TOKEN-*)
│  │  └─ ClaimsPrincipalExtensionsTests.cs   (UT-CLAIMS-*)
│  ├─ Grading/
│  │  └─ AnswerGradingTests.cs               (UT-GRADE-*)
│  ├─ Sepay/
│  │  ├─ SePayContentParserTests.cs          (UT-SEPAY-PARSE-*)
│  │  ├─ SePayAmountMatcherTests.cs          (UT-SEPAY-AMOUNT-*)
│  │  ├─ SePayIpnEvaluatorTests.cs           (UT-SEPAY-EVAL-*)
│  │  └─ SePayServiceTests.cs                (UT-SEPAY-SVC-*)
│  ├─ Refund/
│  │  ├─ RefundCsvWriterTests.cs             (UT-CSV-*)
│  │  ├─ RefundCompletionTests.cs            (UT-RFC-*)
│  │  ├─ RefundDayWindowTests.cs             (UT-RFP-WINDOW-*)
│  │  ├─ RefundServicePolicyTests.cs         (UT-RFP-*)      [U2]
│  │  └─ RefundFieldProtectorTests.cs        (UT-PROT-*)
│  ├─ Content/
│  │  ├─ ContentAccessCoversTests.cs         (UT-GATE-COVERS-*)
│  │  ├─ ContentAccessServiceTests.cs        (UT-GATE-*)     [U2]
│  │  └─ EnrollmentServiceTests.cs           (UT-ENROL-*)    [U2]
│  ├─ Progress/
│  │  └─ ProgressRollupTests.cs              (UT-PROG-*)
│  ├─ Quota/
│  │  └─ AiQuotaServiceTests.cs              (UT-QUOTA-*)     [U2]
│  ├─ Subscription/
│  │  ├─ SubscriptionLifecycleServiceTests.cs (UT-LIFE-*)    [U2]
│  │  └─ PackageTierResolverTests.cs         (UT-TIER-*)      [U2]
│  ├─ Access/
│  │  └─ ResourceAccessServiceTests.cs       (UT-RES-*)
│  ├─ Notification/
│  │  └─ NotificationServiceTests.cs         (UT-NOTIF-*)     [U2]
│  ├─ Attempt/
│  │  └─ ExerciseAttemptServiceTests.cs      (UT-ATT-*)       [U2]
│  └─ Common/
│     ├─ SystemConfigServiceTests.cs         (UT-CFG-*)       [U2]
│     ├─ RateLimitPartitioningTests.cs       (UT-RLP-*)
│     ├─ PagingTests.cs                      (UT-PAGE-*)
│     ├─ AuthorizationAttributeTests.cs      (UT-ATTR-*)
│     ├─ MiddlewareTests.cs                  (UT-MW-*)
│     └─ DtoValidationTests.cs               (UT-DTO-*)
└─ Integration/                              (xem DANH-MUC-INTEGRATION-TEST.md §7)
```

Mọi class trong `Unit/` gắn `[Trait("Level","Unit")]` + `[Trait("Tier","U1"|"U2")]`.

---

## 6. Thứ tự triển khai

| Bước | Nội dung | Số case (xấp xỉ) | Chặn bởi |
|---|---|---|---|
| **B0** ✅ | Dựng hạ tầng [§1.3](#13-hạ-tầng-test-xây-mới): dự án `ELearning_ToanHocHay.Tests`, gói NuGet, `<InternalsVisibleTo>`, `SqliteDb`/`TestConfig`/`Claims`/`Entities`/`Fakes`/`DataProtection`. | — | — |
| **B1** ✅ | U1 thuần: `AnswerGrading`, `SecureTokens`, `ClaimsPrincipalExtensions`, `PasswordHasher`, `PagedRequest`, `RateLimitPartitioning`, `RefundCsvWriter`, `RefundFieldProtector`, `JwtService` | ~120 | B0 |
| **B2** ✅ | Refactor [§2](#2-refactor-mở-đường-làm-trước): tách policy / parser / evaluator / `Apply(entity)` / `Covers` internal / `ProgressRollup` + inject `TimeProvider` | — | review kiến trúc |
| **B3** ✅ | U1 sau refactor: `LoginThrottlePolicy`, `SePayContentParser`, `SePayAmountMatcher`, `SePayIpnEvaluator`, `SePayService`, `RefundCompletion`, `RefundDayWindow`, `ContentAccess.Covers`, `ProgressRollup` | ~80 | B2 |
| **B4** ✅ | U1 `AuthService` (`Login`, `RefreshToken`, `Logout`, `ChangePassword`, `ValidateToken`), `ResourceAccessService`, `EnrollmentService` (repo fake bằng `NSubstitute`) | ~70 | B3 |
| **B5** ✅ | U2 (SQLite): `Register`/`Confirm`/`Forgot`/`Reset`, `AiQuotaService`, `SubscriptionLifecycleService`, `PackageTierResolver`, `ContentAccessService`, `RefundServicePolicy`, `SystemConfigService`, `NotificationService`, `ExerciseAttemptService`, `ProgressProjection` | ~110 | B4 |
| **B6** | `UT-ATTR-*`, `UT-MW-*`, `UT-DTO-*` | ~25 | B4 |

Tổng: **~500 unit test**. Sau B6 mới bắt đầu bổ sung **integration test**
([DANH-MUC-INTEGRATION-TEST.md](DANH-MUC-INTEGRATION-TEST.md)).

---

## 7. Trạng thái hiện tại

> Cập nhật 2026-09-06.

- ✅ **Hạ tầng test cũ đã xoá** — toàn bộ thư mục `Tests/` (kể cả `Tests/AnswerGradingTests.cs`,
  `Tests/Infrastructure/*`, dự án `ELearning_ToanHocHay_Control.Tests.csproj`) đã bị gỡ khỏi repo
  và khỏi solution.
- ✅ **B0 — Hạ tầng test mới (§1.3): XONG.**
  - Dự án `ELearning_ToanHocHay.Tests/` (`net8.0`, xUnit 2.9.2, FluentAssertions 6.12.1,
    NSubstitute 5.1.0, `Microsoft.EntityFrameworkCore.Sqlite` 8.0.22,
    `Microsoft.AspNetCore.DataProtection` 8.0.22), đã thêm vào `ELearning_ToanHocHay_Control.sln`.
  - `<InternalsVisibleTo Include="ELearning_ToanHocHay.Tests" />` đã thêm vào project chính.
  - `Unit/Infrastructure/`: `SqliteDb.cs` · `TestConfig.cs` (+ preset `Jwt`) · `Claims.cs`
    (`For` / `Anonymous`) · `Entities.cs` (object mother: `NewUser/NewStudent/NewParent/NewPayment/`
    `NewSubscription/NewPackage/NewCourse/NewQuestion/NewRefundRequest`) · `Fakes.cs`
    (`FakeRefundFieldProtector`, `FakeClock : TimeProvider`, `FakeSystemConfig`, `RecordingEmail`) ·
    `DataProtection.cs` (`Ephemeral()`).
  - `InfrastructureSmokeTests.cs`: 8 test khói xác nhận SQLite `EnsureCreated` chạy (seed catalog +
    system config lên), object mother round-trip, fake/claims/data-protection hoạt động — **8/8 xanh**
    (`dotnet test --filter "Level=Unit"`).
- ✅ **B1 — U1 thuần: XONG.** 135 test, **135/135 xanh** (tổng cộng 143 với hạ tầng). File:
  - `Unit/Grading/AnswerGradingTests.cs` — UT-GRADE-01..55 (MC/TF/FillBlank/Essay/null,
    `IsFillBlankCorrect`, `Normalize`, `TryParseNumeric`).
  - `Unit/Auth/SecureTokensTests.cs` — UT-TOKEN-01..07.
  - `Unit/Auth/ClaimsPrincipalExtensionsTests.cs` — UT-CLAIMS-01..13.
  - `Unit/Auth/PasswordHasherTests.cs` — UT-PWD-01..06.
  - `Unit/Auth/JwtServiceTests.cs` — UT-JWT-01..14.
  - `Unit/Common/PagingTests.cs` — UT-PAGE-01..07 (U1) + `PagingQueryTests` UT-PAGE-08/09 (U2, SQLite).
  - `Unit/Common/RateLimitPartitioningTests.cs` — UT-RLP-01..10.
  - `Unit/Refund/RefundCsvWriterTests.cs` — UT-CSV-01..14.
  - `Unit/Refund/RefundFieldProtectorTests.cs` — UT-PROT-01..08.
- ✅ **B2 — Refactor mở đường (§2): XONG.** 10 điểm refactor + `TimeProvider` (xem bảng §2).
  Sản phẩm build sạch, 143/143 test cũ vẫn xanh — không đổi hành vi. File mới:
  `Services/Helpers/{LoginThrottlePolicy,SePayContentParser,SePayAmountMatcher,SePayIpnEvaluator,RefundDayWindow,ProgressRollup}.cs`,
  `Services/Interfaces/{IPackageTierResolver,IRefreshTokenIssuer}.cs`,
  `Services/Implementations/{PackageTierResolver,RefreshTokenIssuer}.cs`.
- ✅ **B3 — U1 sau refactor: XONG.** ~78 test, **221/221 xanh**. File:
  - `Unit/Auth/LoginThrottlePolicyTests.cs` — UT-THROTTLE-01..10.
  - `Unit/Sepay/SePayContentParserTests.cs` — UT-SEPAY-PARSE-01..11.
  - `Unit/Sepay/SePayAmountMatcherTests.cs` — UT-SEPAY-AMOUNT-01..08.
  - `Unit/Sepay/SePayIpnEvaluatorTests.cs` — UT-SEPAY-EVAL-01..09.
  - `Unit/Sepay/SePayServiceTests.cs` — UT-SEPAY-SVC-01..09.
  - `Unit/Refund/RefundCompletionTests.cs` — UT-RFC-01..07.
  - `Unit/Refund/RefundDayWindowTests.cs` — UT-RFP-WINDOW-01..05.
  - `Unit/Content/ContentAccessCoversTests.cs` — UT-GATE-COVERS-01..08.
  - `Unit/Progress/ProgressRollupTests.cs` — UT-PROG-01..04 (phần U1); UT-PROG-05..09 để lại B5 (U2).
- ✅ **B4 — U1 `AuthService` / `ResourceAccessService` / `EnrollmentService`: XONG.** ~65 test,
  **286/286 xanh**. Mọi phụ thuộc là NSubstitute; `AppDbContext` chỉ là stub (5 hàm Auth không
  chạm tới sau refactor §2). File:
  - `Unit/Auth/_AuthHarness.cs` — bộ substitute + `FakeClock` dùng chung.
  - `Unit/Auth/AuthServiceLoginTests.cs` — UT-AUTH-LOGIN-01..17.
  - `Unit/Auth/AuthServiceRefreshTokenTests.cs` — UT-AUTH-REFRESH-01..09.
  - `Unit/Auth/AuthServicePasswordTests.cs` — UT-AUTH-PWD-01..08.
  - `Unit/Auth/AuthServiceValidateTokenTests.cs` — UT-AUTH-VALIDATE-01..06.
  - `Unit/Access/ResourceAccessServiceTests.cs` — UT-RES-01..12 (`CanViewAttemptAsync` là tên
    thật của "CanAccessAttempt"; `CanModifyAttemptAsync` chưa có case riêng — bổ sung ở B5/IT nếu cần).
  - `Unit/Content/EnrollmentServiceTests.cs` — UT-ENROL-01..07.
- ✅ **B5 — U2 (SQLite): XONG.** ~110 test, **405/405 xanh**.
  - ✅ đợt 1 (49 test, **335/335 xanh**): `PackageTierResolver` (UT-TIER), `SystemConfigService` (UT-CFG-01..08),
    `SubscriptionLifecycleService` (UT-LIFE), `AiQuotaService` (UT-QUOTA), `ContentAccessService`
    GetCourseAccess (UT-GATE — fake repo).
    - Hạ tầng thêm: `Unit/Infrastructure/Seed.cs` (chèn đồ thị entity hợp lệ cho SQLite).
    - **Sửa bug sản phẩm:** `PackageTierResolver` sắp xếp `Package.Tier` ở DB — cột này persist
      dạng **string** (`HasConversion<string>`) nên `ORDER BY` ra thứ tự chữ cái ("Standard" > "Premium").
      Đã đổi sang nạp danh sách tier rồi `Max()` theo enum. (UT-TIER-03)
  - ✅ đợt 2 (29 test, **364/364 xanh**): `Confirm`/`Resend` (UT-AUTH-CONFIRM), `Forgot`/`Reset`
    (UT-AUTH-RESET), `Register` (UT-AUTH-REGISTER) — `AppDbContext` SQLite + repo thật
    (`UserRepository`…) trên cùng context; email/hasher/cache/refresh-token repo là fake
    (`Unit/Auth/_AuthU2.cs`).
  - ✅ đợt 3 (24 test, **388/388 xanh**): `NotificationService`/`NotificationRules` (UT-NOTIF),
    `RefundService` chính sách duyệt (UT-RFP: create-guard, trần/ngày, dual-control, state machine).
    - **Sửa tương thích SQLite:** `RefundService.CheckDailyCapAsync` dùng `SumAsync(decimal)` — EF
      SQLite provider không hỗ trợ; đổi sang `Select(...).ToListAsync()` rồi `.Sum()` phía client
      (số dòng nhỏ; Postgres không đổi hành vi).
  - ✅ đợt 4 (4 test, **392/392 xanh**): `ProgressProjectionService` U2 (UT-PROG-06..09) —
    `MarkLessonCompleteAsync` (ngưỡng 20s, 100% + roll-up bài→chương→cache khoá học),
    `ProjectAttemptAsync` bỏ qua attempt `InProgress`. Hạ tầng thêm: `Seed.CourseVersion/Node/Exercise`.
  - ✅ đợt 5 (13 test): `ExerciseAttemptService` (UT-ATT-01..13) — sociable: repo thật
    (`ExerciseAttemptRepository`…) trên SQLite, AI queue / progression / notify / email là fake.
    `CompleteExercise` chạy cả row-lock raw SQL + transaction thật; ATT-11 khẳng định idempotency.
    `Seed` thêm: `QuestionBank/Question/AttachQuestion/Attempt/Answer`.
- ⏳ **B6:** `UT-ATTR` (authorization attributes), `UT-MW` (middleware), `UT-DTO` (~25 case).
