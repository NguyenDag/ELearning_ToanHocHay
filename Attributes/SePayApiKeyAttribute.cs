using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Net.Http.Headers;

namespace ELearning_ToanHocHay_Control.Attributes
{
    /// <summary>
    /// Attribute để xác thực API Key từ SePay IPN
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class SePayApiKeyAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string HeaderName = "X-Api-Key";

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            // 1. Kiểm tra header có chứa API Key không
            if (!context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var apiKey))
            {
                context.Result = new UnauthorizedObjectResult(ApiResponse<object>.ErrorResponse(
                    "API Key is missing",
                    new List<string> { $"{HeaderName} header is required" }
                ));
                return;
            }

            // 2. Lấy SePayService từ DI container
            var sePayService = context.HttpContext.RequestServices
                .GetService<ISePayService>();

            if (sePayService == null)
            {
                context.Result = new ObjectResult(ApiResponse<object>.ErrorResponse(
                    "Service configuration error",
                    new List<string> { "SePayService is not registered" }
                ))
                {
                    StatusCode = 500
                };
                return;
            }

            // 3. Validate API Key
            if (!sePayService.ValidateApiKey(apiKey))
            {
                context.Result = new UnauthorizedObjectResult(ApiResponse<object>.ErrorResponse(
                    "Invalid API Key",
                    new List<string> { "The provided API Key is not valid" }
                ));
                return;
            }

            // API Key hợp lệ - cho phép tiếp tục
        }
    }
}
