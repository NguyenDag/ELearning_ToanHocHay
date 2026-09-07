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
        private readonly IPasswordHasher _passwordHasher;
        private readonly Microsoft.Extensions.Caching.Memory.IMemoryCache _cache;
        private readonly IMapper _mapper;

        public AdminUserService(
            IUserRepository userRepo,
            IAuditLogRepository auditRepo,
            IRefreshTokenRepository refreshRepo,
            IPasswordHasher passwordHasher,
            Microsoft.Extensions.Caching.Memory.IMemoryCache cache,
            IMapper mapper)
        {
            _userRepo = userRepo;
            _auditRepo = auditRepo;
            _refreshRepo = refreshRepo;
            _passwordHasher = passwordHasher;
            _cache = cache;
            _mapper = mapper;
        }

        private void InvalidateSessions(int userId)
            => _cache.Remove($"sstamp:{userId}");

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
            user.SecurityStamp = Guid.NewGuid().ToString("N");
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepo.UpdateUserAsync(user);

            await _refreshRepo.RevokeAllForUserAsync(targetUserId);
            InvalidateSessions(targetUserId);
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
            user.SecurityStamp = Guid.NewGuid().ToString("N");
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepo.UpdateUserAsync(user);

            // Force re-login so the new role lands in a fresh token.
            await _refreshRepo.RevokeAllForUserAsync(targetUserId);
            InvalidateSessions(targetUserId);
            await AuditAsync(adminUserId, "ChangeRole", targetUserId,
                new { role = oldRole.ToString() }, new { role = newRole.ToString() }, ip);

            return ApiResponse<UserDto>.SuccessResponse(_mapper.Map<UserDto>(user), $"Role changed to {newRole}");
        }

        public async Task<ApiResponse<bool>> ResetPasswordAsync(int targetUserId, int adminUserId, string newPassword, string? ip)
        {
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
                return ApiResponse<bool>.ErrorResponse("Mật khẩu mới phải có ít nhất 6 ký tự");

            var user = await _userRepo.GetByIdAsync(targetUserId);
            if (user == null) return ApiResponse<bool>.NotFound("Không tìm thấy người dùng");

            user.PasswordHash = _passwordHasher.HashPassword(newPassword);
            user.SecurityStamp = Guid.NewGuid().ToString("N");
            user.FailedLoginCount = 0;
            user.LockoutEndsAt = null;
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepo.UpdateUserAsync(user);

            // Buộc đăng nhập lại ở mọi thiết bị.
            await _refreshRepo.RevokeAllForUserAsync(targetUserId);
            InvalidateSessions(targetUserId);
            await AuditAsync(adminUserId, "AdminResetPassword", targetUserId, null, null, ip);

            return ApiResponse<bool>.SuccessResponse(true, "Đã đặt lại mật khẩu");
        }

        public async Task<ApiResponse<UserDto>> SetEmailConfirmedAsync(int targetUserId, int adminUserId, string? ip)
        {
            var user = await _userRepo.GetByIdAsync(targetUserId);
            if (user == null) return ApiResponse<UserDto>.NotFound("Không tìm thấy người dùng");
            if (user.IsEmailConfirmed) return ApiResponse<UserDto>.ErrorResponse("Email đã được xác nhận");

            user.IsEmailConfirmed = true;
            user.EmailConfirmedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepo.UpdateUserAsync(user);

            await AuditAsync(adminUserId, "AdminConfirmEmail", targetUserId, null, null, ip);
            return ApiResponse<UserDto>.SuccessResponse(_mapper.Map<UserDto>(user), "Đã xác nhận email");
        }

        public async Task<ApiResponse<UserDto>> SetActiveAsync(int targetUserId, bool active, int adminUserId, string? ip)
        {
            var user = await _userRepo.GetByIdAsync(targetUserId);
            if (user == null) return ApiResponse<UserDto>.NotFound("Không tìm thấy người dùng");
            if (user.UserId == adminUserId) return ApiResponse<UserDto>.ErrorResponse("Không thể tự vô hiệu hoá tài khoản của mình");
            if (user.LockedAt.HasValue) return ApiResponse<UserDto>.ErrorResponse("Tài khoản đang bị khoá — hãy mở khoá thay vì bật/tắt hoạt động");
            if (user.IsActive == active) return ApiResponse<UserDto>.ErrorResponse(active ? "Tài khoản đã đang hoạt động" : "Tài khoản đã bị vô hiệu hoá");

            user.IsActive = active;
            user.UpdatedAt = DateTime.UtcNow;
            if (!active)
            {
                user.SecurityStamp = Guid.NewGuid().ToString("N");
            }
            await _userRepo.UpdateUserAsync(user);

            if (!active)
            {
                await _refreshRepo.RevokeAllForUserAsync(targetUserId);
                InvalidateSessions(targetUserId);
            }
            await AuditAsync(adminUserId, active ? "ActivateUser" : "DeactivateUser", targetUserId, null, null, ip);
            return ApiResponse<UserDto>.SuccessResponse(_mapper.Map<UserDto>(user), active ? "Đã kích hoạt tài khoản" : "Đã vô hiệu hoá tài khoản");
        }

        public async Task<ApiResponse<PagedResult<AuditLogDto>>> GetAuditLogsAsync(AuditLogFilter filter)
        {
            var (items, total) = await _auditRepo.SearchAsync(filter);
            return ApiResponse<PagedResult<AuditLogDto>>.SuccessResponse(new PagedResult<AuditLogDto>
            {
                Items = items,
                Total = total,
                Page = Math.Max(1, filter.Page),
                PageSize = Math.Clamp(filter.PageSize, 1, 200)
            });
        }

        public async Task<ApiResponse<AuditLogFacetsDto>> GetAuditFacetsAsync()
        {
            var (entityTypes, actions) = await _auditRepo.GetFacetsAsync();
            return ApiResponse<AuditLogFacetsDto>.SuccessResponse(new AuditLogFacetsDto
            {
                EntityTypes = entityTypes,
                Actions = actions
            });
        }

        public async Task<byte[]> ExportAuditLogsCsvAsync(AuditLogFilter filter)
        {
            // Xuất tối đa 5000 dòng khớp bộ lọc.
            filter.Page = 1;
            filter.PageSize = 5000;
            var (items, _) = await _auditRepo.SearchAsync(filter);
            return Services.Helpers.AuditLogCsvWriter.Build(items);
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
