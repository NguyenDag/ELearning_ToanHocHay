using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs;

namespace ELearning_ToanHocHay_Control.Repositories.Interfaces
{
    public interface IAuditLogRepository
    {
        Task AddAsync(AuditLog log);

        /// <summary>Nhật ký đã join tên/email người thực hiện, có lọc + phân trang.</summary>
        Task<(List<AuditLogDto> Items, int Total)> SearchAsync(AuditLogFilter filter);

        /// <summary>Danh sách EntityType + Action phân biệt (cho dropdown lọc).</summary>
        Task<(List<string> EntityTypes, List<string> Actions)> GetFacetsAsync();
    }
}
