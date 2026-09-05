# Danh mục System Test — ToanHocHay (bản gốc)

> **Đây là tài liệu gốc về system test (kiểm thử hệ thống / end-to-end) của ToanHocHay.**
> Mọi kịch bản được liệt kê **đầy đủ** tại đây, theo từng nhóm rõ ràng.
>
> Unit: [DANH-MUC-UNIT-TEST.md](DANH-MUC-UNIT-TEST.md) · Integration: [DANH-MUC-INTEGRATION-TEST.md](DANH-MUC-INTEGRATION-TEST.md) · Luồng: [KE-HOACH-KIEM-THU-HE-THONG.md](KE-HOACH-KIEM-THU-HE-THONG.md).
>
> **P** (ưu tiên): P1 chặn release · P2 nên có trước go-live · P3 cải thiện dần.
> **Cách chạy**: 🤖 tự động · 🖐️ thủ công · 🧪 bán tự động (script + kiểm mắt).

---

## Mục lục

- [1. Định nghĩa & khác biệt với Integration test](#1-định-nghĩa--khác-biệt-với-integration-test)
- [2. Môi trường & thành phần](#2-môi-trường--thành-phần)
- [3. Công cụ & bộ dữ liệu](#3-công-cụ--bộ-dữ-liệu)
- [4. Danh mục kịch bản](#4-danh-mục-kịch-bản)
  - [4.1 Hành trình đầu–cuối (ST-E2E)](#41-hành-trình-đầucuối-st-e2e)
  - [4.2 Hiệu năng & tải (ST-PERF)](#42-hiệu-năng--tải-st-perf)
  - [4.3 Bảo mật (ST-SEC)](#43-bảo-mật-st-sec)
  - [4.4 Khả năng chịu lỗi (ST-RESIL)](#44-khả-năng-chịu-lỗi-st-resil)
  - [4.5 Toàn vẹn dữ liệu & đối soát (ST-DATA)](#45-toàn-vẹn-dữ-liệu--đối-soát-st-data)
  - [4.6 Vận hành & quan trắc (ST-OPS)](#46-vận-hành--quan-trắc-st-ops)
  - [4.7 Tương thích & bản địa hoá (ST-COMPAT)](#47-tương-thích--bản-địa-hoá-st-compat)
  - [4.8 Nghiệm thu theo vai trò (ST-UAT)](#48-nghiệm-thu-theo-vai-trò-st-uat)
- [5. Tiêu chí Pass/Fail & Definition of Done cho release](#5-tiêu-chí-passfail--definition-of-done-cho-release)
- [6. Lịch chạy](#6-lịch-chạy)
- [7. Trạng thái hiện tại](#7-trạng-thái-hiện-tại)

---

## 1. Định nghĩa & khác biệt với Integration test

| | Integration test | **System test** |
|---|---|---|
| Chạy trên | API + Postgres (Testcontainers), 1 process | **Toàn hệ đã triển khai**: WebApp + Backend + Postgres + Flask AI + SePay sandbox + email, nhiều process/nhiều máy |
| Điểm vào | HTTP tới controller | **Trình duyệt** (WebApp UI) hoặc client API thật, như người dùng cuối |
| AI / SePay / Email | test-double | **thật** (sandbox / staging) |
| Mục tiêu | logic tích hợp, phân quyền, transaction | **hành trình người dùng xuyên nhiều màn hình / nhiều ngày**, + thuộc tính phi chức năng (hiệu năng, bảo mật, chịu lỗi, đối soát, vận hành) |
| Thời gian | giây | phút → giờ |
| Tần suất | mỗi commit/PR | mỗi release candidate / nightly / trước go-live |

System test **không** thay integration test — nó khẳng định hệ thống *lắp ráp lại* hoạt động
đúng ở môi trường giống production, và các thuộc tính chỉ lộ ra khi chạy thật.

> System test **độc lập** với dự án test `.NET` (Unit/Integration đang xây mới từ đầu — xem
> [DANH-MUC-INTEGRATION-TEST.md §2](DANH-MUC-INTEGRATION-TEST.md)). Nó chạy qua trình duyệt /
> HTTP client trỏ vào `staging` đã deploy, không dùng `WebApplicationFactory` hay Testcontainers.
> Bộ system test (Playwright / k6 / newman) cũng **chưa tồn tại** — dựng mới theo §3.

---

## 2. Môi trường & thành phần

**Môi trường `staging`** — cấu hình giống production, dữ liệu giả:

| Thành phần | Ghi chú staging |
|---|---|
| Backend `ELearning_ToanHocHay_Control` | net8, sau reverse proxy; `RateLimiting:TrustedProxies` = IP WebApp; `AppSettings:BaseUrl` = origin WebApp (để link email chạy) |
| WebApp `ELearning_ToanHocHay_Web` | net9 MVC, cookie-auth, `Api:BaseUrl` trỏ backend staging |
| PostgreSQL | instance riêng, seed bằng `DemoDataSeeder` (`Seed:DemoData=true`) hoặc bộ dữ liệu UAT |
| Flask AI | service AI thật (staging) — có thể tắt để test `ST-RESIL` |
| SePay | **sandbox**: tài khoản ảo test, gửi IPN thủ công/simulator được |
| Email | SendGrid sandbox hoặc hộp thư bắt (MailHog/Mailtrap) để đọc link xác nhận/reset |
| Serilog sink | file + (staging) log aggregator để kiểm `ST-OPS` |
| DataProtection keys | bảng `DataProtectionKeys` — kiểm số TK hoàn tiền mã hoá được |

**Tài khoản chuẩn** (từ `DemoDataSeeder`, mật khẩu `123456`): `admin@thh.local`, `editor@thh.local`,
`reviewer@thh.local`, `hs01..hs20@thh.local` (5 Standard / 5 Premium / 10 Free), `ph01..ph10@thh.local`.
Cần thêm tay: 1 tài khoản `FinanceManager`, 1 `SupportStaff`.

---

## 3. Công cụ & bộ dữ liệu

| Nhóm | Công cụ đề xuất |
|---|---|
| E2E qua UI | **Playwright** (.NET hoặc TS) — `ELearning_ToanHocHay_Web`; chạy headless trên CI, headed khi debug |
| E2E qua API | **Postman/newman** hoặc xUnit + `HttpClient` trỏ staging (không `WebApplicationFactory`) |
| Tải | **k6** (kịch bản JS) hoặc **NBomber** (.NET) |
| Bảo mật | OWASP ZAP (baseline scan), kiểm tay theo checklist `ST-SEC`, `dotnet` `/security-review` |
| Email | MailHog / Mailtrap API để đọc link |
| SePay IPN | script `curl` gửi payload IPN có `Authorization: Apikey` |
| Đối soát dữ liệu | truy vấn SQL + endpoint `/reconciliation` |

**Bộ dữ liệu UAT**: nhân bản `DemoDataSeeder` + thêm: 3 khoá đủ trạng thái (Draft/Published/Archived),
lịch sử attempt 90 ngày, 5 payment Completed (đủ tuổi khác nhau cho test hoàn tiền), 2 subscription
sắp hết hạn (test sweep), 1 hội thoại chatbot mở.

---

## 4. Danh mục kịch bản

### 4.1 Hành trình đầu–cuối (ST-E2E)

> Mỗi kịch bản đi hết một vòng đời, chạm nhiều màn hình / nhiều vai trò. Ghi rõ **các bước**
> và **điểm kiểm** (checkpoint).

| Mã | Kịch bản | Các bước chính | Điểm kiểm | Chạy | P |
|---|---|---|---|---|---|
| **ST-E2E-01** | Học sinh mới: đăng ký → học thử → mua gói → học có phí | (1) Đăng ký trên WebApp · (2) nhận email, bấm link xác nhận · (3) đăng nhập · (4) xem khoá, mở bài chương 1 (free) OK, bài chương 2 bị khoá · (5) bấm "Nâng cấp", chọn gói, hiện QR · (6) gửi IPN sandbox đúng tiền · (7) refresh — huy hiệu Premium xuất hiện, bài chương 2 mở được · (8) làm 1 bài tập, xem kết quả | email tới trong ≤ 60s; sau IPN ≤ 5s huy hiệu đổi; `PackageTier` claim cập nhật **không cần** đăng nhập lại; điểm bài tập đúng | 🧪 | P1 |
| **ST-E2E-02** | Làm bài kiểm tra có phản hồi AI | (1) Học sinh Premium vào bài Exam · (2) làm, có chuyển tab 3 lần · (3) nộp · (4) thấy điểm ngay, phần "giải chi tiết" đang tải · (5) chờ, phản hồi AI xuất hiện · (6) phụ huynh liên kết nhận thông báo điểm (nếu thấp) + cảnh báo chuyển tab | `complete` trả < 3s; feedback AI hoàn tất < 60s; thông báo tới phụ huynh; `TabSwitchLog` ghi nhận | 🧪 | P1 |
| **ST-E2E-03** | Phụ huynh theo dõi con | (1) Phụ huynh đăng ký/đăng nhập · (2) mời con qua email **hoặc** con nhập connection code · (3) phụ huynh mở dashboard con: tiến độ, điểm, heatmap · (4) phụ huynh thu hồi liên kết · (5) mở lại dashboard con → bị chặn | dashboard con hiển thị đúng số liệu; sau thu hồi → `403`/trang lỗi thân thiện | 🧪 | P1 |
| **ST-E2E-04** | Hoàn tiền toàn phần huỷ gói | (1) Học sinh đã mua gói (subscription Active) gửi yêu cầu hoàn tiền kèm số TK · (2) Finance duyệt · (3) Finance tạo lô, export CSV · (4) mở CSV bằng Excel — số TK, số tiền, nội dung đúng, không bị Excel diễn giải công thức · (5) Finance "đã chi", "xác nhận cả lô" · (6) học sinh: `Payment=Refunded`, gói về `Cancelled`, mất Premium | CSV mở đúng ở Excel thật; timeline `RefundEvent` đủ; `AuditLog` ghi; đối soát `Balanced` | 🧪 | P1 |
| **ST-E2E-05** | Hoàn tiền một phần | như ST-E2E-04 nhưng `amount < payment` | `Payment=PartiallyRefunded`; gói **giữ** Active; học sinh gửi được yêu cầu tiếp cho phần còn lại | 🧪 | P2 |
| **ST-E2E-06** | Biên tập nội dung → xuất bản | (1) Editor tạo khoá, chương, bài, block 12 loại, câu hỏi · (2) submit version · (3) Reviewer duyệt (hoặc trả về kèm comment) · (4) Admin/Reviewer publish · (5) học sinh thấy khoá mới trong danh mục, học được · (6) Editor sửa bài sau publish → bị chặn, phải tạo version mới | version workflow đúng; nội dung published hiển thị đúng trên WebApp; khoá sửa sau publish | 🖐️ | P1 |
| **ST-E2E-07** | Gói hết hạn (sweep) | (1) Seed subscription Active `EndDate` = hôm qua · (2) chờ job sweep (hoặc gọi `run-lifecycle`) · (3) học sinh đăng nhập | dashboard mất Premium; bài có phí khoá lại; `Subscription=Expired` | 🤖 | P1 |
| **ST-E2E-08** | Pending bỏ dở | (1) Tạo yêu cầu thanh toán, **không** chuyển tiền · (2) chờ quá `PendingTimeoutMinutes` (hoặc `run-lifecycle`) | `Subscription=Cancelled`, `Payment=Failed`; WebApp hiển thị "đã huỷ", cho tạo lại | 🤖 | P2 |
| **ST-E2E-09** | Quên mật khẩu | (1) Bấm "Quên mật khẩu", nhập email · (2) nhận email, mở link `/reset-password?token=` · (3) đặt mật khẩu mới · (4) mọi phiên cũ bị đăng xuất · (5) đăng nhập bằng mật khẩu mới | link 1 lần dùng; token hết hạn 1h; phiên cũ chết | 🧪 | P1 |
| **ST-E2E-10** | Chatbot → gặp nhân viên | (1) Học sinh hỏi chatbot, có trả lời AI · (2) bấm "Gặp nhân viên" · (3) SupportStaff thấy trong hàng đợi, nhận, trả lời · (4) học sinh thấy tin nhắn nhân viên · (5) đóng hội thoại | hội thoại + tin nhắn lưu đủ; chuyển vai đúng | 🖐️ | P2 |
| **ST-E2E-11** | Học liên tục nhiều ngày (streak) | mô phỏng hoạt động 5 ngày liên tiếp rồi nghỉ 1 ngày | heatmap đúng; streak = 5 rồi reset; thông báo "không hoạt động 3 ngày" sau đó | 🤖 | P2 |
| **ST-E2E-12** | Mua gói mới khi đang có gói | học sinh đang Standard mua Premium | Premium Active ngay sau IPN; Standard tự `Expired`; chỉ 1 gói Active | 🧪 | P1 |
| **ST-E2E-13** | Đăng nhập đa thiết bị + đổi mật khẩu | đăng nhập trên 2 trình duyệt → đổi mật khẩu ở 1 nơi | trình duyệt còn lại bị đăng xuất ở request tiếp theo (SecurityStamp) | 🖐️ | P2 |
| **ST-E2E-14** | Guest (khách chưa đăng nhập) | duyệt danh mục, đọc bài free, bị chặn ở bài có phí, được mời đăng nhập | không lỗi; CTA đăng nhập/đăng ký rõ ràng | 🖐️ | P2 |

### 4.2 Hiệu năng & tải (ST-PERF)

> Ngưỡng là mục tiêu khởi điểm cho staging (1 instance) — chốt lại theo hạ tầng thật.

| Mã | Kịch bản | Tải | Ngưỡng chấp nhận | Chạy | P |
|---|---|---|---|---|---|
| **ST-PERF-01** | Đăng nhập | 50 VU trong 2 phút | p95 < 800ms; lỗi < 1% | 🤖 k6 | P1 |
| **ST-PERF-02** | Xem cây nội dung khoá `/api/learn/courses/{id}/content` | 100 VU | p95 < 1s; không N+1 (kiểm query count) | 🤖 | P1 |
| **ST-PERF-03** | Dashboard học sinh `overview` | 50 VU | p95 < 1.5s | 🤖 | P1 |
| **ST-PERF-04** | Nộp bài `complete` (chấm tự động + đẩy job) | 30 VU đồng thời | p95 < 3s; job feedback không chặn response | 🤖 | P1 |
| **ST-PERF-05** | Burst IPN — 200 webhook trong 10s (gồm trùng ref) | | tất cả `200`; DB không có subscription kích hoạt 2 lần; không mất giao dịch hợp lệ | 🤖 | P1 |
| **ST-PERF-06** | `start` bài tập đồng thời từ 1 học sinh (spam nút) | 20 request/s | không `5xx`; không tạo attempt rác | 🤖 | P2 |
| **ST-PERF-07** | Export CSV lô hoàn tiền 500 request | | phản hồi < 5s; file đúng định dạng | 🤖 | P2 |
| **ST-PERF-08** | Danh sách phân trang (users/payments/subscriptions) với 100k bản ghi | | p95 < 1s nhờ index; không tải hết bảng | 🧪 | P2 |
| **ST-PERF-09** | Soak test 2h ở tải trung bình | 20 VU liên tục | không rò bộ nhớ (RSS ổn định); không tăng dần thời gian phản hồi; connection pool không cạn | 🤖 | P2 |
| **ST-PERF-10** | Rate limiter dưới tải | spam `auth` từ nhiều `X-Client-Key` | mỗi client bị giới hạn độc lập; client khác không ảnh hưởng; `429` trả nhanh (không tốn tài nguyên) | 🤖 | P2 |

### 4.3 Bảo mật (ST-SEC)

| Mã | Kịch bản | Kỳ vọng | Chạy | P |
|---|---|---|---|---|
| **ST-SEC-01** | IDOR: học sinh B đổi id trong URL để xem `result` / `history` / `dashboard` / `refund` của A | `403` mọi trường hợp (kiểm cả qua WebApp và gọi API trực tiếp) | 🖐️ | P1 |
| **ST-SEC-02** | Nâng quyền: sửa JWT (đổi `role` claim, đổi `UserType`) | chữ ký không khớp → `401`; không có đường "trust client claim" | 🖐️ | P1 |
| **ST-SEC-03** | Token hết hạn / bị thu hồi vẫn dùng | `401`; đổi mật khẩu / khoá user → access token đang bay chết (SecurityStamp) | 🧪 | P1 |
| **ST-SEC-04** | Refresh token reuse | dùng lại token đã xoay → toàn bộ phiên của user bị cắt | 🧪 | P1 |
| **ST-SEC-05** | Rate limit: brute-force mật khẩu | khoá tài khoản leo thang + rate-limit cổng; phân vùng theo người dùng cuối (không khoá nhầm cả WebApp) | 🧪 | P1 |
| **ST-SEC-06** | CSV formula-injection: tên chủ TK `=HYPERLINK(...)`, `+`, `-`, `@` | mở file bằng **Excel thật + Google Sheets** → hiển thị dạng text, không thực thi | 🖐️ | P1 |
| **ST-SEC-07** | PII: dump bảng `RefundRequest` | cột số TK là ciphertext (Data Protection); API chỉ trả 4 số cuối | 🖐️ | P1 |
| **ST-SEC-08** | IPN không có / sai `Apikey` | `401`; không xử lý payload | 🤖 | P1 |
| **ST-SEC-09** | CORS: gọi API từ origin lạ | preflight bị chặn; chỉ origin WebApp trong allowlist qua | 🧪 | P1 |
| **ST-SEC-10** | Rò thông tin lỗi: ép lỗi 500 | body không có stack trace / `ex.Message` / chuỗi kết nối; chỉ envelope + correlation id | 🖐️ | P1 |
| **ST-SEC-11** | Liệt kê tài khoản: `forgot-password` / `resend-confirmation` / `login` với email tồn tại vs không | phản hồi **giống nhau** (message + thời gian tương đương) | 🧪 | P2 |
| **ST-SEC-12** | Security headers (WebApp + API) | có `X-Content-Type-Options`, `X-Frame-Options`/CSP, HSTS (khi HTTPS); cookie `HttpOnly` + `Secure` + `SameSite` | 🖐️ | P2 |
| **ST-SEC-13** | Injection: tham số `?search=`, `?status=`, nội dung câu trả lời chứa `' OR 1=1`, `<script>` | không SQL-i (EF tham số hoá); không stored XSS trên WebApp (mã hoá output) | 🖐️ | P1 |
| **ST-SEC-14** | Upload / nội dung lớn: body khổng lồ, JSON lồng sâu | bị giới hạn kích thước, không DoS | 🧪 | P2 |
| **ST-SEC-15** | ZAP baseline scan (WebApp + API Swagger) | không phát hiện lỗ hổng High; Medium có phương án | 🤖 | P2 |
| **ST-SEC-16** | Truy cập Swagger / endpoint nội bộ (`run-lifecycle`, `run-inactivity-check`, test-hook) trên staging/prod | Swagger tắt ở prod; endpoint nội bộ cần role Finance/Admin | 🖐️ | P2 |
| **ST-SEC-17** | Secrets: JWT `SecretKey`, SePay key, DB creds | không nằm trong repo / không log ra; đọc từ biến môi trường / secret store | 🖐️ | P1 |

### 4.4 Khả năng chịu lỗi (ST-RESIL)

| Mã | Kịch bản | Kỳ vọng | Chạy | P |
|---|---|---|---|---|
| **ST-RESIL-01** | Flask AI **tắt** | `/api/chatbot/health` → `503`; chatbot vẫn nhận & lưu tin, trả fallback; gợi ý/feedback báo lỗi thân thiện, không `500` trần trụi; dashboard AI-assessment degrade an toàn | 🧪 | P1 |
| **ST-RESIL-02** | Flask AI **chậm** (timeout) | request không treo vô hạn; có timeout + thông báo; feedback job retry hoặc đánh dấu thất bại | 🧪 | P2 |
| **ST-RESIL-03** | SePay webhook **đến muộn** (sau khi Pending timeout) | subscription đã `Cancelled` → IPN trả `Ignored` "no longer payable"; tiền thật cần hoàn tay (quy trình rõ) | 🖐️ | P1 |
| **ST-RESIL-04** | SePay webhook **mất** (không đến) | job sweep huỷ Pending sau timeout; đối soát phát hiện "Active without completed payment" nếu có drift | 🤖 | P1 |
| **ST-RESIL-05** | Server **sập giữa lúc xử lý IPN** (kill process sau khi nhận, trước commit) | SePay retry → khi server hồi phục, kích hoạt **đúng 1 lần**; `SePayIpnLog` không có dòng nửa vời | 🖐️ | P1 |
| **ST-RESIL-06** | Server **restart** khi có job nền đang chạy (feedback queue, sweep) | không mất dữ liệu; job chạy lại khi khởi động; không double-effect | 🖐️ | P2 |
| **ST-RESIL-07** | DB **mất kết nối tạm** rồi phục hồi | `/health/ready` → unhealthy trong lúc mất; request lỗi có kiểm soát; tự reconnect; không cần restart app | 🧪 | P2 |
| **ST-RESIL-08** | Email provider **lỗi** | đăng ký/hoàn tiền vẫn thành công (email vào hàng đợi nền); không chặn luồng chính; retry gửi | 🧪 | P2 |
| **ST-RESIL-09** | Hai IPN cùng ref **song song** (gửi đồng thời) | unique index serialize; đúng 1 `Processed`, cái kia `Duplicate`; không kích hoạt 2 lần | 🤖 | P1 |
| **ST-RESIL-10** | Chuyển khoản 2 lần (2 ref khác) vào 1 subscription | rủi ro đã biết (R1 trong [Luong-thanh-toan.md](Luong-thanh-toan.md)): tuần tự thì lần 2 `Duplicate`; song song có thể cần hoàn tay — kiểm & ghi nhận | 🖐️ | P2 |
| **ST-RESIL-11** | Đồng thời: 2 tab cùng bấm "nộp bài" | chấm đúng 1 lần (row lock); không điểm sai | 🧪 | P1 |
| **ST-RESIL-12** | Đồng thời: học sinh mua gói + admin đổi giá package | giao dịch đang mở dùng giá tại thời điểm tạo; không lệch | 🖐️ | P3 |

### 4.5 Toàn vẹn dữ liệu & đối soát (ST-DATA)

| Mã | Kịch bản | Kỳ vọng | Chạy | P |
|---|---|---|---|---|
| **ST-DATA-01** | Đối soát thanh toán `GET /api/finance/subscriptions/reconciliation` sau 1 ngày hoạt động | `Balanced = true`; `ActiveWithoutCompletedPayment = 0`; số Active subscription = số Completed payment tương ứng | 🤖 | P1 |
| **ST-DATA-02** | Đối soát hoàn tiền `GET /api/finance/refunds/reconciliation` | `CompletedRefundTotal == PaymentRefundedTotal`; không `StaleDisbursed` quá hạn không cảnh báo | 🤖 | P1 |
| **ST-DATA-03** | Trần hoàn tiền/ngày | tổng `RefundRequest` đã duyệt trong ngày (giờ VN) ≤ `refund.dailyCapVnd`; `daily-usage` khớp truy vấn SQL | 🧪 | P1 |
| **ST-DATA-04** | Roll-up tiến độ | `NodeProgress` cấp chương/version = trung bình đúng của các node con (`MaterializedPath`); không lệch sau nhiều lần nộp | 🧪 | P1 |
| **ST-DATA-05** | Không có bản ghi mồ côi | không `Subscription` không `Payment`; không `ExerciseAttempt` không `Exercise`; không `RefundRequest` không `Payment` (kiểm bằng SQL) | 🤖 | P1 |
| **ST-DATA-06** | `RefundAmount` tích luỹ | Σ `RefundRequest.Amount` (Completed) cho 1 payment = `Payment.RefundAmount`; `Payment.Status` = Refunded khi ≥ Amount, else PartiallyRefunded | 🧪 | P1 |
| **ST-DATA-07** | Idempotency log | mỗi `referenceCode` đúng 1 dòng `SePayIpnLog`; `RawPayload` lưu nguyên văn | 🤖 | P1 |
| **ST-DATA-08** | Migration up sạch | dựng DB trống → chạy toàn bộ migration → schema khớp model (không pending model changes) | 🤖 | P1 |
| **ST-DATA-09** | Migration trên bản sao dữ liệu thật | phục hồi dump staging → chạy migration mới → không mất dữ liệu, không khoá bảng quá lâu | 🖐️ | P2 |
| **ST-DATA-10** | Timezone | mốc "ngày" của trần hoàn tiền theo `refund.timezoneOffsetHours` (7); `EndDate` subscription tính đúng qua đổi ngày; heatmap theo ngày VN | 🧪 | P2 |
| **ST-DATA-11** | Đơn vị tiền | mọi số tiền là VND nguyên (không phần lẻ); QR `amount` khớp `Package.Price`; CSV `SoTien` là số nguyên | 🖐️ | P1 |
| **ST-DATA-12** | Audit log đầy đủ | mọi thay đổi role user / status refund / status refund-batch có dòng `AuditLog` (kể cả sửa DB trực tiếp — interceptor) | 🧪 | P1 |
| **ST-DATA-13** | Backup & restore | backup định kỳ chạy; thử restore ra instance mới → app khởi động, dữ liệu đủ, DataProtection key ring còn (giải mã số TK OK) | 🖐️ | P1 |

### 4.6 Vận hành & quan trắc (ST-OPS)

| Mã | Kịch bản | Kỳ vọng | Chạy | P |
|---|---|---|---|---|
| **ST-OPS-01** | Health cho load balancer | `/health` = live (không phụ thuộc DB); `/health/ready` = DB OK; LB rút node khi `ready` fail | 🧪 | P1 |
| **ST-OPS-02** | Correlation id xuyên hệ | 1 request từ WebApp → header `X-Correlation-Id` → log backend cùng id → nếu gọi Flask AI thì truyền tiếp; tra 1 id ra toàn bộ chuỗi | 🖐️ | P1 |
| **ST-OPS-03** | Structured log | Serilog log JSON có `CorrelationId`, level đúng; lỗi có đủ ngữ cảnh; **không** log mật khẩu / token / số TK | 🖐️ | P1 |
| **ST-OPS-04** | Log refund | mỗi bước hoàn tiền có dòng `Refund event {EventType} req={id} {From}->{To} amount=... actor=... corr=...` | 🧪 | P2 |
| **ST-OPS-05** | Job nền quan sát được | sweep vòng đời + feedback queue + refund stale-sweep có log mỗi lần chạy (số bản ghi xử lý) | 🖐️ | P2 |
| **ST-OPS-06** | Cấu hình động | đổi `refund.dailyCapVnd`, ngưỡng thông báo qua `PUT /api/admin/config/{key}` → có hiệu lực (sau TTL cache 5') không cần deploy | 🧪 | P2 |
| **ST-OPS-07** | Khởi động nguội | app boot: chạy migrate, (dev) seed, mở cổng trong thời gian hợp lý; fail rõ ràng nếu thiếu config bắt buộc (JWT secret…) | 🖐️ | P2 |
| **ST-OPS-08** | Deploy không gián đoạn (nếu có) | rolling deploy: request đang bay không rớt; migration tương thích ngược | 🖐️ | P3 |
| **ST-OPS-09** | Dung lượng log / xoay vòng | file log xoay vòng, không đầy đĩa | 🖐️ | P3 |

### 4.7 Tương thích & bản địa hoá (ST-COMPAT)

| Mã | Kịch bản | Kỳ vọng | Chạy | P |
|---|---|---|---|---|
| **ST-COMPAT-01** | Trình duyệt | WebApp chạy đúng trên Chrome, Edge, Firefox, Safari (bản mới) | 🖐️ | P2 |
| **ST-COMPAT-02** | Responsive / mobile | các trang chính (danh mục, học bài, làm bài, dashboard, thanh toán) dùng được trên màn hình hẹp | 🖐️ | P2 |
| **ST-COMPAT-03** | Tiếng Việt | dấu hiển thị đúng mọi nơi; email tiếng Việt không vỡ mã; nội dung CK/CSV bỏ dấu đúng (`đ→d`) | 🖐️ | P1 |
| **ST-COMPAT-04** | Thông báo lỗi tiếng Việt | mọi lỗi API hiển thị trên WebApp là tiếng Việt (không lọt message tiếng Anh / JSON thô); `429` có toast tiếng Việt | 🖐️ | P1 |
| **ST-COMPAT-05** | Định dạng số / ngày | tiền hiển thị `199.000 ₫`; ngày định dạng VN; giờ theo múi VN | 🖐️ | P2 |
| **ST-COMPAT-06** | Toast/notification UI | hệ toast thống nhất (`THHToast`); không còn `alert()` / banner cũ | 🖐️ | P2 |
| **ST-COMPAT-07** | In / xuất | CSV lô hoàn tiền mở đúng ở Excel VN (UTF-8, CRLF); kết quả bài in được | 🖐️ | P3 |
| **ST-COMPAT-08** | Chịu tải mạng yếu | thao tác chính có trạng thái loading; không double-submit; timeout thân thiện | 🖐️ | P3 |

### 4.8 Nghiệm thu theo vai trò (ST-UAT)

> Checklist người thật đi theo vai trò, xác nhận "dùng được".

| Mã | Vai trò | Checklist rút gọn | P |
|---|---|---|---|
| **ST-UAT-01** | Học sinh | đăng ký/đăng nhập · xem & học bài free · mua gói · học bài có phí · làm quiz/test/exam · xem kết quả + giải chi tiết · dùng gợi ý AI · xem dashboard cá nhân · hỏi chatbot · nhận thông báo · quên/đổi mật khẩu · gửi yêu cầu hoàn tiền | P1 |
| **ST-UAT-02** | Phụ huynh | đăng ký · liên kết con (mời email / connection code) · xem dashboard + lịch sử làm bài của con · nhận thông báo về con · thu hồi liên kết | P1 |
| **ST-UAT-03** | Content Editor | tạo/sửa khoá–chương–bài–block–câu hỏi · tạo version · submit · xử lý comment review · không publish được (đúng thiết kế) · không sửa được version đã publish | P1 |
| **ST-UAT-04** | Academic Reviewer | xem hàng chờ · duyệt/ý kiến version · duyệt câu hỏi · publish version | P1 |
| **ST-UAT-05** | Finance Manager | xem danh sách payment/subscription (phân trang, lọc) · đối soát thanh toán · tạo/sửa package · duyệt/từ chối hoàn tiền · dual-control · tạo lô · export CSV · đánh dấu đã chi · xác nhận · đối soát hoàn tiền · daily-usage | P1 |
| **ST-UAT-06** | Support Staff | xem hàng đợi chatbot · nhận hội thoại · trả lời · đóng | P2 |
| **ST-UAT-07** | System Admin | khoá/mở khoá user · đổi role · xem audit log · sửa system config · chạy inactivity check · chạy lifecycle sweep · toàn quyền nội dung & tài chính | P1 |
| **ST-UAT-08** | Khách (chưa đăng nhập) | xem trang chủ, danh mục, bài free · bị mời đăng nhập đúng chỗ · không thấy dữ liệu riêng tư | P2 |

---

## 5. Tiêu chí Pass/Fail & Definition of Done cho release

Một release candidate được **duyệt lên production** khi:

1. **100% ST-*** ở mức **P1** pass (E2E, PERF ngưỡng, SEC, RESIL, DATA, OPS, UAT).
2. Không có lỗi **Blocker/Critical** mở.
3. `dotnet test` (unit + integration) xanh toàn bộ trên CI có Docker.
4. `ST-DATA-01`, `ST-DATA-02`, `ST-DATA-05`, `ST-DATA-08` (đối soát + migration) pass **trên bản sao dữ liệu staging**.
5. `ST-SEC-01..10`, `ST-SEC-17` pass; ZAP không High.
6. Runbook cho `ST-RESIL-03/04/05/10` (các trường hợp cần thao tác tay) đã viết và Finance/Ops nắm.
7. `ST-UAT` của các vai trò liên quan tính năng trong release được ký nhận.

**Fail nhanh (dừng test, trả về dev):** bất kỳ P1 nào của E2E-01/02/03/04, PERF-05, RESIL-05/09/11, DATA-06/07/12.

---

## 6. Lịch chạy

| Khi nào | Chạy gì |
|---|---|
| **Mỗi PR** | unit + integration (không thuộc file này) |
| **Nightly trên staging** | ST-E2E-01/02/03/04/07/12 (🤖/🧪) + ST-PERF-01..05 + ST-DATA-01/02/05/07 + ST-SEC-08 + ST-RESIL-09 |
| **Mỗi release candidate** | toàn bộ P1 của mục 4 + regression P2 liên quan |
| **Trước go-live lớn** | thêm toàn bộ P2, ZAP scan, soak test (ST-PERF-09), ST-DATA-09/13 (migration + restore trên dữ liệu thật), full ST-UAT |
| **Sau sự cố production** | thêm 1 ST-RESIL / ST-DATA tái hiện sự cố đó (chống hồi quy) |

---

## 7. Trạng thái hiện tại

- **Chưa có** bộ system test tự động (Playwright / k6 / newman). Việc kiểm hiện làm rời rạc,
  thủ công khi phát triển từng pha (ghi trong [KE-HOACH...](Ra-soat-API-va-ke-hoach-kiem-soat.md)
  các mục "Verified" và note kiểm thủ công của các nhánh).
- Đã kiểm tay (chưa thành case chính thức): IPN happy/duplicate/mismatch trên local; rate limiter
  phân vùng người dùng cuối; lockout + rate-limit ra toast tiếng Việt; `DemoDataSeeder` verify
  2026-09-04 (login, catalog, gating, dashboard, heatmap, tier).
- **Việc cần làm:**
  1. Dựng môi trường `staging` đủ 6 thành phần (mục 2).
  2. Bộ dữ liệu UAT (mục 3).
  3. Khung Playwright + k6 + newman; CI nightly.
  4. Triển khai ST-E2E-01..04 trước (hành trình cốt lõi), rồi PERF/SEC/RESIL/DATA P1.
  5. Viết runbook cho các trường hợp thao tác tay (hoàn tiền lệch, IPN muộn).
