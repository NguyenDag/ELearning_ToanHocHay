using System.Text.Json;
using AutoMapper;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Models.DTOs.Question;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using ELearning_ToanHocHay_Control.Services.Interfaces;

namespace ELearning_ToanHocHay_Control.Services.Implementations
{
    public class AdminUserService : IAdminUserService
    {
        private readonly IUserRepository _userRepo;
        private readonly IAuditLogRepository _auditRepo;
        private readonly IRefreshTokenRepository _refreshRepo;
        private readonly IMapper _mapper;

        public AdminUserService(
            IUserRepository userRepo,
            IAuditLogRepository auditRepo,
            IRefreshTokenRepository refreshRepo,
            IMapper mapper)
        {
            _userRepo = userRepo;
            _auditRepo = auditRepo;
            _refreshRepo = refreshRepo;
            _mapper = mapper;
        }

        public async Task<ApiResponse<UserDto>> LockUserAsync(int targetUserId, int adminUserId, string reason, string? ip)
        {
            var user = await _userRepo.GetByIdAsync(targetUserId);
            if (user == null) return ApiResponse<UserDto>.ErrorResponse("User not found");
            if (user.UserId == adminUserId) return ApiResponse<UserDto>.ErrorResponse("You cannot lock your own account");
            if (user.LockedAt.HasValue) return ApiResponse<UserDto>.ErrorResponse("Account is already locked");

            user.LockedAt = DateTime.UtcNow;
            user.LockedReason = reason;
            user.LockedByUserId = adminUserId;
            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepo.UpdateUserAsync(user);

            await _refreshRepo.RevokeAllForUserAsync(targetUserId);
            await AuditAsync(adminUserId, "LockUser", targetUserId, null, new { reason }, ip);

            return ApiResponse<UserDto>.SuccessResponse(_mapper.Map<UserDto>(user), "Account locked");
        }

        public async Task<ApiResponse<UserDto>> UnlockUserAsync(int targetUserId, int adminUserId, string? ip)
        {
            var user = await _userRepo.GetByIdAsync(targetUserId);
            if (user == null) return ApiResponse<UserDto>.ErrorResponse("User not found");
            if (!user.LockedAt.HasValue) return ApiResponse<UserDto>.ErrorResponse("Account is not locked");

            user.LockedAt = null;
            user.LockedReason = null;
            user.LockedByUserId = null;
            user.IsActive = true;
            user.FailedLoginCount = 0;
            user.LockoutEndsAt = null;
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepo.UpdateUserAsync(user);

            await AuditAsync(adminUserId, "UnlockUser", targetUserId, null, null, ip);
            return ApiResponse<UserDto>.SuccessResponse(_mapper.Map<UserDto>(user), "Account unlocked");
        }

        public async Task<ApiResponse<UserDto>> ChangeRoleAsync(int targetUserId, UserType newRole, int adminUserId, string? ip)
        {
            var user = await _userRepo.GetByIdAsync(targetUserId);
            if (user == null) return ApiResponse<UserDto>.ErrorResponse("User not found");
            if (user.UserId == adminUserId) return ApiResponse<UserDto>.ErrorResponse("You cannot change your own role");
            if (user.UserType == newRole) return ApiResponse<UserDto>.ErrorResponse($"User is already a {newRole}");

            // Student / Parent carry a dependent profile row — switching those roles here would orphan it.
            if (user.UserType is UserType.Student or UserType.Parent || newRole is UserType.Student or UserType.Parent)
                return ApiResponse<UserDto>.ErrorResponse(
                    "Cannot switch between learner (Student/Parent) and staff roles from here");

            var oldRole = user.UserType;
            user.UserType = newRole;
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepo.UpdateUserAsync(user);

            // Force re-login so the new role lands in a fresh token.
            await _refreshRepo.RevokeAllForUserAsync(targetUserId);
            await AuditAsync(adminUserId, "ChangeRole", targetUserId,
                new { role = oldRole.ToString() }, new { role = newRole.ToString() }, ip);

            return ApiResponse<UserDto>.SuccessResponse(_mapper.Map<UserDto>(user), $"Role changed to {newRole}");
        }

        public async Task<ApiResponse<PagedResult<AuditLogDto>>> GetAuditLogsAsync(
            string? entityType, int? entityId, int? userId, int page, int pageSize)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 200);

            var (items, total) = await _auditRepo.QueryAsync(entityType, entityId, userId, page, pageSize);
            return ApiResponse<PagedResult<AuditLogDto>>.SuccessResponse(new PagedResult<AuditLogDto>
            {
                Items = items.Select(l => new AuditLogDto
                {
                    LogId = l.LogId,
                    UserId = l.UserId,
                    Action = l.Action,
                    EntityType = l.EntityType,
                    EntityId = l.EntityId,
                    OldValueJson = l.OldValueJson,
                    NewValueJson = l.NewValueJson,
                    IpAddress = l.IpAddress,
                    CreatedAt = l.CreatedAt
                }).ToList(),
                Total = total,
                Page = page,
                PageSize = pageSize
            });
        }

        private Task AuditAsync(int adminUserId, string action, int targetUserId, object? oldValue, object? newValue, string? ip)
            => _auditRepo.AddAsync(new AuditLog
            {
                UserId = adminUserId,
                Action = action,
                EntityType = "User",
                EntityId = targetUserId,
                OldValueJson = oldValue == null ? null : JsonSerializer.Serialize(oldValue),
                NewValueJson = newValue == null ? null : JsonSerializer.Serialize(newValue),
                IpAddress = ip,
                CreatedAt = DateTime.UtcNow
            });
    }
}
