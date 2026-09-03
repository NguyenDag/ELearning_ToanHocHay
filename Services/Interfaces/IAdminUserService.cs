using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Models.DTOs.Question;

namespace ELearning_ToanHocHay_Control.Services.Interfaces
{
    /// <summary>P1 — admin account operations, each written to the AuditLog.</summary>
    public interface IAdminUserService
    {
        Task<ApiResponse<UserDto>> LockUserAsync(int targetUserId, int adminUserId, string reason, string? ip);
        Task<ApiResponse<UserDto>> UnlockUserAsync(int targetUserId, int adminUserId, string? ip);
        Task<ApiResponse<UserDto>> ChangeRoleAsync(int targetUserId, UserType newRole, int adminUserId, string? ip);
        Task<ApiResponse<PagedResult<AuditLogDto>>> GetAuditLogsAsync(
            string? entityType, int? entityId, int? userId, int page, int pageSize);
    }
}
