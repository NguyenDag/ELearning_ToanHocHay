using ELearning_ToanHocHay_Control.Data.Entities;

namespace ELearning_ToanHocHay_Control.Repositories.Interfaces
{
    public interface IAuditLogRepository
    {
        Task AddAsync(AuditLog log);
        Task<(List<AuditLog> Items, int Total)> QueryAsync(
            string? entityType, int? entityId, int? userId, int page, int pageSize);
    }
}
