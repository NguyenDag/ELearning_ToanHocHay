# Luồng hoàn tiền — bán tự động (semi-automatic refund)

> **Bản nội bộ · không phân phối ra ngoài**
> Thu tiền: [Luồng thanh toán](Luong-thanh-toan.md). Nhánh `feat/refund-workflow`, migration `P8_RefundWorkflow`.

| | |
|---|---|
| **Mô hình** | Hệ thống quản lý toàn bộ vòng đời yêu cầu; **tiền do người chuyển** (Finance chuyển khoản tay từ internet banking). Không tích hợp payout API. |
| **Vì sao không tự động 100%** | IPN của SePay không trả số TK/tên người chuyển, và dự án chưa có tích hợp *chuyển tiền đi*. Xuất file chi hộ CSV là bước giảm ~90% thao tác tay. |

---

## 1. Tổng quan

```
Người dùng / Finance          Backend                         Finance (internet banking)
  │  POST /api/refunds            │                                     │
  │  (hoặc /api/finance/refunds)  │  RefundRequest(PendingReview)        │
  │ ────────────────────────────► │  + RefundEvent(Created)             │
  │                              │  → thông báo Finance                │
  │                              │                                     │
  │     Finance: approve ───────► │  PendingReview → Approved           │
  │                              │  (kiểm trần/ngày + dual-control)    │
  │                              │                                     │
  │     Finance: tạo lô ────────► │  RefundBatch(Draft), request→Batched│
  │     Finance: export ────────► │  → file CSV chi hộ ────────────────►│  upload lên bank
  │     Finance: mark-disbursed ─►│  Exported → Disbursed              │  (chuyển khoản thật)
  │     Finance: confirm-all ───► │  Disbursed → Completed             │
  │                              │  → Payment.Refunded / PartiallyRefunded
```

Thiết kế tách **tiền** khỏi **quyền lợi**: `RefundRequest` điều phối; hiệu ứng lên `Payment` + `Subscription` chỉ xảy ra khi một yêu cầu đạt `Completed`.

---

## 2. Entity ([Data/Entities/Refund.cs](../Data/Entities/Refund.cs))

| Entity | Vai trò |
|---|---|
| **`RefundRequest`** | Một yêu cầu hoàn tiền cho một `Payment`; vòng đời + thông tin TK người nhận (số TK **mã hoá**) |
| **`RefundBatch`** | Một lô gộp nhiều `RefundRequest` đã duyệt để xuất một file chi hộ |
| **`RefundEvent`** | Timeline truy vết — mỗi chuyển trạng thái ghi một dòng (actor + IP + correlation-id + from/to + amount + note) |

Số TK ngân hàng người nhận (`BankAccountNumberProtected`) được mã hoá bằng **ASP.NET Data Protection** ([RefundFieldProtector.cs](../Services/Helpers/RefundFieldProtector.cs)), key ring persist vào bảng `DataProtectionKeys`. API chỉ trả **4 số cuối**; giải mã duy nhất khi build file CSV.

---

## 3. Máy trạng thái

### `RefundRequest`

```
PendingReview ──approve (single)─────────────► Approved
PendingReview ──approve (dual, lần 1)────────► PendingSecondApproval
PendingSecondApproval ──approve (người khác)─► Approved
{PendingReview, PendingSecondApproval} ──reject──► Rejected        (kết thúc)
{PendingReview, PendingSecondApproval, Approved, Failed} ──cancel──► Cancelled  (chỉ khi chưa vào lô)
Approved ──(tạo lô)──► Batched
Batched  ──(batch mark-disbursed)──► Disbursed
{Approved, Disbursed} ──confirm (bankRef)──► Completed             (kết thúc — áp hiệu ứng Payment)
{Approved, Disbursed} ──mark-failed──► Failed
Failed   ──retry──► Approved
Batched / Disbursed ──(batch cancel)──► Approved
```

### `RefundBatch`

`Draft → Exported → Disbursed → Completed`; `{Draft, Exported, Disbursed} → Cancelled` (các request thành viên quay về `Approved`).

### Hiệu ứng khi `RefundRequest → Completed` ([RefundCompletion.cs](../Services/Helpers/RefundCompletion.cs), trong transaction)

- `Payment.RefundAmount += request.Amount` (**tích luỹ**)
- `RefundAmount ≥ Payment.Amount` → `Payment.Status = Refunded`, else `PartiallyRefunded`
- `Payment.RefundedAt = now`
- Hoàn **toàn phần** và `Payment.Subscription` đang `Active`/`Pending` → `Cancelled`

---

## 4. API

### Người dùng — [RefundsController](../Controllers/RefundsController.cs) `[Route("api/refunds")] [Authorize]`

| Method | Đường dẫn | Ghi chú |
|---|---|---|
| POST | `/api/refunds` | `[EnableRateLimiting("refund")]`; body `CreateRefundRequestDto`; caller **phải sở hữu** giao dịch (`IResourceAccessService.CanAccessPaymentAsync`) |
| GET | `/api/refunds/me` | phân trang; chỉ yêu cầu của mình (là người thụ hưởng hoặc người tạo) |
| GET | `/api/refunds/{id}` | chi tiết + timeline `Events`; chủ sở hữu hoặc Finance |

### Finance — [FinanceController](../Controllers/FinanceController.cs) `[Route("api/finance")] [AuthorizeUserType(FinanceManager, SystemAdmin)]`

| Method | Đường dẫn | Ghi chú |
|---|---|---|
| POST | `/api/finance/refunds` | tạo hộ; body `CreateRefundRequestDto` (beneficiary suy từ `PaymentId`) |
| GET | `/api/finance/refunds?status=&page=` | danh sách phân trang |
| GET | `/api/finance/refunds/{id}` | chi tiết + events |
| GET | `/api/finance/refunds/daily-usage` | `{ CapVnd, UsedVnd, RemainingVnd, WindowStartUtc, ResetAtUtc }` |
| GET | `/api/finance/refunds/reconciliation` | `RefundReconciliationReport` |
| POST | `/api/finance/refunds/{id}/approve` | body `ApproveRefundDto { Note? }` — kiểm trần/ngày + dual-control |
| POST | `/api/finance/refunds/{id}/reject` | body `RejectRefundDto { Reason }` |
| POST | `/api/finance/refunds/{id}/cancel` | chỉ khi chưa vào lô |
| POST | `/api/finance/refunds/{id}/confirm` | body `ConfirmRefundDto { BankTransactionRef, Note? }` → `Completed` |
| POST | `/api/finance/refunds/{id}/mark-failed` | body `MarkRefundFailedDto { Reason }` |
| POST | `/api/finance/refunds/{id}/retry` | `Failed → Approved` |
| POST | `/api/finance/refund-batches` | body `CreateRefundBatchDto { RefundRequestIds? }` (rỗng = tất cả `Approved`) |
| GET | `/api/finance/refund-batches?status=&page=` | danh sách |
| GET | `/api/finance/refund-batches/{id}` | chi tiết + request thành viên |
| GET | `/api/finance/refund-batches/{id}/export` | file `text/csv` (xem §5); `Draft → Exported` |
| POST | `/api/finance/refund-batches/{id}/mark-disbursed` | body `MarkBatchDisbursedDto { Note?, DisbursedAt? }` |
| POST | `/api/finance/refund-batches/{id}/confirm-all` | tất cả thành viên `Disbursed → Completed` (transaction) |
| POST | `/api/finance/refund-batches/{id}/cancel` | thành viên quay về `Approved` |

> **Breaking:** `POST /api/payments/{id}/refund` cũ (refund một-bước) đã **xoá**. WebApp phải chuyển sang luồng này.

---

## 5. File chi hộ CSV ([RefundCsvWriter.cs](../Services/Helpers/RefundCsvWriter.cs))

Mẫu generic ngân hàng VN, UTF-8, CRLF:

```
STT,SoTaiKhoan,TenNguoiHuong,MaNganHang,SoTien,NoiDung
1,0071000123456,NGUYEN VAN A,970436,199000,HOAN TIEN CustomerRequest REF 3f2a...
```

- `SoTaiKhoan` = giải mã `BankAccountNumberProtected`
- `MaNganHang` = `BankBin` (mã napas)
- `SoTien` = `(long)Amount` (VND nguyên)
- `NoiDung` = ASCII không dấu: `HOAN TIEN {ReasonCode} REF {PublicId}`
- Escape RFC 4180 + **chống CSV formula-injection**: field bắt đầu bằng `= + - @ TAB CR` được thêm dấu `'` để Excel coi là text (`TenNguoiHuong` là free text do người yêu cầu nhập).

---

## 6. Ba kiểm soát

### 6.1 Trần hoàn tiền/ngày

Kiểm khi **duyệt** (và khi thử lại): `usedToday + request.Amount > refund.dailyCapVnd` → từ chối `400` "Vượt trần…".
- `usedToday` = Σ `RefundRequest.Amount` với `Status ∈ {Approved, Batched, Disbursed, Completed}` và `ApprovedAt ≥ mốc 00:00 giờ VN`.
- Mốc "ngày" tính theo `refund.timezoneOffsetHours` (mặc định 7).
- Minh bạch: `GET /api/finance/refunds/daily-usage`.

### 6.2 Rate-limit theo người dùng — hai lớp

| Lớp | Cơ chế |
|---|---|
| **Cổng** | policy `"refund"` ([Program.cs](../Program.cs)) — fixed window theo user id, `RateLimiting:RefundPermitLimit` (mặc định 5/phút) trên `POST /api/refunds` và `POST /api/finance/refunds` |
| **Nghiệp vụ** | [RefundService.CreateAsync](../Services/Implementations/RefundService.cs) — đếm `RefundRequest` theo **người thụ hưởng** trong 30 ngày trượt (trừ `Rejected`/`Cancelled`); `≥ refund.maxRequestsPerUserPer30d` (mặc định 3) → `409`. Finance tạo hộ **cũng tính**; `SystemAdmin` được bỏ qua |

### 6.3 Ghi log rõ ràng — ba tầng

1. **`RefundEvent`** — mọi chuyển trạng thái: `EventType`, `From/ToStatus`, `ActorUserId`, `ActorUserType`, `IpAddress`, `CorrelationId`, `AmountSnapshot`, `Note`. Trả kèm `GET /api/finance/refunds/{id}` (`Events[]`).
2. **`AuditSaveChangesInterceptor`** ([AuditSaveChangesInterceptor.cs](../Data/AuditSaveChangesInterceptor.cs)) — theo dõi `RefundRequest.Status` / `Amount` và `RefundBatch.Status`; bắt cả sửa DB trực tiếp.
3. **Serilog** — structured log mỗi bước ([RefundEventWriter.cs](../Services/Helpers/RefundEventWriter.cs)): `Refund event {EventType} req={id} {From}->{To} amount={Amount} actor={ActorId} corr={CorrelationId}`.

### 6.4 Dual-control (tuỳ chọn)

`refund.dualControlThresholdVnd` (mặc định `0` = tắt). Khi `> 0` và `Amount ≥ ngưỡng`:
- Duyệt lần 1 → `PendingSecondApproval` (`FirstApprovedByUserId`), **chưa** tiêu trần/ngày.
- Duyệt lần 2 phải là **người Finance khác** (cùng người → `409`) → `Approved`, kiểm trần/ngày.

---

## 7. Đối soát & cảnh báo

`GET /api/finance/refunds/reconciliation` → `RefundReconciliationReport`:

| Trường | Ý nghĩa |
|---|---|
| `PendingReviewCount` | chờ duyệt (gồm `PendingSecondApproval`) |
| `ApprovedNotBatchedCount` | đã duyệt nhưng chưa gộp lô |
| `DisbursedNotCompletedCount` | đã chi nhưng chưa xác nhận |
| `StaleDisbursedCount` | `Disbursed` quá `refund.staleDisbursedDays` (mặc định 3) |
| `BatchesAwaitingDisbursementCount` | lô `Exported` chưa `mark-disbursed` |
| `CompletedRefundTotal` vs `PaymentRefundedTotal` | phải khớp → `Balanced` |

**Sweep nền** (piggy-back trên `SubscriptionLifecycleHostedService`): yêu cầu `Disbursed` quá hạn → sinh `Notification` cho Finance (không tự đổi trạng thái — người quyết định).

---

## 8. Cấu hình (`SystemConfig`, group `refund`, sửa qua `PUT /api/admin/config/{key}`)

| Key | Mặc định | Ý nghĩa |
|---|---|---|
| `refund.dailyCapVnd` | `20000000` | trần tổng tiền hoàn được duyệt / ngày |
| `refund.maxRequestsPerUserPer30d` | `3` | số yêu cầu / người thụ hưởng / 30 ngày trượt |
| `refund.maxPaymentAgeDays` | `180` | không hoàn giao dịch cũ hơn X ngày |
| `refund.dualControlThresholdVnd` | `0` | `≥` ngưỡng cần 2 người duyệt; 0 = tắt |
| `refund.timezoneOffsetHours` | `7` | mốc "ngày" tính trần |
| `refund.staleDisbursedDays` | `3` | `Disbursed` quá hạn → cảnh báo Finance |

`RateLimiting:RefundPermitLimit` (appsettings, mặc định 5) — rate-limit cổng.

---

## 9. Liệt kê case

### ✅ Happy

| # | Kịch bản | Kết quả |
|---|---|---|
| H1 | Học sinh gửi yêu cầu cho giao dịch của mình | `PendingReview`, `RefundEvent(Created)`, Finance nhận thông báo |
| H2 | Finance duyệt → gộp lô → export → mark-disbursed → confirm-all | request `Completed`, `Payment.Refunded`, `RefundAmount` = full, sub `Cancelled` |
| H3 | Hoàn một phần (amount < payment) | `Payment.PartiallyRefunded`; phần còn lại có thể yêu cầu tiếp |
| H4 | Confirm đơn lẻ (không qua lô) từ `Approved` | cho phép — one-off; `bankRef` bắt buộc |
| H5 | Huỷ lô | request thành viên quay về `Approved` |
| H6 | `Failed` → retry | về `Approved` (kiểm lại trần/ngày), rời khỏi lô |
| H7 | Dual-control (threshold > 0) | 2 người Finance khác nhau mới `Approved` |

### ⚠️ Bad case (được xử lý)

| # | Kịch bản | Cách chặn |
|---|---|---|
| B1 | Học sinh gửi yêu cầu cho giao dịch **người khác** | `403` (`CanAccessPaymentAsync`) |
| B2 | Hoàn giao dịch `Pending`/`Failed` | `400` — "Chỉ hoàn tiền được giao dịch đã Completed" |
| B3 | `amount` > số còn có thể hoàn | `400` |
| B4 | Giao dịch quá `refund.maxPaymentAgeDays` | `400` |
| B5 | Đã có yêu cầu đang xử lý cho giao dịch đó | `409` |
| B6 | Quá `refund.maxRequestsPerUserPer30d` | `409` |
| B7 | Duyệt vượt trần/ngày | `400` — "Vượt trần…" |
| B8 | Cùng người duyệt cả 2 lần (dual-control) | `409` |
| B9 | Duyệt/confirm một yêu cầu đã `Rejected`/`Completed` | `409` — state machine chặn |
| B10 | Non-finance gọi `/api/finance/refunds/*` | `403` (class-level `[AuthorizeUserType]`) |
| B11 | Ẩn danh gọi `/api/refunds` | `401` |
| B12 | Tên chủ TK chứa công thức Excel (`=…`) | CSV writer thêm `'` — Excel coi là text |
| B13 | Số TK ngân hàng trong DB dump | Ciphertext (Data Protection); API chỉ trả 4 số cuối |

### 🔴 Ngoài phạm vi (pha sau)

- Payout API — chi tiền tự động về ngân hàng; webhook outbound xác nhận
- Name-inquiry — xác thực tên chủ TK khớp tên người dùng
- Mẫu CSV riêng theo từng ngân hàng (hiện chỉ generic)
- Auto-refund cho case B2 overpay / B8 orphan của [luồng thanh toán](Luong-thanh-toan.md)
- Hoá đơn điều chỉnh VAT
- Wallet / số dư nội bộ (kênh hoàn thay thế, tự động 100%)

---

## 10. Test

[`Tests/RefundWorkflowTests.cs`](../Tests/RefundWorkflowTests.cs) (16) + [`Tests/RemainingFeaturesTests.cs`](../Tests/RemainingFeaturesTests.cs) — tạo/ownership, per-user limit, trần/ngày, dual-control, reject→approve chặn, full batch flow + audit trail, hoàn một phần, huỷ lô, role-gating (401/403), CSV formula-injection, hoàn toàn phần huỷ subscription.
