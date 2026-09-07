using System.Text.Json;
using ELearning_ToanHocHay_Control.Common;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.AspNetCore.Http;

namespace ELearning_ToanHocHay_Control.Services.Implementations
{
    /// <summary>
    /// Mặc định: ghi audit qua <see cref="IAuditLogRepository.AddAsync"/> (tự SaveChanges).
    /// Vì vậy hãy gọi <see cref="WriteAsync"/> SAU khi thao tác chính đã lưu thành công.
    /// </summary>
    public sealed class AuditWriter : IAuditWriter
    {
        private readonly IAuditLogRepository _repo;
        private readonly IHttpContextAccessor _http;
        private readonly ILogger<AuditWriter> _logger;

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        public AuditWriter(IAuditLogRepository repo, IHttpContextAccessor http, ILogger<AuditWriter> logger)
        {
            _repo = repo;
            _http = http;
            _logger = logger;
        }

        public async Task WriteAsync(
            string action,
            string entityType,
            int? entityId = null,
            object? oldValue = null,
            object? newValue = null,
            int? actorUserId = null,
            string? ip = null,
            CancellationToken ct = default)
        {
            try
            {
                var actor = actorUserId ?? _http.HttpContext?.User.GetUserId();
                var addr = ip ?? _http.HttpContext?.Connection.RemoteIpAddress?.ToString();

                await _repo.AddAsync(new AuditLog
                {
                    UserId = actor,
                    Action = action,
                    EntityType = entityType,
                    EntityId = entityId,
                    OldValueJson = oldValue is null ? null : JsonSerializer.Serialize(oldValue, JsonOpts),
                    NewValueJson = newValue is null ? null : JsonSerializer.Serialize(newValue, JsonOpts),
                    IpAddress = addr,
                    CreatedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "AuditWriter: không ghi được audit {Action} {EntityType}#{EntityId}",
                    action, entityType, entityId);
            }
        }
    }
}
