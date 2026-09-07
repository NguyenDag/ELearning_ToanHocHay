using ELearning_ToanHocHay_Control.Data;
using ELearning_ToanHocHay_Control.Models.DTOs;
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

        public async Task AddAsync(Data.Entities.AuditLog log)
        {
            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        public async Task<(List<AuditLogDto> Items, int Total)> SearchAsync(AuditLogFilter f)
        {
            var page = Math.Max(1, f.Page);
            var pageSize = Math.Clamp(f.PageSize, 1, 200);

            var q = _context.AuditLogs.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(f.EntityType)) q = q.Where(l => l.EntityType == f.EntityType);
            if (f.EntityId.HasValue) q = q.Where(l => l.EntityId == f.EntityId);
            if (f.UserId.HasValue) q = q.Where(l => l.UserId == f.UserId);
            if (!string.IsNullOrWhiteSpace(f.Action)) q = q.Where(l => l.Action == f.Action);
            if (f.FromUtc.HasValue) q = q.Where(l => l.CreatedAt >= f.FromUtc.Value);
            if (f.ToUtc.HasValue) q = q.Where(l => l.CreatedAt <= f.ToUtc.Value);
            if (!string.IsNullOrWhiteSpace(f.Q))
            {
                var s = f.Q.Trim().ToLower();
                q = q.Where(l => l.EntityType.ToLower().Contains(s)
                                 || l.Action.ToLower().Contains(s)
                                 || (l.User != null && (l.User.FullName.ToLower().Contains(s)
                                                        || l.User.Email.ToLower().Contains(s))));
            }

            var total = await q.CountAsync();
            var items = await q
                .OrderByDescending(l => l.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(l => new AuditLogDto
                {
                    LogId = l.LogId,
                    UserId = l.UserId,
                    ActorName = l.User != null ? l.User.FullName : null,
                    ActorEmail = l.User != null ? l.User.Email : null,
                    Action = l.Action,
                    EntityType = l.EntityType,
                    EntityId = l.EntityId,
                    OldValueJson = l.OldValueJson,
                    NewValueJson = l.NewValueJson,
                    IpAddress = l.IpAddress,
                    CreatedAt = l.CreatedAt
                })
                .ToListAsync();

            return (items, total);
        }

        public async Task<(List<string> EntityTypes, List<string> Actions)> GetFacetsAsync()
        {
            var entityTypes = await _context.AuditLogs.AsNoTracking()
                .Select(l => l.EntityType).Distinct().OrderBy(x => x).ToListAsync();
            var actions = await _context.AuditLogs.AsNoTracking()
                .Select(l => l.Action).Distinct().OrderBy(x => x).ToListAsync();
            return (entityTypes, actions);
        }
    }
}
