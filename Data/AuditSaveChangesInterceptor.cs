using System.Text.Json;
using ELearning_ToanHocHay_Control.Common;
using ELearning_ToanHocHay_Control.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ELearning_ToanHocHay_Control.Data
{
    /// <summary>
    /// P7 — writes an <see cref="AuditLog"/> row whenever a sensitive field changes
    /// (User role / active / lock, Subscription / Payment status, Package price / active,
    /// Question review status). The acting user + IP come from the current HTTP context.
    /// </summary>
    public class AuditSaveChangesInterceptor : SaveChangesInterceptor
    {
        private readonly IHttpContextAccessor _http;

        // entity type -> the fields we care about
        private static readonly Dictionary<Type, string[]> Watched = new()
        {
            [typeof(User)] = new[] { nameof(User.UserType), nameof(User.IsActive), nameof(User.LockedAt) },
            [typeof(Subscription)] = new[] { nameof(Subscription.Status) },
            [typeof(Payment)] = new[] { nameof(Payment.Status) },
            [typeof(Package)] = new[] { nameof(Package.Price), nameof(Package.IsActive) },
            [typeof(Question)] = new[] { nameof(Question.Status) },
        };

        public AuditSaveChangesInterceptor(IHttpContextAccessor http)
        {
            _http = http;
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context is not null) AddAuditRows(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData, InterceptionResult<int> result)
        {
            if (eventData.Context is not null) AddAuditRows(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        private void AddAuditRows(DbContext context)
        {
            var actor = _http.HttpContext?.User.GetUserId();
            var ip = _http.HttpContext?.Connection.RemoteIpAddress?.ToString();

            var entries = context.ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Modified && Watched.ContainsKey(e.Entity.GetType()))
                .ToList();

            foreach (var entry in entries)
            {
                var fields = Watched[entry.Entity.GetType()];
                var oldValues = new Dictionary<string, object?>();
                var newValues = new Dictionary<string, object?>();

                foreach (var field in fields)
                {
                    var prop = entry.Property(field);
                    if (!prop.IsModified || Equals(prop.OriginalValue, prop.CurrentValue)) continue;
                    oldValues[field] = prop.OriginalValue;
                    newValues[field] = prop.CurrentValue;
                }

                if (newValues.Count == 0) continue;

                context.Set<AuditLog>().Add(new AuditLog
                {
                    UserId = actor,
                    Action = "Update",
                    EntityType = entry.Entity.GetType().Name,
                    EntityId = TryGetKey(entry),
                    OldValueJson = JsonSerializer.Serialize(oldValues),
                    NewValueJson = JsonSerializer.Serialize(newValues),
                    IpAddress = ip,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        private static int? TryGetKey(EntityEntry entry)
        {
            var key = entry.Metadata.FindPrimaryKey()?.Properties.FirstOrDefault();
            if (key == null) return null;
            var value = entry.Property(key.Name).CurrentValue;
            return value switch
            {
                int i => i,
                long l => (int)l,
                _ => null
            };
        }
    }
}
