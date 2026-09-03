using ELearning_ToanHocHay_Control.Data;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ELearning_ToanHocHay_Control.Repositories.Implementations
{
    public class AuditLogRepository : IAuditLogRepository
    {
        private readonly AppDbContext _context;

        public AuditLogRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(AuditLog log)
        {
            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        public async Task<(List<AuditLog> Items, int Total)> QueryAsync(
            string? entityType, int? entityId, int? userId, int page, int pageSize)
        {
            var q = _context.AuditLogs.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(entityType)) q = q.Where(l => l.EntityType == entityType);
            if (entityId.HasValue) q = q.Where(l => l.EntityId == entityId);
            if (userId.HasValue) q = q.Where(l => l.UserId == userId);

            var total = await q.CountAsync();
            var items = await q
                .OrderByDescending(l => l.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return (items, total);
        }
    }
}
