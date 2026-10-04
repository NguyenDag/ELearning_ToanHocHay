# Kế hoạch Tối ưu hoá và Khắc phục Lỗ hổng Hệ thống (ToanHocHay)

Dựa trên quá trình audit toàn diện về Logic, Caching, và STRIDE Threat Model, dưới đây là kế hoạch triển khai chi tiết được chia thành 4 giai đoạn, từ các lỗi nghiêm trọng/dễ sửa (Quick Wins) cho đến hệ thống Background Jobs phức tạp.

---

## Giai đoạn 1: Hotfixes & Security (P0) - Ưu tiên cao nhất
*Mục tiêu: Vá các lỗ hổng về bảo mật, giới hạn tài nguyên và sửa lỗi config AI.*

1. **Sửa lỗi cấu hình Model Gemini:**
   - **Vấn đề:** `Gemini_api.py` đang fallback về `gemini-3.6-flash` (không tồn tại).
   - **Action:** Đổi sang `gemini-2.0-flash` hoặc `gemini-1.5-flash`. Bổ sung validate biến môi trường khi start service Flask.
2. ~~**Đóng lỗ hổng Batch AI Quota:**~~ ✅ Đã kiểm tra lại (2026-09-29): phía .NET (`AIService.cs`, implement `IAIService`) chưa từng gọi `/api/hint/batch` hay `/api/feedback/batch` — không có `AIController` proxy nào tồn tại. Hai route đó là dead code, loop không giới hạn số phần tử và không check quota, nên đã bị xoá thẳng khỏi `AI/Main_AI_Service.py` (root-cause fix: bớt code hơn là vá quota cho một đường gọi không tồn tại). Mọi hint/feedback thật đều đi qua `/api/hint` và `/api/feedback` — cả hai đã được gate bởi `AiQuotaService` (`AIHintController.Create`, `AIFeedbackService.CreateAsync`) và bởi `INTERNAL_API_KEY` (A1-06) ở tầng Flask.
3. **Giới hạn Quota cho Feedback & Chat:**
   - **Vấn đề:** Hiện tại chỉ *record* `ChatCount` và `FeedbackCount` vào `AiUsageDaily`, nhưng không có hàm *block* (không giống hàm check hint).
   - **Action:** Bổ sung `TryConsumeFeedbackAsync` và `TryConsumeChatAsync` vào `AiQuotaService`. Phân cấp theo Tier (VD: Free: 5 feedback/ngày, 10 tin nhắn/ngày; Premium: Unlimited).
4. ~~**Fix lỗi lộ dữ liệu (Information Disclosure):**~~ ✅ Đã sửa (2026-09-29):
   - **Vấn đề gốc:** `GetAIInsightAsync` (`CoreDashboardService.cs`) load toàn bộ attempt history không giới hạn, và điểm mạnh/yếu bị tính all-time (không windowed) — một ngày làm 25 đề sẽ lấn át các ngày khác khi average.
   - **Đã làm:**
     - `GetStudentAttemptsAsync(studentId, take: 50)` — push `.Take(50)` xuống SQL (`ExerciseAttemptRepository.cs`) thay vì load full history rồi cắt ở client. 50 attempt này **chỉ để tìm 1 ví dụ câu sai minh hoạ** cho prompt AI, không dùng để tính điểm mạnh/yếu.
     - `GetFullPerformanceAsync(studentId, windowDays = 30)` (`DashboardRepository.cs`) — nguồn tính điểm mạnh/yếu thật — giờ windowed 30 ngày, và tính theo **trung-bình-của-trung-bình-ngày** (`PerformanceRow.Day`): mỗi ngày chỉ đóng góp 1 điểm dữ liệu dù làm 1 hay 25 đề, nên ngày học dồn không lấn át ngày khác, còn ngày-học-ngày-không thì mỗi ngày có học vẫn được tính ngang nhau. Nếu window có dưới 3 ngày hoạt động (học sinh nghỉ dài) → tự fallback về toàn bộ lịch sử thay vì báo "chưa có dữ liệu".
   - **Còn lại (chưa làm — xem mục mới bên dưới ở Giai đoạn 2 và Giai đoạn 3):** đây mới chỉ là *snapshot hiện tại* (30 ngày gần nhất), chưa trả lời được "học sinh đã tiến bộ/tụt so với trước" hay vẽ được biểu đồ lịch sử dài hạn.

---

## Giai đoạn 2: Caching & Performance (P1)
*Mục tiêu: Giảm tải Database và giảm chi phí gọi Gemini API vô ích.*

1. **Cache AI Insight & AI Roadmap:**
   - **Vấn đề:** Gọi trực tiếp sang Gemini mỗi khi load dashboard. Rất chậm và tốn tiền.
   - **Action:** Sử dụng `IMemoryCache` hoặc `IDistributedCache` với key `roadmap:{userId}` và `insight:{userId}`.
   - **TTL:** 24h hoặc tới khi người dùng hoàn thành 1 phiên làm bài mới (invalidate cache bên trong `CompleteExerciseAsync`).
2. **Cache Dashboard Data:**
   - **Vấn đề:** `GetCoreDashboardAsync` query tính toán lại mọi thứ (hoàn thành bao nhiêu, biểu đồ heatmap) mỗi lần F5.
   - **Action:** Thêm `IMemoryCache` (TTL: 5-10 phút). Invalidate cache mỗi khi `ProgressProjectionService` ghi dữ liệu mới.
3. **Tối ưu hàm lấy cấp độ Gói (Tier) của User:**
   - **Vấn đề:** `GetStudentTierAsync` và `GetPackageTierAsync` hit DB mỗi lần chạy. Trong 1 phiên xử lý có thể bị gọi nhiều lần.
   - **Action:** Dùng *Scoped Caching* hoặc `IMemoryCache` với TTL 5-15 phút để lưu Active Package của Student.
4. **[CHƯA LÀM] So sánh xu hướng theo topic ("đã tiến bộ hay tụt"):**
   - **Vấn đề:** `GetFullPerformanceAsync` (mục 4, Giai đoạn 1) chỉ cho biết điểm *hiện tại* (30 ngày gần nhất) theo từng topic, không cho biết học sinh đã thay đổi thế nào so với trước — cần cho câu AI insight kiểu "em đã tiến bộ ở chủ đề X".
   - **Quyết định (2026-09-29):** KHÔNG cần bảng mới, KHÔNG cần batch job. Vì mỗi query chỉ quét attempt của **1 học sinh** (rẻ), tính **on-demand** bằng cách gọi lại helper `GetPerformanceRowsAsync` 2 lần với 2 cutoff khác nhau (30 ngày gần nhất vs 30 ngày liền trước) rồi diff `AverageScore` theo từng topic — tái dùng đúng pattern `ComparisonDto`/`WeekComparison` đã có sẵn ở `CoreDashboardService.GetOverviewStatsAsync` (so `thisWeekStats` vs `lastWeekStats`). Cache 24h như `GetAIInsightAsync` hiện tại là đủ.
   - **Action:** Thêm `ScoreDelta`/`TrendDirection` vào `TopicPerformanceDto`, tính bằng 2 lần gọi `GetPerformanceRowsAsync(studentId, cutoff)` với cutoff lệch nhau `windowDays`.

---

## Giai đoạn 3: Background Jobs & Cron (P2)
*Mục tiêu: Đưa các Entity đang bị bỏ hoang vào luồng tính toán, dọn dẹp hệ thống.*

1. **Setup hệ thống Cron Job:**
   - **Action:** Thêm thư viện `Quartz.NET` hoặc dùng `BackgroundService` kết hợp `PeriodicTimer` của .NET.
2. **DailyProgressJob (Job xử lý Data cuối ngày):**
   - **Nhiệm vụ 1:** Tính toán và ghi nhận vào bảng `SkillProgress` (hiện tại chưa có code nào ghi vào đây). Tổng hợp điểm số/độ phân tích kỹ năng từ các câu trả lời trong ngày. Lưu ý: `SkillProgress` không có cột ngày (1 row/student/skill, bị ghi đè) — chỉ dùng làm **cache "mastery hiện tại"**, không phải bảng lịch sử.
   - **Nhiệm vụ 2:** Cập nhật bảng `LearningPath` sinh các chủ đề cần ôn tập (`WeakAreasJson`, `StrongAreasJson`). Không gọi API AI trong lúc user dùng app mà chạy job vào 2h sáng (Nightly batch).
   - **[CHƯA LÀM] Nhiệm vụ 3 — biểu đồ tiến trình dài hạn (2026-09-29):** nếu sau này cần line chart điểm theo thời gian (không chỉ "so 2 giai đoạn" như mục Giai đoạn 2.4), phải lưu snapshot điểm mỗi ngày — chưa có bảng nào làm việc này (`SkillProgress` bị ghi đè, không có trục thời gian). Tận dụng `DailyActivitySnapshot` (đã có sẵn cột `Date`, hiện chỉ lưu số lượng phút/số đề) — thêm cột điểm (VD `AverageRatio`) và ghi bởi job này. Bắt buộc chạy **nightly (2h sáng)**, không chạy theo tháng: gộp tháng mất độ phân giải (không zoom theo tuần được) và nếu job fail 1 lần thì mất nguyên 1 tháng dữ liệu thay vì chỉ mất 1 ngày.
3. **DataCleanupJob (Dọn dẹp rác hệ thống):**
   - **Vấn đề:** `StartRandomExerciseAsync` sinh rác (Empty Exercise rows khi bỏ dở).
   - **Nhiệm vụ:** Tìm các dòng Table `Exercise` + `ExerciseQuestion` sinh ra > 24h nhưng không có `ExerciseAttempt` nào, xoá chúng.
4. **SubscriptionMaintenanceJob:**
   - **Vấn đề:** Khóa học hết hạn nhưng status vẫn `Active`.
   - **Nhiệm vụ:** Quét `Subscription` có `EndDate < DateTime.UtcNow` và đổi Status sang `Expired`. (Chạy mỗi 1 tiếng).

---

## Giai đoạn 4: Kiến trúc Dài hạn & STRIDE Nâng cao (P3)
*Mục tiêu: Hoàn thiện mức độ Enterprise cho hệ thống.*

1. **Admin Audit Logs (Repudiation STRIDE):**
   - **Vấn đề:** Không có Audit Log cho Admin khi sửa thông tin nhạy cảm (Tặng gói khóa học, sửa điểm).
   - **Action:** Viết `EF Core Interceptor` hoặc Event Trigger để ghi mọi hành động Update/Delete của Admin vào bảng `AdminAuditLog` (Action, UserId, Payload Trước/Sau, Timestamp).
2. **Distributed Cache cho SecurityStamp:**
   - **Vấn đề:** Hiện dùng `IMemoryCache` lưu `sstamp:{uid}`. Nếu chạy 2 pods, cache invalidate trên một pod sẽ không có tác dụng trên pod kia. Invalidation JWT bị trễ.
   - **Action:** Chuyển `IMemoryCache` thành `Redis/IDistributedCache` tối thiểu cho phần Auth token validation.
3. **Cải tiến Token đăng ký/quên mật khẩu:**
   - **Vấn đề:** Dùng Guid thuần để verify email, có thể an toàn sinh ngẫu nhiên nhưng thiếu thông tin hạn sử dụng an toàn.
   - **Action:** Lưu thời điểm tạo token vào DB, validate Expiry = 15 phút (ForgotPassword) và 24h (VerifyEmail).

---
*Ghi chú: Lộ trình này sẽ cần bổ sung thư viện (Quartz.NET, MemoryCache) và thay đổi cấu trúc database đối với phần Admin Log.*
