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
        private const string AuthorizationHeader = "Authorization";
        private const string ApiKeyPrefix = "Apikey ";

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var logger = context.HttpContext.RequestServices
                .GetService<ILogger<SePayApiKeyAttribute>>();

            // 1. Kiểm tra header Authorization
            if (!context.HttpContext.Request.Headers.TryGetValue(AuthorizationHeader, out var authHeaderValue))
            {
                logger?.LogWarning("Authorization header is missing");

                context.Result = new UnauthorizedObjectResult(ApiResponse<object>.ErrorResponse(
                    "API Key is missing",
                    new List<string> { "Authorization header is required with format: 'Apikey YOUR_API_KEY'" }
                ));
                return;
            }

            var authValue = authHeaderValue.ToString();

            // 2. Kiểm tra format "Apikey {key}"
            if (!authValue.StartsWith(ApiKeyPrefix, StringComparison.OrdinalIgnoreCase))
            {
                logger?.LogWarning("Authorization header has invalid format: {AuthValue}", authValue);

                context.Result = new UnauthorizedObjectResult(ApiResponse<object>.ErrorResponse(
                    "Invalid Authorization format",
                    new List<string> { "Authorization header must be in format: 'Apikey YOUR_API_KEY'" }
                ));
                return;
            }

            // 3. Extract API Key (bỏ prefix "Apikey ")
            var apiKey = authValue.Substring(ApiKeyPrefix.Length).Trim();

            if (string.IsNullOrEmpty(apiKey))
            {
                logger?.LogWarning("API Key is empty after removing prefix");

                context.Result = new UnauthorizedObjectResult(ApiResponse<object>.ErrorResponse(
                    "API Key is empty",
                    new List<string> { "API Key value is missing in Authorization header" }
                ));
                return;
            }

            logger?.LogInformation("API Key extracted: {ApiKey}", apiKey);

            // 4. Lấy SePayService từ DI container
            var sePayService = context.HttpContext.RequestServices
                .GetService<ISePayService>();

            if (sePayService == null)
            {
                logger?.LogError("SePayService is not registered in DI container");

                context.Result = new ObjectResult(ApiResponse<object>.ErrorResponse(
                    "Service configuration error",
                    new List<string> { "SePayService is not registered" }
                ))
                {
                    StatusCode = 500
                };
                return;
            }

            // 5. Validate API Key
            if (!sePayService.ValidateApiKey(apiKey))
            {
                logger?.LogWarning("Invalid API Key provided: {ApiKey}", apiKey);

                context.Result = new UnauthorizedObjectResult(ApiResponse<object>.ErrorResponse(
                    "Invalid API Key",
                    new List<string> { "The provided API Key is not valid" }
                ));
                return;
            }

            logger?.LogInformation("✅ API Key validated successfully");
            // API Key hợp lệ - cho phép tiếp tục
        }
    }
}
