using ELearning_ToanHocHay_Control.Common;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ELearning_ToanHocHay_Control.Attributes
{
    /// <summary>Phân quyền theo <see cref="UserType"/>. Trả 401 / 403 với vỏ <see cref="ApiResponse{T}"/> (A5).</summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AuthorizeUserTypeAttribute : Attribute, IAuthorizationFilter
    {
        private readonly UserType[] _allowedUserTypes;

        public AuthorizeUserTypeAttribute(params UserType[] allowedUserTypes)
        {
            _allowedUserTypes = allowedUserTypes;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                context.Result = new ObjectResult(
                    ApiResponse<object>.ErrorResponse("Bạn cần đăng nhập để truy cập tài nguyên này"))
                { StatusCode = StatusCodes.Status401Unauthorized };
                return;
            }

            var userTypeClaim = user.FindFirst(CustomJwtClaims.UserType)?.Value;
            if (string.IsNullOrEmpty(userTypeClaim)
                || !Enum.TryParse<UserType>(userTypeClaim, out var userType)
                || !_allowedUserTypes.Contains(userType))
            {
                context.Result = new ObjectResult(
                    ApiResponse<object>.Forbidden("Bạn không có quyền truy cập tài nguyên này"))
                { StatusCode = StatusCodes.Status403Forbidden };
            }
        }
    }
}
