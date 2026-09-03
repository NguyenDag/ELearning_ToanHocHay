using System.Security.Claims;
using ELearning_ToanHocHay_Control.Common;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Models.DTOs.Refund;

namespace ELearning_ToanHocHay_Control.Services.Interfaces
{
    public record RefundCsvFile(byte[] Content, string FileName);

    /// <summary>Pha 2 — gộp lô các yêu cầu hoàn đã duyệt, xuất file chi hộ CSV, xác nhận đã chuyển.</summary>
    public interface IRefundBatchService
    {
        Task<ApiResponse<RefundBatchDetailDto>> CreateAsync(CreateRefundBatchDto dto, ClaimsPrincipal actor);
        Task<ApiResponse<PagedResult<RefundBatchDto>>> ListAsync(PagedRequest request, RefundBatchStatus? status);
        Task<ApiResponse<RefundBatchDetailDto>> GetByIdAsync(int id);
        Task<ApiResponse<RefundCsvFile>> ExportCsvAsync(int id, ClaimsPrincipal actor);
        Task<ApiResponse<RefundBatchDto>> MarkDisbursedAsync(int id, MarkBatchDisbursedDto dto, ClaimsPrincipal actor);
        Task<ApiResponse<RefundBatchDto>> ConfirmAllAsync(int id, ClaimsPrincipal actor);
        Task<ApiResponse<RefundBatchDto>> CancelAsync(int id, ClaimsPrincipal actor);
    }
}
