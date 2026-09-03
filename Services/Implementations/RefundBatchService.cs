using System.Security.Claims;
using ELearning_ToanHocHay_Control.Common;
using ELearning_ToanHocHay_Control.Data;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Models.DTOs.Refund;
using ELearning_ToanHocHay_Control.Services.Helpers;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ELearning_ToanHocHay_Control.Services.Implementations
{
    public class RefundBatchService : IRefundBatchService
    {
        private readonly AppDbContext _context;
        private readonly IRefundFieldProtector _protector;
        private readonly IRefundEventWriter _events;
        private readonly ILogger<RefundBatchService> _logger;

        public RefundBatchService(
            AppDbContext context,
            IRefundFieldProtector protector,
            IRefundEventWriter events,
            ILogger<RefundBatchService> logger)
        {
            _context = context;
            _protector = protector;
            _events = events;
            _logger = logger;
        }

        public async Task<ApiResponse<RefundBatchDetailDto>> CreateAsync(CreateRefundBatchDto dto, ClaimsPrincipal actor)
        {
            var wantIds = dto.RefundRequestIds?.Distinct().ToList();

            var query = _context.RefundRequests
                .Where(r => r.Status == RefundRequestStatus.Approved && r.RefundBatchId == null);
            if (wantIds is { Count: > 0 })
                query = query.Where(r => wantIds.Contains(r.RefundRequestId));

            var requests = await query.ToListAsync();

            if (wantIds is { Count: > 0 } && requests.Count != wantIds.Count)
                return ApiResponse<RefundBatchDetailDto>.ErrorResponse(
                    "Một số yêu cầu không tồn tại, chưa được duyệt, hoặc đã thuộc lô khác");
            if (requests.Count == 0)
                return ApiResponse<RefundBatchDetailDto>.ErrorResponse("Không có yêu cầu hoàn tiền nào để gộp lô");

            var now = DateTime.UtcNow;
            var batch = new RefundBatch
            {
                PublicId = Guid.NewGuid(),
                Status = RefundBatchStatus.Draft,
                CreatedByUserId = actor.GetUserId()!.Value,
                CreatedAt = now,
                ItemCount = requests.Count,
                TotalAmount = requests.Sum(r => r.Amount)
            };
            _context.RefundBatches.Add(batch);
            await _context.SaveChangesAsync();

            foreach (var r in requests)
            {
                r.RefundBatchId = batch.RefundBatchId;
                r.Status = RefundRequestStatus.Batched;
                r.UpdatedAt = now;
                _events.Add(_context, RefundEventType.AddedToBatch, r.RefundRequestId, batch.RefundBatchId,
                    fromStatus: nameof(RefundRequestStatus.Approved),
                    toStatus: nameof(RefundRequestStatus.Batched), amount: r.Amount);
            }
            _events.Add(_context, RefundEventType.AddedToBatch, refundBatchId: batch.RefundBatchId,
                amount: batch.TotalAmount, note: $"Gộp lô {batch.ItemCount} yêu cầu");
            await _context.SaveChangesAsync();

            _logger.LogInformation("Refund batch {BatchId} created: {Count} items, total {Total}",
                batch.RefundBatchId, batch.ItemCount, batch.TotalAmount);

            return ApiResponse<RefundBatchDetailDto>.Created(await BuildDetailAsync(batch.RefundBatchId), "Đã tạo lô chi hộ");
        }

        public async Task<ApiResponse<PagedResult<RefundBatchDto>>> ListAsync(
            PagedRequest request, RefundBatchStatus? status)
        {
            var q = _context.RefundBatches.AsNoTracking().AsQueryable();
            if (status.HasValue) q = q.Where(b => b.Status == status.Value);

            var page = await q.OrderByDescending(b => b.CreatedAt).ToPagedResultAsync(request);
            return ApiResponse<PagedResult<RefundBatchDto>>.SuccessResponse(page.Map(MapDto));
        }

        public async Task<ApiResponse<RefundBatchDetailDto>> GetByIdAsync(int id)
        {
            if (!await _context.RefundBatches.AnyAsync(b => b.RefundBatchId == id))
                return ApiResponse<RefundBatchDetailDto>.NotFound("Không tìm thấy lô chi hộ");
            return ApiResponse<RefundBatchDetailDto>.SuccessResponse(await BuildDetailAsync(id));
        }

        public async Task<ApiResponse<RefundCsvFile>> ExportCsvAsync(int id, ClaimsPrincipal actor)
        {
            var batch = await _context.RefundBatches
                .Include(b => b.Items)
                .FirstOrDefaultAsync(b => b.RefundBatchId == id);
            if (batch == null)
                return ApiResponse<RefundCsvFile>.NotFound("Không tìm thấy lô chi hộ");
            if (batch.Status is not (RefundBatchStatus.Draft or RefundBatchStatus.Exported))
                return ApiResponse<RefundCsvFile>.Conflict(
                    $"Chỉ xuất được lô ở trạng thái Draft / Exported (hiện {batch.Status})");

            var items = batch.Items.OrderBy(r => r.RefundRequestId).ToList();
            var content = RefundCsvWriter.Build(items, _protector);
            var fileName = $"refund-batch-{batch.PublicId:N}.csv";

            var now = DateTime.UtcNow;
            var firstExport = batch.Status == RefundBatchStatus.Draft;
            if (firstExport)
            {
                batch.Status = RefundBatchStatus.Exported;
                batch.ExportedByUserId = actor.GetUserId();
                batch.ExportedAt = now;
            }
            _events.Add(_context, RefundEventType.BatchExported, refundBatchId: batch.RefundBatchId,
                fromStatus: firstExport ? nameof(RefundBatchStatus.Draft) : nameof(RefundBatchStatus.Exported),
                toStatus: nameof(RefundBatchStatus.Exported), amount: batch.TotalAmount,
                note: $"Xuất CSV {items.Count} dòng, tổng {batch.TotalAmount:N0} VND");
            await _context.SaveChangesAsync();

            _logger.LogInformation("Refund batch {BatchId} CSV exported by {Actor}: {Count} rows, total {Total}",
                batch.RefundBatchId, actor.GetUserId(), items.Count, batch.TotalAmount);

            return ApiResponse<RefundCsvFile>.SuccessResponse(new RefundCsvFile(content, fileName));
        }

        public async Task<ApiResponse<RefundBatchDto>> MarkDisbursedAsync(
            int id, MarkBatchDisbursedDto dto, ClaimsPrincipal actor)
        {
            var batch = await _context.RefundBatches
                .Include(b => b.Items)
                .FirstOrDefaultAsync(b => b.RefundBatchId == id);
            if (batch == null)
                return ApiResponse<RefundBatchDto>.NotFound("Không tìm thấy lô chi hộ");
            if (batch.Status != RefundBatchStatus.Exported)
                return ApiResponse<RefundBatchDto>.Conflict(
                    $"Chỉ đánh dấu đã chi cho lô đã Exported (hiện {batch.Status})");

            var now = DateTime.UtcNow;
            batch.Status = RefundBatchStatus.Disbursed;
            batch.DisbursedByUserId = actor.GetUserId();
            batch.DisbursedAt = dto.DisbursedAt ?? now;
            batch.DisbursementNote = dto.Note;

            foreach (var r in batch.Items.Where(r => r.Status == RefundRequestStatus.Batched))
            {
                r.Status = RefundRequestStatus.Disbursed;
                r.UpdatedAt = now;
                _events.Add(_context, RefundEventType.MarkedDisbursed, r.RefundRequestId, batch.RefundBatchId,
                    fromStatus: nameof(RefundRequestStatus.Batched),
                    toStatus: nameof(RefundRequestStatus.Disbursed), amount: r.Amount);
            }
            _events.Add(_context, RefundEventType.MarkedDisbursed, refundBatchId: batch.RefundBatchId,
                amount: batch.TotalAmount, note: dto.Note);
            await _context.SaveChangesAsync();

            return ApiResponse<RefundBatchDto>.SuccessResponse(MapDto(batch), "Đã đánh dấu lô đã chi");
        }

        public async Task<ApiResponse<RefundBatchDto>> ConfirmAllAsync(int id, ClaimsPrincipal actor)
        {
            var batch = await _context.RefundBatches
                .Include(b => b.Items)
                .FirstOrDefaultAsync(b => b.RefundBatchId == id);
            if (batch == null)
                return ApiResponse<RefundBatchDto>.NotFound("Không tìm thấy lô chi hộ");
            if (batch.Status != RefundBatchStatus.Disbursed)
                return ApiResponse<RefundBatchDto>.Conflict(
                    $"Chỉ xác nhận hoàn tất cho lô đã Disbursed (hiện {batch.Status})");

            await using var tx = await _context.Database.BeginTransactionAsync();

            var now = DateTime.UtcNow;
            var confirmed = 0;
            foreach (var r in batch.Items.Where(r => r.Status == RefundRequestStatus.Disbursed))
            {
                r.Status = RefundRequestStatus.Completed;
                r.CompletedAt = now;
                r.BankTransactionRef ??= $"BATCH-{batch.PublicId:N}";
                r.UpdatedAt = now;
                await RefundCompletion.ApplyAsync(_context, r);
                _events.Add(_context, RefundEventType.Confirmed, r.RefundRequestId, batch.RefundBatchId,
                    fromStatus: nameof(RefundRequestStatus.Disbursed),
                    toStatus: nameof(RefundRequestStatus.Completed), amount: r.Amount, note: "Xác nhận theo lô");
                confirmed++;
            }

            batch.Status = RefundBatchStatus.Completed;
            _events.Add(_context, RefundEventType.Confirmed, refundBatchId: batch.RefundBatchId,
                amount: batch.TotalAmount, note: $"Xác nhận hoàn tất {confirmed} yêu cầu");
            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            _logger.LogInformation("Refund batch {BatchId} confirmed: {Count} requests completed", id, confirmed);
            return ApiResponse<RefundBatchDto>.SuccessResponse(MapDto(batch), $"Đã xác nhận {confirmed} yêu cầu hoàn tất");
        }

        public async Task<ApiResponse<RefundBatchDto>> CancelAsync(int id, ClaimsPrincipal actor)
        {
            var batch = await _context.RefundBatches
                .Include(b => b.Items)
                .FirstOrDefaultAsync(b => b.RefundBatchId == id);
            if (batch == null)
                return ApiResponse<RefundBatchDto>.NotFound("Không tìm thấy lô chi hộ");
            if (batch.Status is RefundBatchStatus.Completed or RefundBatchStatus.Cancelled)
                return ApiResponse<RefundBatchDto>.Conflict($"Không thể huỷ lô ở trạng thái {batch.Status}");

            var now = DateTime.UtcNow;
            var reverted = 0;
            foreach (var r in batch.Items.Where(r =>
                r.Status is RefundRequestStatus.Batched or RefundRequestStatus.Disbursed))
            {
                var from = r.Status.ToString();
                r.Status = RefundRequestStatus.Approved;
                r.RefundBatchId = null;
                r.UpdatedAt = now;
                _events.Add(_context, RefundEventType.RemovedFromBatch, r.RefundRequestId, batch.RefundBatchId,
                    fromStatus: from, toStatus: nameof(RefundRequestStatus.Approved), amount: r.Amount);
                reverted++;
            }

            batch.Status = RefundBatchStatus.Cancelled;
            batch.CancelledAt = now;
            _events.Add(_context, RefundEventType.Cancelled, refundBatchId: batch.RefundBatchId,
                note: $"Huỷ lô, trả {reverted} yêu cầu về Approved");
            await _context.SaveChangesAsync();

            return ApiResponse<RefundBatchDto>.SuccessResponse(MapDto(batch), "Đã huỷ lô chi hộ");
        }

        // ---------------------------------------------------------------- helpers

        private async Task<RefundBatchDetailDto> BuildDetailAsync(int batchId)
        {
            var batch = await _context.RefundBatches.AsNoTracking()
                .Include(b => b.Items)
                .FirstAsync(b => b.RefundBatchId == batchId);

            var detail = new RefundBatchDetailDto();
            CopyInto(batch, detail);
            detail.Items = batch.Items
                .OrderBy(r => r.RefundRequestId)
                .Select(r => new RefundRequestDto
                {
                    RefundRequestId = r.RefundRequestId,
                    PublicId = r.PublicId,
                    PaymentId = r.PaymentId,
                    BeneficiaryUserId = r.BeneficiaryUserId,
                    OnBehalf = r.OnBehalf,
                    ReasonCode = r.ReasonCode,
                    ReasonNote = r.ReasonNote,
                    Amount = r.Amount,
                    Status = r.Status,
                    BankBin = r.BankBin,
                    BankAccountNumberLast4 = r.BankAccountNumberLast4,
                    BankAccountHolderName = r.BankAccountHolderName,
                    ApprovedByUserId = r.ApprovedByUserId,
                    ApprovedAt = r.ApprovedAt,
                    RefundBatchId = r.RefundBatchId,
                    BankTransactionRef = r.BankTransactionRef,
                    RejectionReason = r.RejectionReason,
                    FailureReason = r.FailureReason,
                    CompletedAt = r.CompletedAt,
                    CreatedAt = r.CreatedAt
                })
                .ToList();
            return detail;
        }

        private static RefundBatchDto MapDto(RefundBatch b)
        {
            var dto = new RefundBatchDto();
            CopyInto(b, dto);
            return dto;
        }

        private static void CopyInto(RefundBatch b, RefundBatchDto dto)
        {
            dto.RefundBatchId = b.RefundBatchId;
            dto.PublicId = b.PublicId;
            dto.Status = b.Status;
            dto.ItemCount = b.ItemCount;
            dto.TotalAmount = b.TotalAmount;
            dto.CreatedByUserId = b.CreatedByUserId;
            dto.CreatedAt = b.CreatedAt;
            dto.ExportedAt = b.ExportedAt;
            dto.DisbursedAt = b.DisbursedAt;
            dto.DisbursementNote = b.DisbursementNote;
        }
    }
}
