using System.Globalization;
using ELearning_ToanHocHay_Control.Data;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ELearning_ToanHocHay_Control.Services.Implementations
{
    public class SystemConfigService : ISystemConfigService
    {
        private readonly AppDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly IAuditWriter _audit;
        private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

        public SystemConfigService(AppDbContext context, IMemoryCache cache, IAuditWriter audit)
        {
            _context = context;
            _cache = cache;
            _audit = audit;
        }

        private async Task<string?> RawAsync(string key)
        {
            return await _cache.GetOrCreateAsync($"cfg:{key}", async e =>
            {
                e.AbsoluteExpirationRelativeToNow = CacheTtl;
                return await _context.SystemConfigs.AsNoTracking()
                    .Where(c => c.ConfigKey == key)
                    .Select(c => c.ConfigValue)
                    .FirstOrDefaultAsync();
            });
        }

        public async Task<int> GetIntAsync(string key, int fallback)
            => int.TryParse(await RawAsync(key), out var v) ? v : fallback;

        public async Task<decimal> GetDecimalAsync(string key, decimal fallback)
            => decimal.TryParse(await RawAsync(key), NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : fallback;

        public async Task<bool> GetBoolAsync(string key, bool fallback)
            => bool.TryParse(await RawAsync(key), out var v) ? v : fallback;

        public async Task<string> GetStringAsync(string key, string fallback)
            => await RawAsync(key) ?? fallback;

        public async Task<ApiResponse<List<SystemConfigDto>>> GetAllAsync(string? group)
        {
            var q = _context.SystemConfigs.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(group)) q = q.Where(c => c.ConfigGroup == group);

            var items = await q.OrderBy(c => c.ConfigGroup).ThenBy(c => c.ConfigKey)
                .Select(c => new SystemConfigDto
                {
                    ConfigKey = c.ConfigKey,
                    ConfigValue = c.ConfigValue,
                    ConfigType = c.ConfigType.ToString(),
                    ConfigGroup = c.ConfigGroup,
                    Description = c.Description,
                    UpdatedAt = c.UpdatedAt,
                    UpdatedByName = c.UpdatedByUser != null ? c.UpdatedByUser.FullName : null
                })
                .ToListAsync();
            return ApiResponse<List<SystemConfigDto>>.SuccessResponse(items);
        }

        private static bool ValidValueForType(ConfigValueType type, string? value)
        {
            if (string.IsNullOrEmpty(value)) return true; // cho phép rỗng
            return type switch
            {
                ConfigValueType.Int => int.TryParse(value, out _),
                ConfigValueType.Decimal => decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out _),
                ConfigValueType.Bool => bool.TryParse(value, out _),
                _ => true
            };
        }

        public async Task<ApiResponse<SystemConfigDto>> SetAsync(string key, string? value, int updatedBy)
        {
            var row = await _context.SystemConfigs.FirstOrDefaultAsync(c => c.ConfigKey == key);
            if (row == null)
                return ApiResponse<SystemConfigDto>.NotFound($"Không có khoá cấu hình '{key}'");

            if (!ValidValueForType(row.ConfigType, value))
                return ApiResponse<SystemConfigDto>.ErrorResponse(
                    $"Giá trị không hợp lệ cho kiểu {row.ConfigType}");

            var oldValue = row.ConfigValue;
            row.ConfigValue = value;
            row.UpdatedAt = DateTime.UtcNow;
            row.UpdatedBy = updatedBy;
            await _context.SaveChangesAsync();
            _cache.Remove($"cfg:{key}");

            await _audit.WriteAsync("UpdateConfig", "SystemConfig", row.ConfigId,
                oldValue: new { key, value = oldValue },
                newValue: new { key, value },
                actorUserId: updatedBy);

            return ApiResponse<SystemConfigDto>.SuccessResponse(new SystemConfigDto
            {
                ConfigKey = row.ConfigKey,
                ConfigValue = row.ConfigValue,
                ConfigType = row.ConfigType.ToString(),
                ConfigGroup = row.ConfigGroup,
                Description = row.Description,
                UpdatedAt = row.UpdatedAt
            }, "Đã cập nhật");
        }
    }
}
