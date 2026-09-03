using ELearning_ToanHocHay_Control.Models.DTOs;

namespace ELearning_ToanHocHay_Control.Services.Interfaces
{
    public class SystemConfigDto
    {
        public string ConfigKey { get; set; } = "";
        public string? ConfigValue { get; set; }
        public string? ConfigGroup { get; set; }
        public string? Description { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>P6/P7 — typed reads of the SystemConfig table + admin edit.</summary>
    public interface ISystemConfigService
    {
        Task<int> GetIntAsync(string key, int fallback);
        Task<decimal> GetDecimalAsync(string key, decimal fallback);
        Task<bool> GetBoolAsync(string key, bool fallback);
        Task<string> GetStringAsync(string key, string fallback);

        Task<ApiResponse<List<SystemConfigDto>>> GetAllAsync(string? group);
        Task<ApiResponse<SystemConfigDto>> SetAsync(string key, string? value, int updatedBy);
    }
}
