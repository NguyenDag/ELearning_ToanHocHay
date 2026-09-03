using ELearning_ToanHocHay_Control.Common;
using ELearning_ToanHocHay_Control.Data;
using ELearning_ToanHocHay_Control.Data.Entities;

namespace ELearning_ToanHocHay_Control.Services.Helpers
{
    /// <summary>
    /// Ghi một dòng <see cref="RefundEvent"/> cho mỗi chuyển trạng thái (timeline truy vết)
    /// và đồng thời log structured qua Serilog. Actor / IP / correlation-id lấy từ HTTP context.
    /// Chỉ thêm entity vào change tracker — caller chịu trách nhiệm SaveChanges.
    /// </summary>
    public interface IRefundEventWriter
    {
        void Add(
            AppDbContext ctx,
            RefundEventType type,
            int? refundRequestId = null,
            int? refundBatchId = null,
            string? fromStatus = null,
            string? toStatus = null,
            decimal? amount = null,
            string? note = null);
    }

    public class RefundEventWriter : IRefundEventWriter
    {
        private readonly IHttpContextAccessor _http;
        private readonly ILogger<RefundEventWriter> _logger;

        public RefundEventWriter(IHttpContextAccessor http, ILogger<RefundEventWriter> logger)
        {
            _http = http;
            _logger = logger;
        }

        public void Add(
            AppDbContext ctx,
            RefundEventType type,
            int? refundRequestId = null,
            int? refundBatchId = null,
            string? fromStatus = null,
            string? toStatus = null,
            decimal? amount = null,
            string? note = null)
        {
            var httpCtx = _http.HttpContext;
            var actorId = httpCtx?.User.GetUserId();
            var actorType = httpCtx?.User.GetUserType()?.ToString();
            var ip = httpCtx?.Connection.RemoteIpAddress?.ToString();
            var correlationId = httpCtx?.GetCorrelationId();

            ctx.Set<RefundEvent>().Add(new RefundEvent
            {
                RefundRequestId = refundRequestId,
                RefundBatchId = refundBatchId,
                EventType = type,
                FromStatus = fromStatus,
                ToStatus = toStatus,
                ActorUserId = actorId,
                ActorUserType = actorType,
                IpAddress = ip,
                CorrelationId = correlationId,
                AmountSnapshot = amount,
                Note = note,
                CreatedAt = DateTime.UtcNow
            });

            _logger.LogInformation(
                "Refund event {EventType} req={RefundRequestId} batch={RefundBatchId} {From}->{To} " +
                "amount={Amount} actor={ActorId} actorType={ActorType} corr={CorrelationId} note={Note}",
                type, refundRequestId, refundBatchId, fromStatus, toStatus, amount,
                actorId, actorType, correlationId, note);
        }
    }
}
