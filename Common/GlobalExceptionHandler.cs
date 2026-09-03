using ELearning_ToanHocHay_Control.Models.DTOs;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ELearning_ToanHocHay_Control.Common
{
    /// <summary>
    /// P7 (A2-15) — last-resort handler for unhandled exceptions. Logs the detail
    /// server-side with the correlation id; returns a generic ProblemDetails body
    /// (also wrapped as an <see cref="ApiResponse{T}"/> so existing clients keep working).
    /// </summary>
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            var correlationId = httpContext.GetCorrelationId();

            _logger.LogError(exception,
                "Unhandled exception on {Method} {Path} (correlationId {CorrelationId})",
                httpContext.Request.Method, httpContext.Request.Path, correlationId);

            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Đã xảy ra lỗi máy chủ",
                Detail = "Yêu cầu không thể hoàn tất. Vui lòng thử lại sau.",
                Instance = httpContext.Request.Path
            };
            if (correlationId != null) problem.Extensions["correlationId"] = correlationId;

            await httpContext.Response.WriteAsJsonAsync(new ApiResponse<object>
            {
                Success = false,
                Message = problem.Title!,
                Errors = correlationId != null ? new List<string> { $"correlationId: {correlationId}" } : new()
            }, cancellationToken);

            return true;
        }
    }
}
