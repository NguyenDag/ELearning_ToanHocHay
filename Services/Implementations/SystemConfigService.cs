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
        private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

        public SystemConfigService(AppDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
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
                    ConfigGroup = c.ConfigGroup,
                    Description = c.Description,
                    UpdatedAt = c.UpdatedAt
                })
                .ToListAsync();
            return ApiResponse<List<SystemConfigDto>>.SuccessResponse(items);
        }

        public async Task<ApiResponse<SystemConfigDto>> SetAsync(string key, string? value, int updatedBy)
        {
            var row = await _context.SystemConfigs.FirstOrDefaultAsync(c => c.ConfigKey == key);
            if (row == null)
                return ApiResponse<SystemConfigDto>.ErrorResponse($"Unknown config key '{key}'");

            row.ConfigValue = value;
            row.UpdatedAt = DateTime.UtcNow;
            row.UpdatedBy = updatedBy;
            await _context.SaveChangesAsync();
            _cache.Remove($"cfg:{key}");

            return ApiResponse<SystemConfigDto>.SuccessResponse(new SystemConfigDto
            {
                ConfigKey = row.ConfigKey,
                ConfigValue = row.ConfigValue,
                ConfigGroup = row.ConfigGroup,
                Description = row.Description,
                UpdatedAt = row.UpdatedAt
            }, "Đã cập nhật");
        }
    }
}
