# Rà soát API & Kế hoạch kiểm soát dự án ToanHocHay

> **Bản nội bộ · không phân phối ra ngoài**

Đánh giá tính đúng đắn, mức đầy đủ và các lỗi logic của tầng API backend; kèm kế hoạch
khắc phục và kiểm thử chia thành 8 giai đoạn để theo dõi tiến độ theo từng chặng.

| | |
|---|---|
| **Stack** | .NET 8 Web API · EF Core · PostgreSQL · Flask AI (OpenAI/Gemini) |
| **Phạm vi rà soát** | 19 controller · ~20 service · SePay IPN · dịch vụ AI Flask |
| **Nhánh** | `main` (commit `4d1f037`) |
| **Ngày** | 03·09·2026 |
| **Trạng thái DB** | Chưa có dữ liệu thật — được phép reset |

**Quy ước mức độ:**

- 🔴 **Chặn phát hành** — phải sửa trước khi có người dùng thật
- 🟠 **Rủi ro cao** — sai nghiệp vụ / mất tính năng
- 🔵 **Cần dọn** — nợ kỹ thuật, không chặn

---

## Mục lục

1. [Tóm tắt điều hành](#0-tóm-tắt-điều-hành)
2. [A1 — Xác thực & phân quyền](#a1--xác-thực--phân-quyền)
3. [A2 — Lỗi logic nghiệp vụ](#a2--lỗi-logic-nghiệp-vụ)
4. [A3 — API còn thiếu](#a3--api-còn-thiếu)
5. [A4 — API thừa · trùng · code chết](#a4--api-thừa--trùng--code-chết)
6. [A5 — Vấn đề xuyên suốt](#a5--vấn-đề-xuyên-suốt)
7. [B — Kế hoạch kiểm soát theo 8 giai đoạn](#b--kế-hoạch-kiểm-soát-theo-8-giai-đoạn)
8. [C — Chiến lược kiểm thử tổng thể](#c--chiến-lược-kiểm-thử-tổng-thể)
9. [Phụ lục — Ma trận phân quyền đề xuất](#phụ-lục--ma-trận-phân-quyền-đề-xuất)

---

## 0. Tóm tắt điều hành

Phần lõi làm bài tập — bắt đầu, tự lưu, chấm điểm, xem kết quả, AI feedback — về cơ bản
chạy được và có xử lý các tình huống khó (resume bài dở, timeout, chuyển tab). Tuy nhiên
tầng API hiện **chưa sẵn sàng để mở cho người dùng thật** vì bốn nhóm vấn đề:

1. 🔴 **Gần như không có phân quyền.** Đa số controller (`User`, `Exercise`,
   `ExerciseAttempts`, `Subscription`, `Payment`, `Package`, `AIFeedback`, `AIHint`,
   `Chatbot`, `Dashboard` con) không gắn `[Authorize]`. Người lạ có thể liệt kê toàn bộ
   user, xoá user, huỷ subscription của người khác, tự đánh dấu payment "đã thanh toán",
   kích hoạt subscription mà không trả tiền, đọc bài làm và lịch sử của bất kỳ học sinh nào
   (IDOR toàn hệ thống).

2. 🔴 **Cập nhật user làm hỏng tài khoản.** `UserService.UpdateUserAsync` gán thẳng mật
   khẩu chưa hash vào `PasswordHash` và ghi đè mọi trường; luồng `update-profile` vô tình
   đặt `UserType = Student` và `IsActive = false` cho bất kỳ ai.

3. 🔴 **Thanh toán do client định giá.** `CreatePendingAsync` nhận `AmountPaid` từ client
   và không đối chiếu với `Package.Price`; QR và IPN đều dùng số tiền client gửi. Có thể
   mua gói Premium với giá 1 đồng.

4. 🟠 **Dashboard tiến độ & AI cao cấp đang rỗng.** Không có nơi nào ghi `NodeProgress`;
   `GetWeakTopicsAsync` / `GetFullPerformanceAsync` trả list rỗng (TODO). Hệ quả: "bài đã
   hoàn thành", "chủ đề yếu", AI assessment và AI roadmap luôn trả nhánh "chưa có dữ liệu".
   Chưa có API xem bài giảng (`ContentNode`) nào cả.

Kế hoạch bên dưới xử lý theo thứ tự rủi ro: **P0** bịt lỗ hổng bảo mật ngay trên codebase
hiện tại, **P1–P2** hoàn thiện tài khoản và tầng nội dung đang thiếu, **P3–P5** siết luồng
làm bài / tiến độ / thanh toán, **P6–P7** AI & phụ huynh & vận hành. Mỗi giai đoạn có tiêu
chí hoàn thành (DoD) và trọng tâm kiểm thử riêng.

---

## A1 — Xác thực & phân quyền

Đây là nhóm nghiêm trọng nhất. Cần một *ma trận phân quyền* (xem phụ lục) rồi áp
`[Authorize]` + `[AuthorizeUserType(...)]` cùng kiểm tra quyền sở hữu (ownership) cho mọi
endpoint.

| Mã | Mức | Vấn đề | Vị trí |
|---|---|---|---|
| **A1-01** | 🔴 | **UserController mở hoàn toàn.** Chỉ `POST /api/user` có `[AuthorizeUserType(SystemAdmin)]`. GET danh sách user, GET theo id/email, PUT, DELETE, POST `update-profile` đều ẩn danh. Bất kỳ ai cũng liệt kê / sửa / xoá được mọi tài khoản. | `Controllers/UserController.cs` |
| **A1-02** | 🔴 | **ExerciseAttempts không kiểm tra chủ sở hữu.** `start`, `start-random`, `save-answer`, `submit`, `complete`, `{id}/result`, `student/{studentId}/history`, `{id}/report-tab-switch` — không `[Authorize]`, `studentId` lấy từ body/URL. Học sinh A nộp bài hộ / xem lịch sử / xem kết quả của học sinh B; kẻ xấu spam email "chuyển tab" tới phụ huynh bất kỳ. | `Controllers/ExerciseAttemptsController.cs` |
| **A1-03** | 🔴 | **Subscription / Payment / Package sửa được ẩn danh.** `PUT /api/subscription/cancel/{id}`, `PATCH /api/subscription/{id}/status` (đặt thẳng `Active` không cần thanh toán), `GET /api/subscription` (toàn bộ), `PUT /api/payment/update-status/{id}` (đánh dấu `Completed`), `GET /api/payment` (toàn bộ dữ liệu tài chính), `POST/PUT/DELETE /api/package` — tất cả không xác thực. | `SubscriptionController.cs` · `PaymentController.cs` · `PackageController.cs` |
| **A1-04** | 🔴 | **Endpoint nội dung không giới hạn vai trò.** `ExercisesController` (tạo/sửa/xoá đề, thêm/bớt câu hỏi, đổi điểm) và `QuestionsController` (tạo câu hỏi hàng loạt) không có `[Authorize]`. Phải giới hạn `ContentEditor` / `AcademicReviewer`. | `ExercisesController.cs` · `QuestionsController.cs` |
| **A1-05** | 🔴 | **DashboardController: chỉ `overview` kiểm tra quyền.** `chapter-score-comparison`, `ai-assessment`, `ai-roadmap` **không gọi** `VerifyStudentAccessAsync`; chỉ kiểm tra gói *của học sinh mục tiêu*. Một user đã đăng nhập đọc được phân tích AI của học sinh khác chỉ bằng cách đổi `studentId` trên URL. | `Controllers/DashboardController.cs:63–96` |
| **A1-06** | 🔴 | **AI endpoint không xác thực, không giới hạn.** `AIFeedbackController`, `AIHintController`, `ChatbotController` mở ẩn danh — mỗi request tốn tiền OpenAI. Không kiểm `AiHintLimitDaily` / `UnlimitedAiHint` của gói. Dịch vụ Flask `:5001` cũng không có auth và bị publish port ra ngoài trong `docker-compose.yml`. | `AIFeedbackController.cs` · `AIHintController.cs` · `ChatbotController.cs` · `docker-compose.yml` |
| **A1-07** | 🟠 | **Refresh token không thực sự hoạt động.** `RefreshTokenAsync` nhận access token cũ và gọi `ValidateToken` với `ValidateLifetime = true` → khi token hết hạn thì refresh cũng fail. Không có entity refresh token, không xoay vòng, không thu hồi. `LogoutAsync` không làm gì (không blacklist). Đổi mật khẩu xong token cũ vẫn dùng được. | `Services/Implementations/AuthService.cs:188` · `JwtService.cs` |
| **A1-08** | 🟠 | **Đăng nhập không giới hạn số lần thử.** Không đếm lần sai, không khoá tạm, không rate-limit, không CAPTCHA. README của chính dự án cũng ghi nhận thiếu điểm này. | `AuthController.cs` / `AuthService.LoginAsync` |
| **A1-09** | 🔵 | **Controller debug lộ cấu hình nhạy cảm.** `EmailDebugController.GetConfig` trả về username SMTP, độ dài mật khẩu và 4 ký tự đầu mật khẩu. `EmailTestController`, `ExampleController` là code mẫu. Xoá hoặc chặn bằng cờ môi trường trước khi lên production. | `EmailDebugController.cs` · `EmailTestController.cs` · `ExampleController.cs` |
| **A1-10** | 🔵 | **CORS phản chiếu mọi origin kèm credentials.** `SetIsOriginAllowed(origin => true)` + `AllowCredentials()` — chấp nhận bất kỳ website nào gọi API kèm cookie/token. Thay bằng danh sách origin cụ thể. | `Program.cs:130–139` |
| **A1-11** | 🔵 | **Endpoint `/api/auth/me` trả về null email & userType.** Đọc `User.FindFirst("Email")` / `"UserType"` nhưng claim thực tế là `email` (đã map sang `ClaimTypes.Email`) và `user_type`. Dùng đúng hằng trong `CustomJwtClaims`. | `AuthController.cs:83–89` · `Common/CustomJwtClaims.cs` |

---

## A2 — Lỗi logic nghiệp vụ

| Mã | Mức | Vấn đề | Vị trí |
|---|---|---|---|
| **A2-01** | 🔴 | **UpdateUserAsync lưu mật khẩu thô & ghi đè toàn bộ trường.** `user.PasswordHash = updateUserDto.Password;` — không hash. Gán không điều kiện `Phone, Dob, AvatarUrl, UserType, IsActive`. Luồng `update-profile` tạo `UpdateUserDto` chỉ có `FullName` và `Password = ""` → xoá hash mật khẩu, đặt `UserType = Student` (enum 0), `IsActive = false`. | `Services/Implementations/UserService.cs:163–210` · `UserController.cs:90` |
| **A2-02** | 🔴 | **Giá subscription do client quyết định.** `CreatePendingAsync` dùng `dto.AmountPaid` làm số tiền của `Payment`, `Subscription.AmountPaid`, QR và điều kiện đối chiếu IPN. Server không bao giờ so với `package.Price`. Cũng không kiểm caller có sở hữu `dto.StudentId` hay không. | `Services/Implementations/SubscriptionPaymentService.cs:33` |
| **A2-03** | 🟠 | **Bài tập ngẫu nhiên không thể lưu & luôn bị timeout.** `StartRandomExerciseAsync` tạo `ExerciseAttempt` nhưng **không set `PlannedEndTime`** (mặc định `0001-01-01`). `SaveAnswerAsync` chặn khi `PlannedEndTime <= UtcNow` → luôn đúng → không lưu được câu nào. `CompleteExerciseAsync` tính `isTimeout = now >= PlannedEndTime` → luôn timeout. | `ExerciseAttemptService.cs:590–665` |
| **A2-04** | 🟠 | **Nộp bài chờ AI chấm xong mới trả kết quả.** `CompleteExerciseAsync` gọi `await Task.WhenAll(aiTasks)` cho từng câu sai, mỗi call HttpClient timeout 60s. Với nhiều câu sai, request `complete` treo 1–3 phút hoặc lỗi. AI feedback nên chạy nền thật (hàng đợi), trả kết quả ngay. | `ExerciseAttemptService.cs:204–246` |
| **A2-05** | 🟠 | **Suy ra loại gói bằng so khớp chuỗi tên.** `name.Contains("premium")`, `"tiêu chuẩn"`… trong khi `Package.Tier` (enum `Free/Standard/Premium`) đã tồn tại. Trong `LoginAsync` còn có nhánh `name.Contains("Gói Premium")` chạy sau `.ToLower()` nên không bao giờ khớp. `SubscriptionInfoHelper` lại map cứng `PackageId 1/2/3` thành tier. Ba nơi ba kiểu. | `AuthService.cs:128` · `CoreDashboardService.cs:159` · `SubscriptionService.cs:180` |
| **A2-06** | 🟠 | **Tiến độ node không bao giờ được ghi.** Không có `ProgressProjectionService`. Luồng submit không đụng tới `NodeProgress`. Nhưng `DashboardRepository` đọc `NodeProgresses` cho "bài học hoàn thành", "tiến độ chương", "bài gần đây" → luôn 0/rỗng. `GetWeakTopicsAsync` / `GetFullPerformanceAsync` trả rỗng (TODO GĐ2) → `ai-assessment`, `ai-roadmap` luôn rơi vào nhánh "chưa có dữ liệu". | `DashboardRepository.cs:67,123,141,209,215` · `ExerciseAttemptService.cs` (submit) |
| **A2-07** | 🟠 | **Hai đường nộp bài song song, hành vi khác nhau.** `/complete` chấm điểm đầy đủ + AI + phần trăm + đổi `Status`. `/submit` (`SubmitExamAsync`) chỉ lưu `SelectedOptionId`, **không chấm, không đổi Status**. `/submit-answer` gần trùng `/save-answer` nhưng thiếu kiểm tra hết giờ. Controller tự ghi chú `/submit-answer` là "bản cũ". | `ExerciseAttemptsController.cs:53,83` · `ExerciseAttemptRepository.cs:67` |
| **A2-08** | 🟠 | **MaxAttempts & "chỉ làm 1 lần" không được thực thi.** Đoạn chặn làm lại trong `StartExerciseAsync` bị comment; `Exercise.MaxAttempts` có trường nhưng không nơi nào kiểm. Bài Test/Exam có thể làm lại vô hạn để dò đáp án. | `ExerciseAttemptService.cs:513–526` |
| **A2-09** | 🟠 | **Chấm điểm thiếu loại câu hỏi & chuẩn hoá đáp án.** `switch (question.QuestionType)` chỉ xử lý `MultipleChoice`, `TrueFalse`, `FillBlank`. `Essay` luôn bị tính sai (0đ). `TrueFalse` so sánh text nhưng UI có thể gửi `SelectedOptionId`. `FillBlank` chỉ so khớp chính xác sau `Trim().ToLower()` — không chấp nhận "0.5" vs "1/2", dấu cách, dấu phẩy thập phân. | `ExerciseAttemptService.cs:123–144` |
| **A2-10** | 🟠 | **IPN SePay: sync, không transaction, dễ NRE.** `_context.SaveChanges()` (đồng bộ) trong action async; không transaction; truy cập `subscription.Payment.Status` — NRE nếu repo không `Include(Payment)`. `EndDate = UtcNow.AddMonths(1)` bỏ qua `Package.DurationDays`. Không lưu log IPN thô, không đảm bảo `referenceCode` duy nhất (có thể replay sang subscription pending khác). | `Controllers/SepayController.cs:36–79` |
| **A2-11** | 🟠 | **Không có vòng đời subscription.** Không job hết hạn: `Pending` treo vĩnh viễn, `Active` không tự chuyển `Expired` khi qua `EndDate` (chỉ "hết hạn" gián tiếp trong truy vấn đọc). Cho phép nhiều subscription `Active` cùng lúc; `GetActivePackageAsync` sắp theo `CreatedAt` còn login sắp theo `EndDate` — không nhất quán. | `PackageRepository.cs:48` · `AuthService.cs:118` |
| **A2-12** | 🔵 | **Link xác nhận email không nhất quán.** `RegisterAsync` tạo link `{BaseUrl}/Account/ConfirmEmail?token=` (route MVC WebApp) còn API là `GET /api/auth/confirm-email` và `EmailTestController` lại dùng `/api/auth/confirm-email`. `ResendConfirmationEmailAsync` tồn tại trong service nhưng **không có endpoint**. | `AuthService.cs:333,406` · `AuthController.cs:155` |
| **A2-13** | 🔵 | **CreatedBy hard-code.** `ExerciseService.CreateExerciseAsync` đặt `CreatedBy = 3`; `QuestionService` đặt `CreatedBy = 6`. Sẽ lỗi khoá ngoại nếu user đó không tồn tại và gán sai người tạo. Lấy từ token. | `ExerciseService.cs:73` · `QuestionService.cs:34,85` |
| **A2-14** | 🔵 | **N+1 query trong dashboard học sinh.** `ExerciseAttemptService.GetDashboardStatsAsync` lặp `foreach (ch in allChapters)` và query `Exercises` mỗi vòng. Trùng chức năng với `DashboardController` — nên gộp về một service. | `ExerciseAttemptService.cs:876–899` |
| **A2-15** | 🔵 | **Rò rỉ chi tiết lỗi ra client.** Nhiều service trả `ex.Message` / `ex.InnerException.Message` trong `ApiResponse.Errors` (rõ nhất là `RegisterAsync`). Cần global exception handler + `ProblemDetails`, log chi tiết ở server, trả thông điệp chung cho client. | `AuthService.cs:345` · nhiều service |

---

## A3 — API còn thiếu

So với mô hình 7 vai trò trong `docs/Khung-chuong-trinh-thiet-ke-lai.md`, các nhóm sau chưa
có controller/service nào:

### Thiếu và chặn luồng chính

- **Tầng nội dung học.** Không có API đọc `ContentNode` (chương/chủ đề/bài học),
  `ContentBlock`, `LessonResource`, `FlashcardDeck`. Học sinh không xem được bài giảng qua
  API.
- **Ghi danh khoá học.** Không có API tạo `StudentCourse` — mà dashboard lại phụ thuộc bảng
  này để biết học sinh học chương nào.
- **Ghi tiến độ.** Thiếu API "đánh dấu hoàn thành bài học" và `ProgressProjectionService`
  chạy sau mỗi lần submit (xem A2-06).
- **Catalog.** CRUD/duyệt `Subject`, `GradeLevel`, `Course`, `CourseVersion`.

### Thiếu theo vai trò

- **Auth:** quên mật khẩu / đặt lại mật khẩu, gửi lại email xác nhận, refresh-token đúng
  nghĩa.
- **Phụ huynh:** nhập mã liên kết con (`ParentLink`), chấp nhận `ParentInvite`,
  `GET /api/parent/{id}/children/overview` (tổng hợp nhiều con). `ParentController` hiện chỉ
  có Get/Update/Delete.
- **Học sinh:** "gói hiện tại của tôi" và "lịch sử thanh toán của tôi" không cần truyền
  `studentId`.
- **Question bank:** CRUD ngân hàng câu hỏi; sửa/xoá/list/get câu hỏi; workflow duyệt
  (approve/reject) — `Question.Status = PendingReview` nhưng không có nơi duyệt.
- **Exercise:** publish/unpublish, lấy danh sách câu hỏi của đề (đang bị comment), lấy đề để
  chỉnh sửa (kèm đáp án).
- **Admin:** khoá/mở khoá tài khoản (`User.LockedAt` có sẵn), đổi vai trò + ghi `AuditLog`,
  xem log, `SystemConfig`.
- **Thông báo / Hỗ trợ / Duyệt nội dung:** chưa có (nằm ở giai đoạn sau theo roadmap).
- **Vận hành:** endpoint health/readiness cho chính API (`/health`).

### Trạng thái triển khai (cập nhật)

> Nhánh `feat/a3-p2-content-layer`. Build xanh, 58/58 test xanh (có `A3ContentLayerTests`).

| Hạng mục A3 (thuộc P2) | Trạng thái | Ghi chú |
|---|---|---|
| Catalog CRUD (Subject/GradeLevel/CurriculumFramework) | ✅ | `CatalogController` — đọc công khai bản active, ghi cần content role |
| Course + CourseVersion CRUD | ✅ | `CoursesController` — slug + (môn×lớp×bộ sách) unique |
| Workflow version Draft→InReview→Approved→Published | ✅ | publish trong transaction, archive version cũ, lật `Course.Status` |
| Clone cây nội dung khi tạo version | ✅ | `CloneFromVersionId` |
| Cây nội dung: ContentNode (NodeTypeRule + MaterializedPath/Depth) | ✅ | `ContentAuthoringController`, chặn sửa khi version ≠ Draft |
| ContentBlock / LessonResource / FlashcardDeck + Flashcard CRUD | ✅ | |
| `IContentAccessService` 3 bậc (ẩn danh / đăng ký / entitlement) | ✅ | enrolment `StudentCourse` hoặc `PackageEntitlement` trên sub active |
| StudentCourse ghi danh + "khoá của tôi" | ✅ | `EnrollmentController` |
| Tiêu thụ nội dung (cây published + node detail, có gating) | ✅ | `LearnController` (công khai) |
| QuestionBank CRUD + Question CRUD + workflow duyệt câu hỏi | ✅ | `QuestionBanksController` (Draft→PendingReview→Approved/Rejected) |
| Exercise publish/unpublish + lấy đề kèm đáp án + list câu hỏi của đề | ✅ | `ExercisesController` `{id}/publish`, `{id}/unpublish`, `{id}/for-edit`, `{id}/questions` |
| A2-13 — `CreatedBy` lấy từ token | ✅ | Exercise + Question service |
| **Còn lại (chưa làm trong đợt này)** | ⏳ | Duyệt `CourseVersion` bằng `ReviewComment` neo theo node/block; `LessonResource` gắn `MediaAsset` (upload file); re-parent node + rewrite `MaterializedPath`; `CurriculumFramework` gắn `Course` nhiều-nhiều; `NodeRevision` (diff/rollback); `ContentImportJob` (import hàng loạt); phân trang cho danh sách course/node |

---

## A4 — API thừa · trùng · code chết

- `POST /api/exerciseattempts/submit-answer` — trùng `/save-answer`, thiếu kiểm tra hết
  giờ. **Xoá.**
- `POST /api/exerciseattempts/submit` vs `/complete` — gộp về một đường nộp bài duy nhất.
- `StudentController.GetDashboardStats` (`GET /api/Student/dashboard-stats`) vs
  `DashboardController` — hai bản dashboard chồng nhau, chọn một.
- `ExampleController` — controller mẫu, xoá trước production.
- `EmailTestController`, `EmailDebugController` — chỉ dùng khi dev.
- `AI/AI_main.py` (port 5000, thiếu route chatbot/insights/batch) — đã bị
  `Main_AI_Service.py` (port 5001) thay thế. Xoá để tránh nhầm; sửa default URL trong
  `AIService.cs` (`localhost:5000`) cho khớp `5001`.
- `PackageRepository.GetActivePackageTypeAsync` — ép `PackageId` (khoá dòng) thành
  `PackageType`, sai khái niệm; có vẻ không được dùng.
- `Program.cs` — đăng ký `IParentRepository` hai lần.
- `CoreDashboardService.GetAIInsightAsync` / `GetAIRoadmapAsync` — hiện là code chết vì dữ
  liệu đầu vào rỗng (A2-06).
- `RecentLessonModel.ChapterName` luôn rỗng trong repo.

---

## A5 — Vấn đề xuyên suốt

- **Vỏ response không nhất quán.** Chỗ trả `ApiResponse<T>`, chỗ trả object ẩn danh
  (`SubscriptionController.status`, `StudentSubscriptionController`, `DashboardController`).
  Frontend phải xử lý hai kiểu.
- **Mã HTTP không đúng ngữ nghĩa.** Nhiều action trả `Ok()` (200) kể cả khi
  `Success = false` hoặc không tìm thấy (`ParentController`,
  `AIFeedbackController.GetByAttempt`, các filter của `ExercisesController`).
  `UserController.GetByEmail` trả `404` còn `GetById` trả `400` cho cùng tình huống.
- **Route lộn xộn.** `api/auth`, `api/User`, `api/Questions`, `api/Subscription`; ba gốc
  route khác nhau cho "student": `api/Student`, `api/student/{id}/dashboard`,
  `api/student/{id:int}`. Thống nhất kebab-case số nhiều.
- **Không phân trang.** Mọi `GetAll` (user, payment, subscription, exercise, lịch sử làm
  bài) trả toàn bộ.
- **Không có `AuditLog` interceptor** dù schema đã có — nền cho yêu cầu "admin xem toàn bộ
  log".
- **Bí mật trong `appsettings.json`** (JWT SecretKey commit vào repo). Chuyển hẳn sang biến
  môi trường / secret store; xoay khoá.
- **Chatbot không lưu hội thoại phía C#;** Python giữ `UserState` trong RAM, mất khi restart
  và không chia sẻ giữa 2 worker gunicorn.

---

## B — Kế hoạch kiểm soát theo 8 giai đoạn

Thứ tự là thứ tự *triển khai*, theo mức rủi ro. Mỗi giai đoạn kết thúc bằng một mốc kiểm thử
và tiêu chí hoàn thành (DoD) rõ ràng — chỉ chuyển giai đoạn khi DoD đạt. Ước lượng thời gian
giả định 1–2 lập trình viên.

### P0 — Bịt lỗ hổng bảo mật trên codebase hiện tại

> **Mục tiêu:** Không thêm tính năng. API hiện tại đủ an toàn để demo nội bộ / cho người
> dùng thử. **~1–1.5 tuần.**

**Việc chính**

- Lập *ma trận phân quyền* (phụ lục) → áp `[Authorize]` mặc định toàn cục, whitelist
  `[AllowAnonymous]` cho login/register/confirm-email/IPN.
- Thêm kiểm tra ownership: helper `CurrentStudentId()` / `CurrentUserId()` từ claim; mọi
  endpoint có `studentId` / `attemptId` phải đối chiếu (học sinh chỉ thao tác dữ liệu của
  mình; phụ huynh qua `ParentLink.Status = Active`).
- Sửa A2-01: tách `ChangePassword` khỏi `UpdateUser`; `UpdateProfile` chỉ cập nhật trường
  được phép (patch semantics); không bao giờ gán `PasswordHash` từ DTO thô.
- Sửa A2-02: server tự lấy giá từ `Package.Price`; bỏ `AmountPaid` khỏi request tạo
  subscription.
- Gắn `VerifyStudentAccessAsync` vào **mọi** endpoint dashboard (A1-05).
- Xoá `ExampleController`; chặn `EmailDebug/EmailTest` sau `if (env.IsDevelopment())`; bỏ
  password preview.
- CORS: danh sách origin cụ thể. Bí mật: chuyển ra env, xoay JWT key.
- Global exception handler + `ProblemDetails`; ngừng trả `ex.Message`.
- AI endpoint: yêu cầu đăng nhập + rate-limit tối thiểu; Flask `:5001` bỏ publish port ra
  ngoài, thêm shared secret header.

**Trọng tâm kiểm thử**

- Bộ test "ma trận phân quyền": với mỗi endpoint × mỗi vai trò (ẩn danh, student A,
  student B, parent liên kết, parent không liên kết, admin) → khẳng định 200 / 401 / 403
  đúng kỳ vọng.
- Test IDOR: student A dùng token của mình gọi tài nguyên của student B → 403.
- Test giá: tạo subscription rồi kiểm `Payment.Amount == Package.Price`.
- Smoke regression toàn bộ luồng làm bài (start → save → complete → result).

**✅ Definition of Done**

Không endpoint nào trả dữ liệu/ghi dữ liệu khi thiếu quyền. Bộ test ma trận phân quyền xanh
100%. Quét `/security-review` không còn mục Critical/High. Không còn bí mật trong repo.

---

### P1 — Hoàn thiện tài khoản & xác thực

> **Mục tiêu:** Vòng đời tài khoản đầy đủ và an toàn. **~1.5 tuần.**

**Việc chính**

- Refresh token thật: entity `RefreshToken` (hash, hạn, thu hồi, xoay vòng), access token
  ngắn hạn (15–30 phút). `Logout` thu hồi. Đổi mật khẩu → thu hồi toàn bộ refresh token.
- Thống nhất luồng xác nhận email (A2-12); thêm endpoint gửi lại; thêm quên/đặt lại mật
  khẩu.
- Giới hạn đăng nhập: đếm lần sai + khoá tạm theo thời gian tăng dần / theo IP.
- Admin: khoá/mở khoá tài khoản, đổi vai trò (ghi `AuditLog`).
- Sửa `/api/auth/me` (A1-11); chuẩn hoá đọc claim qua một extension duy nhất.

**Trọng tâm kiểm thử**

- Unit: `PasswordHasher` (hash/verify, chống null), `JwtService` (claim đúng, chữ ký, hết
  hạn, sai issuer/audience).
- Integration end-to-end: register → nhận token email → confirm → login → gọi API có quyền
  → access hết hạn → refresh → change-password → refresh cũ bị từ chối → logout.
- Negative: email trùng (đã xác nhận / chưa xác nhận), token confirm hết hạn/đã dùng, đăng
  nhập sai 5 lần → khoá, reset password bằng token cũ.

**✅ Definition of Done**

Không dùng lại được token sau logout/đổi mật khẩu. Brute-force login bị chặn. Toàn bộ kịch
bản e2e xác thực tự động xanh.

**Trạng thái triển khai (cập nhật)** — nhánh `feat/a3-p2-content-layer`, migration `P1_AuthTokens`, test `P1AuthTests` (6):

| Hạng mục P1 | Trạng thái | Ghi chú |
|---|---|---|
| Refresh token thật (A1-07) | ✅ | entity `RefreshToken` (hash SHA-256, xoay vòng mỗi lần dùng, phát hiện replay → thu hồi cả họ); access token 30 phút; `RefreshTokenDays=30` |
| Logout / đổi mật khẩu / reset → thu hồi refresh token | ✅ | logout thu hồi 1 hoặc tất cả |
| Thống nhất luồng xác nhận email (A2-12) | ✅ | link trỏ `/api/auth/confirm-email`; resend cùng route |
| Endpoint gửi lại email xác nhận | ✅ | `POST /api/auth/resend-confirmation` |
| Quên / đặt lại mật khẩu | ✅ | `PasswordResetToken` (1h, 1 lần), không lộ email tồn tại; `forgot-password` / `reset-password` |
| Giới hạn đăng nhập (A1-08) | ✅ | `FailedLoginCount` + `LockoutEndsAt` tăng dần 1→30 phút sau 5 lần sai; rate-limit `auth` cấu hình được |
| Admin khoá/mở khoá + đổi vai trò + ghi `AuditLog` | ✅ | `AdminController` (`/api/admin/users/{id}/lock|unlock|role`, `/api/admin/audit-logs`) |
| Sửa `/api/auth/me` (A1-11) + extension claim duy nhất | ✅ | đọc qua `ClaimsPrincipalExtensions` |
| **Còn lại** | ⏳ | JWT blacklist cho access token đang hành (hiện chỉ ngắn hạn 30′); `AuditLog` interceptor tự động (P7); đổi vai trò giữa learner↔staff (đang chặn để không mồ côi Student/Parent) |

> ⚠️ `JwtSettings:ExpirationMinutes` đổi từ 1440 → **30**. Frontend phải dùng luồng refresh token (`LoginResponse.RefreshToken` + `POST /api/auth/refresh-token`).

---

### P2 — Tầng nội dung học (đang thiếu hoàn toàn)

> **Mục tiêu:** Có API để tạo và tiêu thụ nội dung: Catalog → Course → ContentNode →
> Exercise. **~3 tuần.**

**Việc chính**

- CRUD + duyệt `Subject`, `GradeLevel`, `CurriculumFramework`, `Course`, `CourseVersion`.
- API cây nội dung: đọc `ContentNode` (theo course/version, theo cha), `ContentBlock`,
  `LessonResource`, `FlashcardDeck`; ràng buộc cây (depth, materialized path).
- `StudentCourse`: ghi danh, danh sách khoá đang học.
- Question bank + Question CRUD đầy đủ; workflow duyệt câu hỏi và `CourseVersion`
  (Draft → InReview → Approved → Published).
- Exercise: publish/unpublish, list câu hỏi của đề, `CreatedBy` từ token (A2-13).
- `IContentAccessService` tập trung: quyết định 3 bậc truy cập (ẩn danh / đã đăng ký / đã
  mua) theo `IsFree` + entitlement.

**Trọng tâm kiểm thử**

- Lifecycle nội dung: editor tạo course/version/node/block → reviewer duyệt → publish → học
  sinh chưa mua chỉ thấy node `IsFree` → học sinh đã ghi danh thấy đầy đủ.
- Toàn vẹn cây: không tạo được node sai loại cha, `MaterializedPath` đúng, xoá chương → xử
  lý node con.
- Gating: gọi node/exercise không thuộc quyền → 403; guest vượt hạn mức → 429/403.

**✅ Definition of Done**

Có thể seed một khoá "Toán 6" hoàn chỉnh qua API và một học sinh mới đọc được bài giảng miễn
phí. `IContentAccessService` có test cho cả 3 bậc.

---

### P3 — Siết luồng làm bài & chấm điểm

> **Mục tiêu:** Một đường nộp bài, chấm điểm đúng mọi loại câu hỏi, chống gian lận cơ bản.
> **~2 tuần.**

**Việc chính**

- Gộp `/submit` + `/complete` thành một; xoá `/submit-answer` (A2-07).
- Sửa bug bài ngẫu nhiên không set `PlannedEndTime` (A2-03).
- Thực thi `MaxAttempts`, trạng thái `Published/IsActive`, tier truy cập (A2-08).
- Chuyển AI feedback sang hàng đợi nền — `complete` trả ngay, feedback bổ sung sau; endpoint
  poll trạng thái feedback (A2-04).
- Chuẩn hoá chấm điểm: xử lý mọi `QuestionType` gồm `Essay` (đánh dấu "chờ chấm tay"); quy
  tắc chuẩn hoá `FillBlank` (số thập phân, phân số, khoảng trắng); thống nhất `TrueFalse`
  dùng option (A2-09).
- `report-tab-switch`: auth + chống spam (debounce, giới hạn email/attempt).

**Trọng tâm kiểm thử**

- Ma trận chấm điểm: mỗi loại câu × (đúng / sai / bỏ trống) × (nộp thường / hết giờ) → điểm
  và `CompletionPercentage` đúng.
- Resume: start → save vài câu → start lại → nhận đúng attempt dở dang với câu đã lưu.
- Concurrency: gọi `complete` hai lần song song → chỉ một lần chấm, lần sau báo "đã nộp".
- Bài ngẫu nhiên: start-random → save-answer thành công → complete cho điểm đúng (không
  timeout ảo).
- MaxAttempts = 1: làm lần 2 bị từ chối.

**✅ Definition of Done**

Chỉ còn một endpoint nộp bài. Bộ test ma trận chấm điểm xanh. `complete` phản hồi < 2s bất
kể số câu sai.

**Trạng thái triển khai (cập nhật)**

| Hạng mục P3 | Trạng thái | Ghi chú |
|---|---|---|
| Gộp `/submit` + `/complete`, xoá `/submit-answer` (A2-07) | ✅ | làm ở đợt A2 |
| Bug bài ngẫu nhiên `PlannedEndTime` (A2-03) | ✅ | |
| `MaxAttempts` + `Published/IsActive` (A2-08) | ✅ | kiểm trong `StartExerciseAsync` |
| **Tier truy cập** (A2-08 phần còn lại) | ✅ | `StartExerciseAsync` từ chối khi tier gói < `Exercise.RequiredTier` (bài free được miễn) |
| AI feedback hàng đợi nền (A2-04) | ✅ | |
| Chuẩn hoá chấm điểm mọi `QuestionType` (A2-09) | ✅ | `AnswerGrading` |
| `report-tab-switch`: auth | ✅ | làm ở đợt A1 |
| `report-tab-switch`: chống spam | ✅ | debounce 15s + ngừng gửi email sau 5 lần/attempt (log vẫn ghi đủ) |
| **Còn lại** | ⏳ | test concurrency "gọi `complete` 2 lần song song" chưa có; AI feedback vẫn chạy trong process (hàng đợi in-memory, mất khi restart) |

---

### P4 — Tiến độ & Dashboard chạy trên dữ liệu thật

> **Mục tiêu:** Bỏ mọi số liệu rỗng/giả; dashboard và AI cao cấp có dữ liệu thật.
> **~2.5 tuần.**

**Việc chính**

- `ProgressProjectionService`: sau mỗi submit và mỗi lần hoàn thành bài học → cập nhật
  `NodeProgress` node lá rồi roll-up theo `MaterializedPath` (A2-06).
- API "đánh dấu hoàn thành bài học" có ngưỡng thời gian xem (bỏ `isCompleted = true`
  hard-code).
- Gộp về một `DashboardService`; bỏ N+1 (A2-14); truy vấn tiến độ chương/điểm yếu/hiệu suất
  thật → mở khoá `ai-assessment`, `ai-roadmap`.
- Thay toàn bộ so khớp chuỗi tên gói bằng `Package.Tier` (A2-05).
- `DailyActivitySnapshot` + heatmap; tính streak từ snapshot thay vì quét toàn bộ attempt.

**Trọng tâm kiểm thử**

- Projection: submit một bài trong chương X → `NodeProgress` của bài, chủ đề, chương cập
  nhật đúng; hoàn thành hết bài của chương → chương = 100%.
- Thống kê: dựng dữ liệu 2 tuần cố định → weekly stats, so sánh tuần, streak, điểm TB khớp
  giá trị tính tay.
- Tier gating: Free không thấy `Charts` / `AIInsights` link; Premium gọi `ai-roadmap` nhận
  nội dung AI thật (mock AI service).
- Phụ huynh: xem dashboard con đã liên kết OK, con chưa liên kết → 403.

**✅ Definition of Done**

Không còn field dashboard trả 0/rỗng do thiếu projection. `ai-assessment` / `ai-roadmap` gọi
AI với dữ liệu thật (kiểm qua mock). Không còn `Contains("premium")` trong codebase.

---

### P5 — Thanh toán & Subscription hoàn chỉnh

> **Mục tiêu:** IPN chắc chắn, vòng đời subscription tự động, có thể đối soát. **~2 tuần.**

**Việc chính**

- IPN: async + transaction; bảng `SePayIpnLog` lưu payload thô; `referenceCode` duy nhất
  (idempotent theo giao dịch, không chỉ theo subscription); `EndDate` theo
  `Package.DurationDays`; dung sai số tiền cấu hình được (A2-10).
- Job nền: hết hạn `Active` → `Expired`; giải phóng `Pending` quá hạn (vd 30 phút).
- Guard: một học sinh một subscription `Active` tại một thời điểm (hoặc theo entitlement nếu
  mở nhiều môn); thống nhất tie-break (A2-11).
- Endpoint "gói của tôi", "lịch sử thanh toán của tôi"; luồng phụ huynh đứng tên trả
  (`Payment.PaidByUserId`).
- (Nếu trong phạm vi) `Order` / `OrderItem` cho mua khoá lẻ.

**Trọng tâm kiểm thử**

- IPN: đúng → Active; trùng referenceCode → bỏ qua; sai số tiền → không kích hoạt;
  subscription không tồn tại → 200 "not found"; giao dịch "out" → bỏ qua; sai API key → 401.
- State machine subscription: mọi chuyển trạng thái hợp lệ/không hợp lệ theo bảng `allowed`.
- Job hết hạn: subscription qua `EndDate` → chuyển `Expired`, dashboard mất quyền Premium.
- Giá: không tạo được subscription với giá khác `Package.Price`.

**✅ Definition of Done**

Chạy lại toàn bộ payload IPN mẫu → trạng thái hệ thống ổn định & idempotent. Có báo cáo đối
soát: tổng `Payment.Completed` == tổng subscription `Active` hợp lệ.

---

### P6 — AI, Phụ huynh, Thông báo

> **Mục tiêu:** Hoàn thiện các tính năng quanh người học. **~2.5 tuần.**

**Việc chính**

- AI hint/feedback: thực thi `AiHintLimitDaily` / `UnlimitedAiHint` theo gói; bảng đếm lượt
  dùng / ngày; log chi phí; shared secret C# ↔ Python; giữ Python trong mạng nội bộ.
- Chatbot: lưu `ChatConversation` / `ChatMessage` phía C#; health check; xử lý Python
  restart.
- Phụ huynh: nhập mã liên kết con (`ParentLink` Pending → Active), `ParentInvite`,
  `GET /api/parent/{id}/children/overview`.
- `Notification` + rule engine: sinh thông báo theo luật (chuyển tab, điểm < 5, nghỉ 3
  ngày), `Audience` Student/Parent/Both; tuỳ chọn nhận thông báo.
- Chuyển email chuyển-tab sang hàng đợi nền (hiện gọi đồng bộ trong request).

**Trọng tâm kiểm thử**

- Giới hạn hint: gói Free hết hạn mức → 429; Premium `UnlimitedAiHint` → không chặn; reset
  theo ngày.
- Liên kết phụ huynh: mã đúng/sai/hết hạn; revoke → mất quyền xem dashboard con ngay.
- Rule engine: dựng sự kiện điểm thấp → đúng số thông báo, đúng người nhận.
- AI service down → API vẫn trả kết quả bài làm, feedback ở trạng thái "đang xử lý".

**✅ Definition of Done**

Không gọi AI vượt hạn mức gói. Phụ huynh liên kết/huỷ liên kết phản ánh tức thì vào quyền
truy cập. Thông báo sinh đúng luật (test rule engine xanh).

---

### P7 — Vận hành & chất lượng

> **Mục tiêu:** Sẵn sàng production thật sự. **~1.5 tuần + duy trì.**

**Việc chính**

- Phân trang / lọc / sắp xếp cho mọi endpoint danh sách.
- Chuẩn hoá vỏ response & mã HTTP (A5); dọn route về kebab-case.
- `AuditLog` qua `SaveChanges` interceptor cho entity nhạy cảm.
- Health/readiness, structured logging + correlation id, cấu hình Serilog.
- Dọn Swagger (nhóm tag, ẩn endpoint dev), xoá `AI_main.py`, `ExampleController`.
- Load test luồng cao điểm (nhiều học sinh nộp bài cùng lúc); tinh chỉnh index DB.

**Trọng tâm kiểm thử**

- Contract test: snapshot schema response của mọi endpoint (chặn thay đổi vỡ frontend).
- Load: 200 học sinh đồng thời start/submit → p95 < ngưỡng, không deadlock.
- Chạy lại toàn bộ regression suite P0–P6.
- `/security-review` lần cuối trên nhánh release.

**✅ Definition of Done**

Toàn bộ regression suite xanh trong CI. Load test đạt ngưỡng. Swagger sạch, không endpoint
dev. Có dashboard log/metric cơ bản.

---

## C — Chiến lược kiểm thử tổng thể

### Kim tự tháp test

- **Unit (nhiều nhất).** Logic thuần: chấm điểm, chuẩn hoá đáp án, tính streak/thống kê
  tuần, state machine subscription, `PromotionEngine` (khi có), map tier từ `Package.Tier`,
  `PasswordHasher`, `JwtService`, `SePayService` (regex, đối chiếu số tiền). Không chạm DB —
  mock repository.
- **Integration (trung bình).** `WebApplicationFactory` + PostgreSQL thật qua
  [Testcontainers](https://dotnet.testcontainers.org/) (không dùng InMemory — khác biệt
  Npgsql). Mỗi test tự seed và rollback. Bao phủ: từng luồng nghiệp vụ đầu-cuối, ma trận
  phân quyền, IPN, projection tiến độ.
- **E2E / hợp đồng (ít).** Bộ sưu tập `.http` hoặc Postman/newman chạy các kịch bản người
  dùng thật trên môi trường staging; snapshot schema response.

### Hạ tầng & cổng chất lượng

- Mock dịch vụ Flask AI bằng [WireMock.NET](https://wiremock.org/) — không gọi OpenAI thật
  trong test.
- Seed dữ liệu chuẩn: một script tạo bộ "golden dataset" (1 khoá Toán 6, 3 chương, ~30 câu
  hỏi, 3 học sinh với lịch sử cố định, 3 gói). Mọi test integration/e2e chạy trên bộ này.
- CI gate: PR không merge nếu (a) unit + integration đỏ, (b) coverage tầng Service < 70%,
  (c) `/security-review` có mục Critical/High mới.
- Mỗi giai đoạn: viết test *trước hoặc cùng* code sửa lỗi; lỗi trong tài liệu này chuyển
  thành test case hồi quy vĩnh viễn (đặt tên theo mã `A1-01`…).

### Checklist hồi quy nhanh (chạy trước mỗi lần deploy)

| Luồng | Bước | Kỳ vọng |
|---|---|---|
| Đăng ký | `register → email → confirm → login` | Nhận token, `PackageType` đúng tier |
| Phân quyền | student B gọi tài nguyên student A | 403 ở mọi endpoint |
| Làm bài | `start → save×N → complete → result` | Điểm & % đúng, phản hồi < 2s |
| Bài ngẫu nhiên | `start-random → save → complete` | Lưu được, không timeout ảo |
| Tiến độ | `complete` bài chương X → `GET dashboard` | `NodeProgress` & % chương cập nhật |
| Thanh toán | `create sub → QR → IPN đúng` | Sub `Active`, giá == `Package.Price` |
| IPN trùng | gửi lại cùng `referenceCode` | Bỏ qua, không kích hoạt lại |
| Phụ huynh | xem dashboard con liên kết / chưa liên kết | 200 / 403 |
| AI hạn mức | gói Free xin hint quá hạn mức | 429, không gọi OpenAI |

---

## Phụ lục — Ma trận phân quyền đề xuất

Bản khởi điểm cho P0 — rà lại cùng team sản phẩm. "Chủ sở hữu" = student thao tác dữ liệu
của chính mình; phụ huynh chỉ đọc, qua `ParentLink.Status = Active`.

| Nhóm endpoint | Ẩn danh | Student | Parent | ContentEditor | Reviewer | Finance | Admin |
|---|---|---|---|---|---|---|---|
| auth: login, register, confirm, forgot-password | ✔ | – | – | – | – | – | – |
| auth: me, logout, change-password, refresh | ✗ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| user: list / get / update / delete | ✗ | self | self | self | self | self | ✔ tất cả |
| content đọc: course, node, block (IsFree) | ✔ hạn mức | ✔ | ✔ | ✔ | ✔ | ✗ | ✔ |
| content ghi: course, node, question, exercise | ✗ | ✗ | ✗ | ✔ | ✔ (duyệt) | ✗ | ✔ |
| attempt: start / save / complete | guest* | chủ sở hữu | ✗ | ✗ | ✗ | ✗ | ✗ |
| attempt: result / history | guest* | chủ sở hữu | con liên kết | ✗ | ✗ | ✗ | ✔ |
| dashboard student | ✗ | chủ sở hữu | con liên kết | ✗ | ✗ | ✗ | ✔ |
| subscription: create / cancel (của mình) | ✗ | ✔ | ✔ (trả hộ con) | ✗ | ✗ | ✔ | ✔ |
| subscription/payment: list tất cả, update-status | ✗ | ✗ | ✗ | ✗ | ✗ | ✔ | ✔ |
| package: CRUD | ✗ | ✗ | ✗ | ✗ | ✗ | ✔ | ✔ |
| sepay/ipn | ✔ (API key) | – | – | – | – | – | – |
| ai: hint / feedback / chatbot | ✗ | ✔ theo gói | ✔ | ✗ | ✗ | ✗ | ✔ |
| admin: lock user, role change, audit log, config | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✔ |

`guest*` chỉ khi thiết kế §5.14 (khách làm bài) được bật; mặc định P0 nên khoá — yêu cầu
đăng nhập.

---

*Rà soát dựa trên mã nguồn tại nhánh `main` (commit `4d1f037`) · 19 controller, tầng service
& repository, SePay IPN, dịch vụ AI Flask · Tài liệu nội bộ, không phân phối ra ngoài.*
