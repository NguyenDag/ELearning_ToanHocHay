# Kế hoạch kiểm thử hệ thống — ToanHocHay

> **Bản nội bộ.** Khung kiểm thử theo **luồng nghiệp vụ** (business flow), làm lớp nhìn
> bổ sung cho bộ test hiện có (đang tổ chức theo mã rà soát A1–A5 và pha P0–P8 —
> xem [Ra-soat-API-va-ke-hoach-kiem-soat.md](Ra-soat-API-va-ke-hoach-kiem-soat.md)).
>
> Tài liệu này là **khung** — liệt kê đầy đủ luồng + kịch bản + ánh xạ sang test đã có +
> phân tích khoảng trống. Việc **triển khai chi tiết** từng kịch bản làm ở bước sau,
> theo lộ trình §9.
>
> **Danh mục chi tiết theo tầng (mỗi file tự chứa, là bản gốc của tầng đó):**
> - Unit — [DANH-MUC-UNIT-TEST.md](DANH-MUC-UNIT-TEST.md)
> - Integration — [DANH-MUC-INTEGRATION-TEST.md](DANH-MUC-INTEGRATION-TEST.md)
> - System / E2E — [DANH-MUC-SYSTEM-TEST.md](DANH-MUC-SYSTEM-TEST.md)

---

## 1. Mục tiêu & phạm vi

| | |
|---|---|
| **Mục tiêu** | Mỗi luồng người dùng chính có một bộ test bám luồng, đọc được như đặc tả sống; hồi quy bắt được lỗi trước khi lên staging. |
| **Trong phạm vi** | Backend API (`ELearning_ToanHocHay_Control`) — 12 luồng ở §3. Unit test thuần + integration test qua HTTP (Testcontainers Postgres). |
| **Ngoài phạm vi (Unit + Integration)** | Test UI WebApp (Playwright), test tải/hiệu năng, test hợp đồng với Flask AI thật, test bảo mật xâm nhập → **đã tách sang** [DANH-MUC-SYSTEM-TEST.md](DANH-MUC-SYSTEM-TEST.md) (§10). |
| **Ngôn ngữ / thư viện** | xUnit + FluentAssertions + `Xunit.SkippableFact` + `Microsoft.AspNetCore.Mvc.Testing` + `Testcontainers.PostgreSql` + `NSubstitute` + `Microsoft.EntityFrameworkCore.Sqlite` (danh sách đầy đủ ở [UNIT §1.3](DANH-MUC-UNIT-TEST.md) / [IT §2.1](DANH-MUC-INTEGRATION-TEST.md)). |

---

## 2. Kiến trúc khung test

> ⚠️ **Hạ tầng test cũ (`Tests/*` — factory, seed, collection, mọi file `*Tests.cs`) sẽ bị
> xoá và viết lại từ đầu.** Đặc tả hạ tầng mới **chi tiết** nằm ở:
> - **Unit** — [DANH-MUC-UNIT-TEST.md §1.3](DANH-MUC-UNIT-TEST.md)
> - **Integration** — [DANH-MUC-INTEGRATION-TEST.md §2](DANH-MUC-INTEGRATION-TEST.md)
> - **System** — [DANH-MUC-SYSTEM-TEST.md §2–§3](DANH-MUC-SYSTEM-TEST.md)
>
> Phần dưới chỉ là tổng quan; con số case đầy đủ ở 3 danh mục trên.

### 2.1 Ba tầng

| Tầng | Khi nào dùng | Hạ tầng (mới) | Danh mục |
|---|---|---|---|
| **Unit** | Logic không cần HTTP: chấm điểm, regex, chính sách, chuẩn hoá, CSV, so tiền, ánh xạ claim | U1 = thuần · U2 = SQLite in-memory + NSubstitute | [DANH-MUC-UNIT-TEST.md](DANH-MUC-UNIT-TEST.md) — ~500 case `UT-*` |
| **Integration** | Mọi luồng đi qua controller → service → EF → **Postgres thật** (Testcontainers), gọi qua HTTP | `WebApplicationFactory<Program>` + `Testcontainers.PostgreSql` + fakes cho AI/email | [DANH-MUC-INTEGRATION-TEST.md](DANH-MUC-INTEGRATION-TEST.md) — ~180 case `IT-F*` |
| **System / E2E** | Toàn hệ đã triển khai (WebApp + BE + Postgres + Flask AI + SePay sandbox + email), qua trình duyệt / client thật | môi trường `staging` + Playwright / k6 / newman | [DANH-MUC-SYSTEM-TEST.md](DANH-MUC-SYSTEM-TEST.md) — `ST-*` |

> Không dùng `EFCore.InMemory` — U2 dùng **SQLite in-memory** (giữ khoá ngoại / transaction /
> `LIKE`), integration dùng **Postgres thật** (materialized path, `StartsWith`, unique index).

### 2.2 Dự án test

Một dự án `ELearning_ToanHocHay.Tests` (mới, `net8.0`) chứa cả `Unit/` và `Integration/`.
Chi tiết gói NuGet, `<InternalsVisibleTo>`, các file hạ tầng — xem 2 danh mục Unit/Integration.

### 2.3 Quy ước chung

- **Trait**: `[Trait("Level","Unit"|"Integration")]` + (integration) `[Trait("Flow","Payment")]` + `[Trait("Priority","P1")]`.
- **Chạy**: `dotnet test --filter "Level=Unit"` (không cần Docker) · `--filter "Level=Integration&Flow=Payment"` · `--filter "Priority=P1"`.
- **Tên test** khớp mã trong danh mục tương ứng (`UT-AUTH-LOGIN-06`, `IT-F7-05`, `ST-E2E-01`).
- **Thân test**: `// Arrange / Act / Assert`, mỗi assert 1 hành vi.
- **Cô lập**: mỗi test tự dựng dữ liệu riêng (`Guid`-hoá), chỉ đọc golden dataset, không sửa.

---

## 3. Bản đồ luồng

> Cột **Prior art** = file test **cũ (sẽ bị xoá)** có logic tham khảo khi viết lại. Cột
> **Độ phủ prior art** = code cũ phủ được bao nhiêu, chỉ để ước lượng công viết mới.

| # | Luồng | Controller chính | Prior art (code cũ) | Độ phủ prior art |
|---|---|---|---|---|
| **F1** | Xác thực & tài khoản | `AuthController`, `AdminController` | `P1AuthTests`, `A1AuthorizationMatrixTests` | 🟢 Tốt |
| **F2** | Danh mục & ghi danh | `CatalogController`, `CoursesController`, `EnrollmentController`, `LearnController` | `A3ContentLayerTests` | 🟡 Khá |
| **F3** | Học bài (bài giảng) | `LearnController`, `ProgressController` | `P4ProgressTests` | 🟡 Khá |
| **F4** | Làm bài tập / kiểm tra | `ExerciseAttemptsController` | `A2BusinessLogicTests`, `P3P4RemainingTests`, `AnswerGradingTests` | 🟢 Tốt |
| **F5** | Tiến độ & Dashboard học sinh | `DashboardController`, `ProgressController`, `StudentController` | `P4ProgressTests`, `P3P4RemainingTests` | 🟡 Khá |
| **F6** | Dashboard & liên kết phụ huynh | `ParentController`, `DashboardController` | `P6Tests`, `A1AuthorizationMatrixTests` | 🟡 Khá |
| **F7** | Thanh toán (SePay VA + IPN) | `SubscriptionController`, `SepayController`, `PaymentController`, `FinanceController` | `P5PaymentTests`, `A2BusinessLogicTests` | 🟢 Tốt |
| **F8** | Hoàn tiền (bán tự động) | `RefundsController`, `FinanceController` | `RefundWorkflowTests`, `RemainingFeaturesTests` | 🟢 Tốt |
| **F9** | Trợ giúp AI & Chatbot | `AIHintController`, `AIFeedbackController`, `ChatbotController` | `P6Tests`, `RemainingFeaturesTests` | 🟡 Khá |
| **F10** | Thông báo | `NotificationsController`, `AdminController` | `P6Tests` | 🟡 Khá |
| **F11** | Soạn nội dung (authoring) | `ContentAuthoringController`, `CoursesController`, `QuestionBanksController` | `A3ContentLayerTests`, `RemainingFeaturesTests` | 🟡 Khá |
| **F12** | Hợp đồng API & Vận hành | (toàn hệ) | `A5NormalizationTests`, `ContractTests`, `P7Tests` | 🟢 Tốt |

**Chú giải độ phủ:** 🟢 hầu hết happy + bad case chính đã có · 🟡 có nền, còn thiếu nhánh
· 🔴 chưa có.

---

## 4. Chi tiết từng luồng

> Đây là bản đồ luồng ở mức cao. **Danh sách case đầy đủ (đã cập nhật theo hạ tầng mới)** ở
> [DANH-MUC-INTEGRATION-TEST.md §4](DANH-MUC-INTEGRATION-TEST.md) (`IT-F*`).
>
> Cột **Trạng thái** ở đây: ✅ có prior art (code cũ sẽ xoá — port logic assert) · 🔲 viết mới
> hoàn toàn · ➖ ngoài phạm vi backend. **Không hàng nào "đã xong".**
> Cột **P**: P1 bắt buộc · P2 nên có · P3 khi rảnh.

### F1 — Xác thực & tài khoản

Tài liệu nền: pha P1 trong [Ra-soat-API...](Ra-soat-API-va-ke-hoach-kiem-soat.md).

| Mã | Kịch bản | Loại | P | Trạng thái |
|---|---|---|---|---|
| F1-01 | Đăng ký tạo user chưa xác thực + gửi email xác nhận; không tự đăng nhập | Integration | P1 | 🔲 |
| F1-02 | Xác nhận email bằng token hợp lệ → `IsEmailConfirmed=true`; token sai/hết hạn → 400 | Integration | P1 | 🔲 |
| F1-03 | Gửi lại email xác nhận: luôn 200 (không lộ email tồn tại), có giới hạn tần suất | Integration | P2 | 🔲 |
| F1-04 | Đăng nhập email chưa xác nhận → chặn với thông báo rõ | Integration | P1 | 🔲 |
| F1-05 | Đăng nhập đúng → trả `Token` + `RefreshToken`; xoay refresh thu hồi token cũ | Integration | P1 | ✅ `P1AuthTests` |
| F1-06 | Dùng lại refresh token đã xoay (reuse detection) → thu hồi cả họ token | Integration | P1 | 🔲 (P1 mới test rotation, chưa test reuse) |
| F1-07 | Đổi mật khẩu → thu hồi mọi refresh token; access token cũ hết hiệu lực | Integration | P1 | ✅ `P1AuthTests` |
| F1-08 | 5 lần sai mật khẩu → khoá tạm thời (`LockoutEndsAt`); hết hạn khoá đăng nhập lại được | Integration | P1 | ✅ `P1AuthTests` (phần "hết hạn khoá" còn mỏng) |
| F1-09 | Quên mật khẩu luôn 200; đặt lại bằng token đổi được mật khẩu; token 1 lần | Integration | P1 | ✅ `P1AuthTests` |
| F1-10 | Admin khoá user → chặn đăng nhập + vô hiệu access token đang bay (SecurityStamp); mở khoá khôi phục | Integration | P1 | ✅ `P1AuthTests` + `P7Tests` |
| F1-11 | Admin đổi role → ghi `AuditLog`; non-admin gọi → 403 | Integration | P1 | ✅ `P1AuthTests` |
| F1-12 | `GET /api/auth/me` trả email + userType + (studentId/parentId) đúng | Integration | P2 | ✅ `A1AuthorizationMatrixTests` |
| F1-13 | `logout` thu hồi refresh token hiện tại | Integration | P2 | 🔲 |
| F1-14 | Rate limit `auth` phân vùng theo người dùng cuối (`X-Client-Key`): client A spam không chặn client B; 429 trả vỏ ApiResponse tiếng Việt | Integration | P2 | 🔲 (đã kiểm thủ công, chưa có test tự động) |
| F1-15 | Ẩn danh gọi endpoint cần đăng nhập → 401 (không 302) | Integration | P1 | ✅ rải rác `A1...` |
| F1-16 | `validate-token` / `refresh-token` với token rác → 401, không 500 | Integration | P2 | 🔲 |

### F2 — Danh mục & ghi danh

| Mã | Kịch bản | Loại | P | Trạng thái |
|---|---|---|---|---|
| F2-01 | Khách xem danh sách môn / lớp / khung / khoá học → 200 (public) | Integration | P1 | ✅ `A3ContentLayerTests` (subjects) — thiếu grade/framework/courses |
| F2-02 | Xem khoá theo slug → 200; slug không tồn tại → 404 envelope | Integration | P2 | 🔲 |
| F2-03 | `GET /api/learn/courses/{id}/content` — khách chỉ thấy node `IsFree`; nhánh trả phí bị cắt | Integration | P1 | ✅ `A3ContentLayerTests` |
| F2-04 | Node trả phí gọi trực tiếp `/api/learn/nodes/{id}` — học sinh chưa có quyền → 403 | Integration | P1 | ✅ `A3ContentLayerTests` |
| F2-05 | Ghi danh khoá miễn phí → `StudentCourse`; `GET /api/enrollments/me` liệt kê | Integration | P1 | ✅ `A3ContentLayerTests` |
| F2-06 | Học sinh đã ghi danh → `AccessLevel=Full`, thấy toàn cây, mở được node trả phí | Integration | P1 | ✅ `A3ContentLayerTests` |
| F2-07 | Cổng 3 tầng: node `IsFree` / học sinh có `PackageEntitlement(SubjectGrade)` / học sinh ghi danh khoá — mỗi tầng cho đúng mức truy cập | Integration | P1 | 🟡 một phần (`A3` + `P4`); thiếu tổ hợp entitlement theo môn-lớp |
| F2-08 | Ghi danh khoá chưa `Published` → 404/400 | Integration | P2 | 🔲 |
| F2-09 | Ghi danh 2 lần cùng khoá → idempotent (không nhân bản) | Integration | P2 | 🔲 |
| F2-10 | Học sinh gọi endpoint authoring (`POST /api/courses`, `/api/question-banks`) → 403 | Integration | P1 | ✅ `A3ContentLayerTests` |

### F3 — Học bài (bài giảng)

| Mã | Kịch bản | Loại | P | Trạng thái |
|---|---|---|---|---|
| F3-01 | Mở node bài giảng "showcase" đủ 12 loại block → payload có đủ block/resource/flashcard | Integration | P2 | 🔲 |
| F3-02 | Bài khoá (chưa đủ quyền) → 403 kèm envelope gợi ý nâng gói | Integration | P1 | ✅ `A3ContentLayerTests` (403) — thiếu kiểm nội dung gợi ý |
| F3-03 | `POST /api/progress/lessons/{nodeId}/complete` cần thời gian xem tối thiểu; đủ → `NodeProgress=100%` | Integration | P1 | ✅ `P4ProgressTests` |
| F3-04 | Hoàn thành bài → roll-up `MaterializedPath` lên chương / version (dùng `StartsWith`, gồm chính node) | Integration | P1 | ✅ `P4ProgressTests` |
| F3-05 | Nộp bài tập trong bài giảng → cập nhật `NodeProgress` của node bài tập + roll-up | Integration | P1 | ✅ `P4ProgressTests` |
| F3-06 | `GET /api/progress/versions/{id}` trả % hoàn thành theo cây | Integration | P2 | 🔲 |
| F3-07 | Đánh dấu hoàn thành bài không thuộc khoá đã ghi danh → 403 | Integration | P2 | 🔲 |
| F3-08 | Học sinh khác đọc tiến độ của tôi → 403 (owner guard) | Integration | P1 | 🟡 heatmap có (`P4`), lesson progress chưa |
| F3-09 | Flashcard deck: lật thẻ / đánh dấu thuộc (nếu có endpoint) | Integration | P3 | ➖/🔲 (rà endpoint) |

### F4 — Làm bài tập / kiểm tra

Tài liệu nền: pha P3.

| Mã | Kịch bản | Loại | P | Trạng thái |
|---|---|---|---|---|
| F4-01 | `start` exercise đã publish → tạo `ExerciseAttempt(InProgress)` + `PlannedEndTime` | Integration | P1 | ✅ helper trong `A2BusinessLogicTests` |
| F4-02 | `start` khi tier không đủ (Test/Exam yêu cầu gói) → **403** (không còn 400) | Integration | P1 | 🟡 gián tiếp; nên có test riêng cho từng loại |
| F4-03 | `start-random` từ ngân hàng câu hỏi (có/không `DurationMinutes`) → lưu & hoàn thành, không "timeout ảo" | Integration | P1 | ✅ `A2BusinessLogicTests` (A2-03) |
| F4-04 | `save-answer` nhiều lần cho 1 câu → ghi đè, không nhân bản | Integration | P1 | 🟡 dùng trong helper, chưa assert ghi đè |
| F4-05 | `complete` trả nhanh (<10s), chấm xong phần tự động, đẩy job AI feedback nền | Integration | P1 | ✅ `A2BusinessLogicTests` (A2-04) |
| F4-06 | Ma trận chấm: MC theo option, TF theo option/text, FillBlank chuẩn hoá số/phân số/khoảng trắng, Essay → chờ chấm tay | Unit + Integration | P1 | ✅ `AnswerGradingTests` + `A2BusinessLogicTests` (A2-09) |
| F4-07 | `MaxAttempts=1` → lần `start` thứ 2 bị chặn (400 "attempt") | Integration | P1 | ✅ `A2BusinessLogicTests` (A2-08) |
| F4-08 | `complete` 2 lần song song → chấm đúng 1 lần (row lock) | Integration | P1 | ✅ `P3P4RemainingTests` |
| F4-09 | `GET /{attemptId}/result` — chủ sở hữu 200; học sinh khác 403; phụ huynh liên kết 200; phụ huynh lạ 403 | Integration | P1 | ✅ `A1AuthorizationMatrixTests` |
| F4-10 | `GET student/{id}/history` — cùng ma trận quyền như F4-09 | Integration | P1 | ✅ `A1AuthorizationMatrixTests` |
| F4-11 | `report-tab-switch` — chống spam (debounce) + trần email; ghi `TabSwitchLog` | Integration | P2 | 🔲 (chỉ notif rule engine có ở P6) |
| F4-12 | `feedback-status` poll → `TotalWrong`, tiến độ job; khi xong → `/result` có `FullSolution` | Integration | P2 | 🟡 một phần (`A2-04`) |
| F4-13 | `save-answer` / `complete` trên attempt của người khác → 403; ẩn danh → 401 | Integration | P1 | ✅ `A1AuthorizationMatrixTests` |
| F4-14 | `complete` sau `PlannedEndTime` → vẫn chấm (không mất bài), đánh dấu quá giờ nếu có | Integration | P2 | 🔲 |
| F4-15 | Endpoint cũ `/submit`, `/submit-answer` → 404 | Integration | P3 | ✅ `A2BusinessLogicTests` (A2-07) |
| F4-16 | Burst nhiều `start` đồng thời của 1 học sinh → không 5xx | Integration | P2 | ✅ `ContractTests` |

### F5 — Tiến độ & Dashboard học sinh

Tài liệu nền: pha P4.

| Mã | Kịch bản | Loại | P | Trạng thái |
|---|---|---|---|---|
| F5-01 | `dashboard/overview` — số liệu tổng (bài học, điểm TB, streak) đúng với dữ liệu seed | Integration | P1 | 🟡 chỉ test quyền + tier, chưa assert số liệu |
| F5-02 | `chapter-score-comparison` — so tuần này với tuần trước, `TrendDirection` đúng | Integration | P1 | ✅ `P3P4RemainingTests` |
| F5-03 | `ai-assessment` / `ai-roadmap` — trả điểm yếu thật từ `NodeProgress`/attempt (không mock rỗng) | Integration | P2 | 🔲 (cần `FakeAiService`) |
| F5-04 | `heatmap` — `DailyActivitySnapshot` 90 ngày, streak tính đúng | Integration | P2 | 🟡 chỉ test owner guard |
| F5-05 | Tier trên dashboard lấy từ `Package.Tier` (không `Contains("premium")`) | Integration | P1 | ✅ `P4ProgressTests` |
| F5-06 | Học sinh Free gọi dashboard cần gói → 403 `upgradeRequired` (không 500) | Integration | P1 | 🟡 gián tiếp |
| F5-07 | Học sinh B đọc `students/{A}/dashboard/*` → 403 mọi endpoint | Integration | P1 | ✅ `A1AuthorizationMatrixTests` |
| F5-08 | `dashboard-stats` (StudentController) — quyền + hình dạng | Integration | P2 | 🔲 |
| F5-09 | Roll-up biểu đồ tới cấp chương (remaining P4) | Integration | P2 | 🟡 `RemainingFeaturesTests`? cần rà |

### F6 — Dashboard & liên kết phụ huynh

Tài liệu nền: pha P6 (ParentLinkService).

| Mã | Kịch bản | Loại | P | Trạng thái |
|---|---|---|---|---|
| F6-01 | Phụ huynh mời con bằng email (`POST /api/parents/{id}/invites`) → tạo lời mời | Integration | P1 | 🔲 |
| F6-02 | Học sinh nhập connection code (`POST /api/parents/link`) → `ParentLink(Active)` | Integration | P1 | ✅ `P6Tests` |
| F6-03 | `GET /api/parents/{id}/children` + `/children/overview` — liệt kê con + tiến độ tóm tắt | Integration | P1 | 🟡 overview gián tiếp |
| F6-04 | Phụ huynh liên kết đọc được history + dashboard của con | Integration | P1 | ✅ `A1AuthorizationMatrixTests` |
| F6-05 | Thu hồi liên kết (`DELETE .../children/{studentId}`) → mất quyền dashboard/history ngay | Integration | P1 | ✅ `P6Tests` |
| F6-06 | Phụ huynh **không** liên kết đọc dữ liệu học sinh → 403 | Integration | P1 | ✅ `A1AuthorizationMatrixTests` |
| F6-07 | Phụ huynh sửa/xoá hồ sơ phụ huynh khác → 403; xoá cần SystemAdmin | Integration | P2 | 🔲 |
| F6-08 | 1 học sinh liên kết nhiều phụ huynh; cờ `IsPrimaryGuardian`, `Relationship` | Integration | P2 | 🔲 |
| F6-09 | `GET /api/students/{id}/parents` — endpoint đã thêm (`StudentController`); test IT-F6-10 | Integration | P3 | ✅ |

### F7 — Thanh toán (SePay VA + QR + IPN)

Tài liệu nền: [Luong-thanh-toan.md](Luong-thanh-toan.md).

| Mã | Kịch bản | Loại | P | Trạng thái |
|---|---|---|---|---|
| F7-01 | `POST /api/subscriptions` → `Payment(Pending)` + `Subscription(Pending)`, **giá lấy từ `Package.Price`**, trả `qrUrl` mang `amount` | Integration | P1 | ✅ `A2BusinessLogicTests` (A2-02) |
| F7-02 | IPN `in` hợp lệ, đúng tiền, nội dung `SUBSCRIPTION_{id}` → `Active`, `Payment.Completed`, `TransactionId=ref`, `EndDate=now+DurationDays` | Integration | P1 | ✅ `P5PaymentTests` |
| F7-03 | Sai số tiền (ngoài `AmountToleranceVnd`) → `AmountMismatch`, subscription giữ `Pending` | Integration | P1 | ✅ `P5PaymentTests` |
| F7-04 | IPN lặp cùng `referenceCode` → `Duplicate`, chỉ 1 dòng `SePayIpnLog` | Integration | P1 | ✅ `P5PaymentTests` |
| F7-05 | IPN ref mới nhưng subscription đã `Active` → `Duplicate`, không kích hoạt lại | Integration | P1 | 🟡 rà `P5` |
| F7-06 | Giao dịch `out` / subscription không tồn tại → `Ignored` + **200** | Integration | P1 | ✅ `P5PaymentTests` |
| F7-07 | IPN thiếu/sai API key → **401** | Integration | P1 | ✅ `P5PaymentTests` |
| F7-08 | Kích hoạt subscription thứ 2 → subscription `Active` cũ tự `Expired` (guard A2-11) | Integration | P1 | ✅ `P5PaymentTests` |
| F7-09 | Overpay/underpay trong dung sai → vẫn kích hoạt | Integration | P2 | 🔲 (cần set `AmountToleranceVnd`) |
| F7-10 | Sweep: `Active` quá `EndDate` → `Expired`; `Pending` quá `PendingTimeoutMinutes` → `Cancelled` + `Payment.Failed` | Integration | P1 | ✅ `P5PaymentTests` |
| F7-11 | 2 IPN cùng ref chạy song song → unique index serialize, 1 thắng, cuối cùng `Duplicate` | Integration | P2 | 🔲 |
| F7-12 | `GET /api/subscriptions/me`, `/api/payments/me` → chỉ dữ liệu của mình | Integration | P1 | ✅ `P5PaymentTests` |
| F7-13 | `PUT /api/subscriptions/cancel/{id}` — chủ sở hữu huỷ được `Pending`/`Active`; người khác 403 | Integration | P2 | 🔲 |
| F7-14 | `GET /api/finance/subscriptions/reconciliation` — Finance/Admin 200, học sinh 403; `Balanced` khi không drift | Integration | P1 | ✅ `P5PaymentTests` |
| F7-15 | Danh sách `GET /api/payments` / `/api/subscriptions` — chỉ Finance/Admin, có phân trang + lọc status | Integration | P1 | ✅ `A1...` + `P7Tests` |
| F7-16 | Học sinh `PATCH /api/subscriptions/{id}/status` / `PUT /api/payments/update-status` → 403; ẩn danh → 401 | Integration | P1 | ✅ `A1AuthorizationMatrixTests` |
| F7-17 | Client gửi kèm `amount=1đ` khi tạo subscription → bị bỏ qua, dùng `Package.Price` | Integration | P1 | ✅ `A2BusinessLogicTests` (A2-02) |
| F7-18 | Regex nội dung IPN: `SUBSCRIPTION-1`, `SUBSCRIPTION_1`, `...SUBSCRIPTION_1...` đều trích được; rác → `Ignored` | Unit | P2 | 🔲 |

### F8 — Hoàn tiền (bán tự động)

Tài liệu nền: [Luong-hoan-tien.md](Luong-hoan-tien.md).

| Mã | Kịch bản | Loại | P | Trạng thái |
|---|---|---|---|---|
| F8-01 | Học sinh tạo yêu cầu hoàn cho **giao dịch của mình** → `PendingReview` + `RefundEvent(Created)` | Integration | P1 | ✅ `RefundWorkflowTests` |
| F8-02 | Học sinh hoàn giao dịch **người khác** → 403 (`CanAccessPaymentAsync`) | Integration | P1 | ✅ `RefundWorkflowTests` |
| F8-03 | Hoàn giao dịch `Pending`/`Failed` → 400; `amount` > phần còn hoàn được → 400 | Integration | P1 | ✅ `RefundWorkflowTests` |
| F8-04 | Giao dịch cũ hơn `refund.maxPaymentAgeDays` → 400 | Integration | P2 | 🔲 |
| F8-05 | Đã có yêu cầu đang xử lý cho giao dịch đó → 409 | Integration | P1 | ✅ `RefundWorkflowTests` |
| F8-06 | Quá `refund.maxRequestsPerUserPer30d` (theo người thụ hưởng, 30 ngày trượt) → 409; SystemAdmin được bỏ qua | Integration | P1 | ✅ `RefundWorkflowTests` |
| F8-07 | Finance `approve` → `Approved`, tiêu trần/ngày | Integration | P1 | ✅ `RefundWorkflowTests` |
| F8-08 | Duyệt vượt `refund.dailyCapVnd` → 400 "Vượt trần" | Integration | P1 | ✅ `RefundWorkflowTests` |
| F8-09 | Dual-control (`dualControlThresholdVnd>0`): lần 1 → `PendingSecondApproval`; cùng người duyệt lần 2 → 409; người Finance khác → `Approved` | Integration | P1 | ✅ `RefundWorkflowTests` |
| F8-10 | `reject` rồi `approve` → state machine chặn (409) | Integration | P1 | ✅ `RefundWorkflowTests` |
| F8-11 | Full batch: approve → tạo lô → export CSV (`Draft→Exported`) → mark-disbursed → confirm-all → request `Completed`, `Payment.Refunded`, `RefundAmount` đủ, subscription `Cancelled`, đủ `RefundEvent` + `AuditLog` | Integration | P1 | ✅ `RefundWorkflowTests` + `RemainingFeaturesTests` |
| F8-12 | Hoàn một phần → `Payment.PartiallyRefunded`, cho yêu cầu tiếp phần còn lại | Integration | P1 | ✅ `RefundWorkflowTests` |
| F8-13 | Huỷ lô → request thành viên về `Approved` | Integration | P2 | ✅ `RefundWorkflowTests` |
| F8-14 | `Failed` → `retry` → `Approved` (kiểm lại trần/ngày), rời lô | Integration | P2 | 🔲 |
| F8-15 | Confirm đơn lẻ từ `Approved` (không qua lô), `bankRef` bắt buộc | Integration | P2 | 🔲 |
| F8-16 | Non-finance gọi `/api/finance/refunds/*` → 403; ẩn danh `/api/refunds` → 401 | Integration | P1 | ✅ `RefundWorkflowTests` |
| F8-17 | CSV export: tên chủ TK bắt đầu `=`/`+`/`-`/`@` → thêm `'` chống formula-injection | Integration | P1 | ✅ `RefundWorkflowTests` |
| F8-18 | Số TK ngân hàng: DB lưu ciphertext (Data Protection); API chỉ trả 4 số cuối | Integration | P1 | 🟡 rà `RefundWorkflowTests` |
| F8-19 | `GET /api/refunds/me` chỉ yêu cầu của mình; `GET /api/refunds/{id}` người lạ → 403 | Integration | P1 | ✅ `RefundWorkflowTests` |
| F8-20 | `GET /api/finance/refunds/daily-usage` + `/reconciliation` — số khớp, `Balanced` | Integration | P2 | 🔲 |
| F8-21 | Rate limit cổng `refund` (5/phút/user) trên `POST /api/refunds` | Integration | P3 | 🔲 (test tắt limit này) |
| F8-22 | Sweep nền: `Disbursed` quá `staleDisbursedDays` → `Notification` cho Finance, không tự đổi trạng thái | Integration | P2 | 🔲 |

### F9 — Trợ giúp AI & Chatbot

Tài liệu nền: pha P6.

| Mã | Kịch bản | Loại | P | Trạng thái |
|---|---|---|---|---|
| F9-01 | Học sinh Free hết hạn mức gợi ý AI trong ngày → 429; sang ngày mới reset | Integration | P1 | ✅ `P6Tests` (phần reset còn mỏng) |
| F9-02 | Gói không giới hạn → không bị rate-limit gợi ý | Integration | P1 | ✅ `P6Tests` |
| F9-03 | `GET /api/ai-hints/quota` phản ánh đúng số đã dùng / còn lại | Integration | P2 | 🔲 |
| F9-04 | Gợi ý / feedback trên attempt của người khác → 403 | Integration | P1 | ✅ `A1AuthorizationMatrixTests` |
| F9-05 | AI feedback được đẩy nền khi `complete`; `/result` điền dần | Integration | P1 | ✅ `A2BusinessLogicTests` |
| F9-06 | Chatbot: lưu `ChatConversation`/`ChatMessage` cả khi Flask down | Integration | P1 | ✅ `P6Tests` |
| F9-07 | `GET /api/chatbot/health` → 503 khi AI không tới được, 200 khi khoẻ | Integration | P2 | ✅ `P6Tests` |
| F9-08 | `request-human` / escalation → chuyển hội thoại sang `SupportStaff`, vào `staff/queue` | Integration | P2 | ✅ `RemainingFeaturesTests` |
| F9-09 | SupportStaff `assign` / `reply` / `close`; non-staff gọi `staff/*` → 403 | Integration | P2 | 🔲 |
| F9-10 | `FakeAiService` chuẩn hoá: bật/tắt "khoẻ", trả nội dung định sẵn (hạ tầng) | Infra | P1 | 🔲 |

### F10 — Thông báo

| Mã | Kịch bản | Loại | P | Trạng thái |
|---|---|---|---|---|
| F10-01 | Điểm thấp → thông báo cho học sinh **và** phụ huynh liên kết | Integration | P1 | ✅ `P6Tests` |
| F10-02 | Chuyển tab nhiều lần khi làm bài → thông báo (rule engine) | Integration | P2 | 🔲 |
| F10-03 | Không hoạt động 3 ngày → thông báo (chạy qua `POST /api/admin/notifications/run-inactivity-check`) | Integration | P2 | 🟡 rà `P6`/`RemainingFeatures` |
| F10-04 | Opt-out 1 loại rule → chỉ user đó tắt, user khác vẫn nhận | Integration | P1 | ✅ `P6Tests` |
| F10-05 | `GET /api/notifications` + `unread-count` + `POST /{id}/read` + `read-all` | Integration | P1 | ✅ `P6Tests` |
| F10-06 | `GET/PUT /api/notifications/preferences` | Integration | P2 | 🔲 |
| F10-07 | Đọc/đánh dấu thông báo của người khác → 403/không thấy | Integration | P2 | 🔲 |

### F11 — Soạn nội dung (authoring)

Tài liệu nền: pha P2 / A3.

| Mã | Kịch bản | Loại | P | Trạng thái |
|---|---|---|---|---|
| F11-01 | CRUD catalog (subject/grade/framework) — đọc public, ghi cần content role | Integration | P1 | 🟡 subject có; grade/framework 🔲 |
| F11-02 | Vòng đời version: `Draft → submit → review(Approve) → publish`; publish lật `Course.Status=Published` | Integration | P1 | ✅ `A3ContentLayerTests` |
| F11-03 | Review `Reject` → version về `Draft`/`Rejected`, không publish được | Integration | P2 | 🔲 |
| F11-04 | Sửa nội dung sau khi version `Published` → 400 "Draft" | Integration | P1 | ✅ `A3ContentLayerTests` |
| F11-05 | Quy tắc loại node: lesson không thể nằm trực tiếp dưới root | Integration | P2 | ✅ `A3ContentLayerTests` |
| F11-06 | Sửa node → ghi `NodeRevision`; restore về bản cũ | Integration | P2 | ✅ `RemainingFeaturesTests` |
| F11-07 | Di chuyển / đổi cha node → viết lại `MaterializedPath` cả subtree | Integration | P1 | ✅ `RemainingFeaturesTests` |
| F11-08 | Reorder các node anh em | Integration | P2 | 🔲 |
| F11-09 | Review đính comment; editor `resolve` | Integration | P2 | ✅ `RemainingFeaturesTests` |
| F11-10 | Ngân hàng câu hỏi: tạo → submit → review(Approve) → `Approved`; reviewer role gate | Integration | P1 | ✅ `A3ContentLayerTests` |
| F11-11 | CRUD block/resource/flashcard-deck/flashcard trên node draft | Integration | P2 | 🔲 |
| F11-12 | Exercise: `publish` / `unpublish`; gắn/gỡ câu hỏi; chỉ content role | Integration | P1 | 🟡 authz có (`A1`), workflow 🔲 |
| F11-13 | `AuthorizeContentRole` chặn đúng: Student/Parent 403, Editor/Reviewer/Admin qua | Integration | P1 | ✅ rải rác |

### F12 — Hợp đồng API & Vận hành

Tài liệu nền: pha P7 / A5.

| Mã | Kịch bản | Loại | P | Trạng thái |
|---|---|---|---|---|
| F12-01 | Response thành công dùng envelope `{ StatusCode, Message, Data }` | Contract | P1 | ✅ `A5NormalizationTests`, `ContractTests` |
| F12-02 | Lỗi dùng envelope; 404 suy từ message; 403/401/400 đúng ngữ nghĩa | Contract | P1 | ✅ `A5NormalizationTests` |
| F12-03 | Model validation fail → 400 với `Errors` trong envelope | Contract | P1 | ✅ `A5NormalizationTests` |
| F12-04 | Enum serialize thành **tên chuỗi**; query param enum vẫn bind từ chuỗi | Contract | P1 | ✅ `ContractTests` |
| F12-05 | Kết quả phân trang có hình dạng ổn định (`Items`, `Page`, `PageSize`, `Total`) | Contract | P1 | ✅ `ContractTests` |
| F12-06 | Route kebab-số nhiều; route PascalCase cũ → 404 | Contract | P1 | ✅ `A5NormalizationTests` |
| F12-07 | Mọi response có `X-Correlation-Id`; echo lại nếu client gửi vào | Ops | P1 | ✅ `P7Tests` |
| F12-08 | `/health` (live) + `/health/ready` (DB) | Ops | P1 | ✅ `P7Tests` |
| F12-09 | `GlobalExceptionHandler` — lỗi chưa bắt → 500 envelope, **không lộ `ex.Message`** | Ops | P1 | 🔲 (cần endpoint ném lỗi có kiểm soát hoặc test qua trường hợp thật) |
| F12-10 | `AuditSaveChangesInterceptor` — đổi field nhạy cảm (role, status refund…) ghi `AuditLog` kể cả sửa DB trực tiếp | Ops | P1 | ✅ `P7Tests` |
| F12-11 | CORS: origin ngoài allowlist bị chặn preflight | Ops | P2 | 🔲 |
| F12-12 | Rate limiter 429 → trả vỏ `ApiResponse` tiếng Việt (không body rỗng) | Ops | P1 | 🔲 (đã kiểm thủ công) |
| F12-13 | `[Authorize]` fallback toàn cục: endpoint quên attribute vẫn cần đăng nhập | Ops | P1 | 🟡 gián tiếp |

---

## 5. Cấu trúc thư mục

Toàn bộ `Tests/` cũ **bị xoá**. Dự án mới `ELearning_ToanHocHay.Tests`:

```
ELearning_ToanHocHay.Tests/
├─ Unit/            → bố cục chi tiết: DANH-MUC-UNIT-TEST.md §5
│  ├─ Infrastructure/   (SqliteDb, TestConfig, Claims, Entities, Fakes, DataProtection)
│  └─ {Auth,Grading,Sepay,Refund,Content,Progress,Quota,Subscription,Access,Notification,Attempt,Common}/
└─ Integration/     → bố cục chi tiết: DANH-MUC-INTEGRATION-TEST.md §7
   ├─ Infrastructure/   (PostgresFixture, ApiFactory, FakeAiService, FakeEmailSink,
   │                     SeedData/SeededIds, FlowSeed, SePayIpn, IntegrationCollection, Envelope)
   └─ IT_F1..F12_*Tests.cs + IT_AuthorizationMatrixTests.cs
```

Không còn khái niệm "file hồi quy theo mã rà soát" — mọi test được tổ chức theo **tầng** (Unit/Integration)
và **luồng** (`Flow` trait). Không stub skipped; viết case nào xong case đó.

---

## 6. Ma trận vai trò × luồng (tham chiếu nhanh)

| Vai trò | F1 | F2 | F3 | F4 | F5 | F6 | F7 | F8 | F9 | F10 | F11 | F12 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Khách (ẩn danh) | đăng ký/đăng nhập | xem catalog + node free | — | — | — | — | — | — | — | — | — | envelope/health |
| Student | ✔ | ghi danh | học + tiến độ | làm bài | dashboard của mình | nhập code liên kết | mua gói | hoàn tiền của mình | hint/chat | nhận | ✗ (403) | — |
| Parent | ✔ | — | xem của con | xem history con | dashboard con | mời/thu hồi | — | — | chat | nhận | ✗ | — |
| ContentEditor | ✔ | — | — | — | — | — | — | — | — | — | soạn + submit | — |
| AcademicReviewer | ✔ | — | — | — | — | — | — | — | — | — | review/publish | — |
| SupportStaff | ✔ | — | — | — | — | — | — | — | staff queue | — | — | — |
| FinanceManager | ✔ | — | — | — | — | — | đối soát, package | duyệt/lô/CSV | — | — | — | — |
| SystemAdmin | ✔ + lock/role | full | — | — | — | xoá link | full | full + config | — | inactivity check | full + config | audit logs |

Từng ô "✗ (403)" và "—" (không có endpoint / 401) đều là **kịch bản phủ định cần test**
— dùng `IT_AuthorizationMatrixTests` data-driven ([DANH-MUC-INTEGRATION-TEST.md §5](DANH-MUC-INTEGRATION-TEST.md)).

---

## 7. Dữ liệu test cần bổ sung vào seed

`SeedData` (golden dataset mới — [DANH-MUC-INTEGRATION-TEST.md §2.5](DANH-MUC-INTEGRATION-TEST.md))
giữ tối thiểu. Các luồng cần thêm dữ liệu **tạo runtime qua `FlowSeed`** (không nhồi golden dataset):

| Cần | Cho luồng | Ghi chú |
|---|---|---|
| 1 khoá **Published** đầy đủ cây (chương free + chương trả phí, bài showcase 12 block) | F2, F3, F11 | `FlowSeed.PublishCourseAsync` |
| Học sinh có `PackageEntitlement(SubjectGrade, MATH, G6)` đang hiệu lực | F2, F4, F5 | phân biệt "ghi danh khoá" vs "có gói theo môn-lớp" |
| Exercise mỗi loại tier (Quiz free / Test Standard / Exam Premium) | F4 | test F4-02 |
| Attempt lịch sử rải 2 tuần (tuần này + tuần trước) | F5 | test so sánh xu hướng |
| `DailyActivitySnapshot` chuỗi có ngày trống | F5 | test streak |
| Payment `Completed` cũ > `maxPaymentAgeDays` | F8 | test F8-04 |
| Subscription `Disbursed`-refund quá hạn | F8 | test sweep F8-22 |
| Hội thoại chatbot đang mở | F9 | test escalation/assign |

---

## 8. Chạy test / CI

```bash
# tất cả (cần Docker; không có Docker → integration tự skip)
dotnet test

# một luồng
dotnet test --filter "Flow=Payment"

# chỉ unit thuần (không cần Docker) — chạy nhanh ở pre-commit
dotnet test --filter "Level=Unit"

# chỉ P1 (bắt buộc pass trước khi merge)
dotnet test --filter "Priority=P1"
```

- **CI**: job có Docker → chạy full. Job không Docker → chạy `Level=Unit` (gác cổng nhẹ).
- **Trước khi merge nhánh feature**: `Priority=P1` của luồng bị đụng phải xanh.
- Container Postgres tái dùng trong 1 lần chạy (`IntegrationCollection`) — ~15–20s khởi động 1 lần.
- **Không** chạy song song các class integration (env-var wiring không an toàn song song).

---

## 9. Lộ trình triển khai

| Giai đoạn | Nội dung | Kết quả |
|---|---|---|
| **G0 — Hạ tầng (xây mới)** | Dự án `ELearning_ToanHocHay.Tests`; hạ tầng Unit ([UNIT §1.3](DANH-MUC-UNIT-TEST.md)) + Integration ([IT §2](DANH-MUC-INTEGRATION-TEST.md)); 1 test khói mỗi tầng | `--filter "Level=Unit"` / `"Level=Integration"` chạy được |
| **G1 — Unit** | ~500 case `UT-*` theo [UNIT §6](DANH-MUC-UNIT-TEST.md) B1→B6 | Logic quyết định được khoá |
| **G2 — Integration luồng tiền (F7, F8)** | toàn bộ `IT-F7-*`, `IT-F8-*` | Luồng tiền phủ kín |
| **G3 — Integration học & tài khoản (F1–F4)** | `IT-F1..F4-*` | Vòng đời tài khoản + học end-to-end |
| **G4 — Integration dashboard & phụ huynh (F5, F6)** | `IT-F5/F6-*` — assert **số liệu** | |
| **G5 — Integration còn lại (F9–F12) + ma trận §5** | `IT-F9..F12-*` + `IT_AuthorizationMatrixTests` | |
| **G6 — System test** | `ST-E2E-01..04` trước, rồi PERF/SEC/RESIL/DATA P1 ([SYSTEM](DANH-MUC-SYSTEM-TEST.md)) | Hành trình cốt lõi xanh trên staging |

---

## 10. Ngoài phạm vi Unit + Integration (đã chuyển sang System test)

Các hạng mục sau **đã có danh mục riêng** ở [DANH-MUC-SYSTEM-TEST.md](DANH-MUC-SYSTEM-TEST.md):

- Test UI WebApp (Playwright) — `ST-E2E-*`, `ST-COMPAT-*`.
- Test tải / hiệu năng (k6) — `ST-PERF-*` (IPN burst, dashboard N+1, refund CSV lớn).
- Test hợp đồng với Flask AI thật + khả năng chịu lỗi — `ST-RESIL-*`.
- Test bảo mật (ZAP + checklist) — `ST-SEC-*`.
- Migration / rollback / backup-restore trên bản sao staging — `ST-DATA-08/09/13`.
```
