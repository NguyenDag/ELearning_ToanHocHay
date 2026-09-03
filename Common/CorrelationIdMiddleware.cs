using Serilog.Context;

namespace ELearning_ToanHocHay_Control.Common
{
    /// <summary>
    /// P7 — attaches a correlation id to every request: read from the inbound
    /// <c>X-Correlation-ID</c> header or generated, echoed on the response, and
    /// pushed into the Serilog <see cref="LogContext"/> so every log line carries it.
    /// </summary>
    public class CorrelationIdMiddleware
    {
        public const string HeaderName = "X-Correlation-ID";
        private readonly RequestDelegate _next;

        public CorrelationIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var incoming)
                                && !string.IsNullOrWhiteSpace(incoming)
                ? incoming.ToString()
                : Guid.NewGuid().ToString("N");

            context.Items[HeaderName] = correlationId;
            context.Response.Headers[HeaderName] = correlationId;

            using (LogContext.PushProperty("CorrelationId", correlationId))
            {
                await _next(context);
            }
        }
    }

    public static class CorrelationIdExtensions
    {
        public static string? GetCorrelationId(this HttpContext context)
            => context.Items.TryGetValue(CorrelationIdMiddleware.HeaderName, out var v) ? v as string : null;
    }
}
