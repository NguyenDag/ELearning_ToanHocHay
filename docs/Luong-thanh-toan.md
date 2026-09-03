# Luồng thanh toán — SePay VA + QR + IPN

> **Bản nội bộ · không phân phối ra ngoài**
> Mô tả cơ chế thu tiền cho gói/subscription của ToanHocHay. Vòng đời hoàn tiền: [Luồng hoàn tiền](Luong-hoan-tien.md).

| | |
|---|---|
| **Cổng** | SePay — trung gian chuyển khoản ngân hàng VN (tài khoản ảo + QR + webhook IPN) |
| **Stack** | .NET 8 · EF Core · PostgreSQL |
| **Không xử lý** | thẻ tín dụng, ví điện tử — chỉ chuyển khoản ngân hàng |

---

## 1. Bức tranh tổng thể

Hệ thống **không giữ tiền**. SePay cấp một **tài khoản ảo (VA)**; khách chuyển khoản vào VA đó với nội dung có mã subscription; SEPay phát hiện giao dịch và gọi **webhook IPN** về backend để kích hoạt.

```
Client                         Backend                        SePay / Ngân hàng
  │  POST /api/subscriptions       │                                │
  │──────────────────────────────► │  Payment(Pending)              │
  │                                │  + Subscription(Pending)       │
  │ ◄─── subscriptionId, amount, qrUrl                              │
  │                                │                                │
  │  quét QR, chuyển khoản ──────────────────────────────────────►  │  tiền vào VA
  │  nội dung: "TKPTTS SUBSCRIPTION_{id}"                           │
  │                                │  ◄──── POST /api/sepay/ipn ─────│  (webhook, có API key)
  │                                │  xác thực + kích hoạt          │
  │                                │  Subscription → Active         │
  │                                │  ───── 200 OK ───────────────► │
```

**Bản chất bất đồng bộ**: việc "tạo yêu cầu" và "tiền về" tách rời về thời gian; cầu nối duy nhất là webhook IPN — có thể đến muộn, đến nhiều lần, hoặc mất.

---

## 2. Các pha

### Pha A — Tạo yêu cầu thanh toán

**Endpoint** `POST /api/subscriptions` ([SubscriptionController.cs](../Controllers/SubscriptionController.cs)) · `[Authorize]` + `IResourceAccessService.CanAccessStudentAsync`
**Service** [SubscriptionPaymentService.CreatePendingAsync](../Services/Implementations/SubscriptionPaymentService.cs)

1. Mở transaction (`IUnitOfWork.BeginTransactionAsync`).
2. Lấy `Package`; **số tiền lấy từ `package.Price` phía server** — không nhận từ client (vá lỗi A2‑02).
3. Tạo `Payment { Status = Pending, PaidByUserId = <token>, Amount = package.Price, PaymentMethod = BankTransfer }` → `SaveChanges`.
4. Tạo `Subscription { Status = Pending, Payment = payment, PackageId, StudentId, AmountPaid = amount }` → `SaveChanges`.
5. `CommitAsync`. Lỗi bất kỳ → `RollbackAsync` + rethrow.
6. Controller gọi [`SePayService.GenerateQrUrl(subscriptionId, amount)`](../Services/Implementations/SePayService.cs) → URL ảnh QR mang `amount` + nội dung `TKPTTS SUBSCRIPTION_{id}`.

Trả về `{ subscriptionId, amount, qrUrl }`.

### Pha B — IPN kích hoạt (trái tim của cơ chế)

**Endpoint** `POST /api/sepay/ipn` ([SepayController.cs](../Controllers/SepayController.cs)) · `[AllowAnonymous]` + `[SePayApiKey]` (header `Authorization: Apikey <key>`, so với `SePay:ApiKeyValidator`)
**Service** [SePayIpnService.ProcessAsync → EvaluateAsync](../Services/Implementations/SePayIpnService.cs)

**Bước idempotency + ghi log (luôn chạy):**
- Tính `referenceCode` (nếu thiếu → `NOCODE-{guid}`).
- Tra `SePayIpnLog` theo `ReferenceCode`. Đã có dòng `Outcome = Processed` → trả **`Duplicate`** ngay, không làm gì.
- Chưa có → tạo dòng log mới; ghi `RawPayload` (JSON nguyên văn), `TransferAmount`, `TransferType`.

**Bước đánh giá (`EvaluateAsync`):**

| Kiểm tra | Không đạt → |
|---|---|
| `transferType == "in"` | `Ignored` — "Ignore out transaction" |
| trích được `SUBSCRIPTION_{id}` từ nội dung (regex `SUBSCRIPTION[\-_]?(\d+)`) | `Ignored` — "Invalid content" |
| subscription tồn tại | `Ignored` — "Subscription not found" |
| subscription **chưa** `Active` | `Duplicate` — "Already processed" |
| subscription không phải `Cancelled` / `Expired` | `Ignored` — "no longer payable" |
| `\|transferAmount − round(AmountPaid)\| ≤ SePay:AmountToleranceVnd` (mặc định 0) | `AmountMismatch` |

**Nếu tất cả đạt → transaction lồng:**
```
BeginTransactionAsync()
  Payment.Status        = Completed
  Payment.TransactionId = referenceCode
  Payment.PaymentDate   = now
  Subscription.Status   = Active
  Subscription.StartDate = now
  Subscription.EndDate   = now + Package.DurationDays (fallback 30)
  → hết hạn mọi Subscription Active KHÁC của cùng student (guard A2-11)
  log.Outcome = Processed
  SaveChangesAsync()
CommitAsync()
```

Controller **luôn trả 200** cho các outcome đã xử lý (`Processed` / `Duplicate` / `Ignored` / `AmountMismatch`); chỉ trả **500** khi có exception chưa bắt — **cố ý**, để SePay retry.

### Pha C — Job dọn dẹp vòng đời (lưới an toàn)

[SubscriptionLifecycleService.RunSweepAsync](../Services/Implementations/SubscriptionLifecycleService.cs) — chạy trên `PeriodicTimer` mỗi `SePay:LifecycleIntervalMinutes` (mặc định 5′, ≤0 tắt), hoặc `POST /api/finance/subscriptions/run-lifecycle`:
- `Active` mà `EndDate ≤ now` → `Expired`.
- `Pending` mà `CreatedAt ≤ now − SePay:PendingTimeoutMinutes` (mặc định 30′) → `Cancelled`, Payment kèm theo → `Failed`.

### Đối soát

`GET /api/finance/subscriptions/reconciliation` ([FinanceController.cs](../Controllers/FinanceController.cs), Finance/Admin) → `ReconciliationReport`:
- `CompletedPaymentCount/Total`, `ActiveSubscriptionCount/AmountTotal`
- `ActiveWithoutCompletedPayment` (drift), `CompletedPaymentWithoutActiveSubscription`
- `Balanced = ActiveWithoutCompletedPayment == 0`

---

## 3. Đảm bảo tính toàn vẹn khi lag / crash / lỗi mạng

| Lớp | Cơ chế |
|---|---|
| **1 — Transaction ACID** | `CreatePendingAsync` và nhánh kích hoạt bọc mọi thao tác nhiều dòng trong `BeginTransaction … Commit`. Postgres đảm bảo commit nguyên tử + bền vững (WAL fsync). |
| **2 — Không lộ trạng thái trung gian** | Kích hoạt lật `Payment.Completed` + `Subscription.Active` + hết hạn sub cũ + `log.Processed` trong **một commit**. |
| **3 — Idempotency** | Unique index `SePayIpnLog.ReferenceCode` — hai IPN cùng ref chạy song song: một thắng, cái kia dính unique violation → 500 → retry → thấy `Processed` → `Duplicate`. Thêm check `subscription.Status == Active` chặn replay bằng ref khác. |
| **4 — Webhook trả 500 ⇒ SePay retry** | Kết hợp lớp 3 ⇒ eventually consistent: tiền đã về thì việc kích hoạt xảy ra đúng một lần khi server hồi phục. |
| **5 — Sweep + đối soát** | `Pending` mồ côi bị thu hồi sau 30′; báo cáo đối soát bắt drift. |

### Phân tích theo điểm sập

| Sập ở đâu | Kết cục |
|---|---|
| Tạo pending, **trước commit** | Rollback → không có Payment/Subscription mồ côi; client retry |
| Sau commit pending, user chưa trả | Dòng `Pending` tồn tại → trả sau thì IPN kích hoạt; không trả → sweep huỷ sau 30′ |
| Tiền đã về, **server sập lúc IPN** | SePay retry webhook → kích hoạt khi hồi phục; retry cạn → drift hiện ở đối soát → kích hoạt tay `PATCH /api/subscriptions/{id}/status` |
| IPN nhận rồi, **sập giữa transaction** (trước commit) | Inner tx không commit (kể cả dòng log) → SePay retry → xử lý lại từ đầu, sạch |
| **Sập sau `CommitAsync()` inner** | Mọi thứ đã bền vững; retry → `Duplicate` |
| Hai IPN cùng ref **song song** | Unique index serialize; một commit, cái kia rollback + 500 → retry → `Duplicate` |

---

## 4. Liệt kê case

### ✅ Happy

| # | Kịch bản | Kết quả |
|---|---|---|
| H1 | Tạo sub → quét QR → chuyển đúng tiền → IPN `in` hợp lệ | `Active`, `Payment.Completed`, `TransactionId = referenceCode`, `EndDate = now + DurationDays` |
| H2 | IPN đến 2 lần cùng ref | Lần 2 → `Duplicate`, chỉ 1 dòng log |
| H3 | IPN ref mới nhưng sub đã `Active` | `Duplicate` — không kích hoạt lại |
| H4 | Mua gói mới khi đang có gói `Active` | Sub mới `Active`, sub cũ tự động `Expired` |
| H5 | Overpay/underpay trong `AmountToleranceVnd` | Vẫn kích hoạt |
| H6 | Tạo pending rồi bỏ, 30′ sau | Sweep: sub → `Cancelled`, payment → `Failed` |
| H7 | Sub `Active` quá `EndDate` | Sweep → `Expired`, dashboard mất Premium |
| H8 | Server sập lúc xử lý IPN | SePay retry → kích hoạt đúng 1 lần khi hồi phục |

### ⚠️ Bad case (được xử lý)

| # | Kịch bản | Cách chặn |
|---|---|---|
| B1 | Client cố đặt giá 1đ | Bỏ `AmountPaid` client, dùng `Package.Price` (A2‑02) |
| B2 | Chuyển sai số tiền (ngoài dung sai) | `AmountMismatch` — sub **giữ `Pending`** |
| B3 | Replay 1 giao dịch để kích hoạt 2 sub | Unique index `ReferenceCode` → IPN thứ 2 lỗi |
| B4 | Giao dịch `out` gửi vào IPN | `Ignored` |
| B5 | Nội dung CK sai/thiếu `SUBSCRIPTION_{id}` | `Ignored` |
| B6 | IPN trỏ subscription không tồn tại | `Ignored` + 200 |
| B7 | IPN thiếu / sai API key | **401** ([SePayApiKeyAttribute](../Attributes/SePayApiKeyAttribute.cs)) |
| B8 | CK vào sub đã `Cancelled`/`Expired` | `Ignored` — "no longer payable"; tiền có thật nhưng không kích hoạt → cần hoàn tiền tay ([Luồng hoàn tiền](Luong-hoan-tien.md)) |
| B9 | 2 IPN cùng ref song song | Unique index serialize; 1 rollback + 500 → retry → `Duplicate` |

### 🔴 Rủi ro còn lại

| # | Kịch bản | Ghi chú |
|---|---|---|
| R1 | Chuyển khoản **2 lần** (2 ref khác nhau) vào cùng 1 sub `Pending`, IPN về **song song** | Không có `SELECT … FOR UPDATE` trên `Subscription` — cả hai có thể kích hoạt 1 Payment; giao dịch thứ 2 không ghi nhận riêng → hoàn tiền tay. Về tuần tự thì check `Status == Active` chặn được |
| R2 | IPN mismatch trả **200** | SePay không retry → nếu do lỗi tạm thời thì mất; cân nhắc 4xx |
| R3 | Hoàn tiền mặt về ngân hàng | Chưa tự động — xem [Luồng hoàn tiền](Luong-hoan-tien.md) |
| R4 | Chưa có `Order` / `OrderItem` | Chưa mua khoá lẻ, chỉ mua gói subscription |

---

## 5. Cấu hình

| Key (`appsettings.json` → `SePay`) | Mặc định | Ý nghĩa |
|---|---|---|
| `SePay:ApiKeyValidator` | — | so với header `Apikey xxx` của IPN |
| `SePay:VA` / `BankName` / `BaseUrl` | — | sinh URL QR |
| `SePay:AmountToleranceVnd` | 0 | dung sai over/under‑payment khi khớp IPN |
| `SePay:PendingTimeoutMinutes` | 30 | nhả `Pending` quá hạn |
| `SePay:LifecycleIntervalMinutes` | 5 | nhịp job sweep (≤0 tắt) |

**Route quan trọng:** `POST /api/subscriptions`, `POST /api/sepay/ipn`, `GET /api/subscriptions/me`, `GET /api/payments/me`, `GET /api/finance/subscriptions/reconciliation`, `POST /api/finance/subscriptions/run-lifecycle`.

---

## 6. Entity

| Entity | Trạng thái | Ghi chú |
|---|---|---|
| `Payment` | `Pending → Completed → {Refunded, PartiallyRefunded}` / `Failed` | `PaidByUserId` (người trả) ≠ `StudentId` (thụ hưởng); `RefundAmount` tích luỹ |
| `Subscription` | `Pending → Active → {Expired, Cancelled}` | 1:1 với `Payment` (FK ở `Subscription.PaymentId`) |
| `SePayIpnLog` | `Received → {Processed, Duplicate, Ignored, AmountMismatch}` | `ReferenceCode` **unique**; `RawPayload` nguyên văn |
