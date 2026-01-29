using ELearning_ToanHocHay_Control.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ELearning_ToanHocHay_Control.Attributes
{
    /// <summary>
    /// Attribute để phân quyền theo UserType
    /// </summary>
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
            // Kiểm tra user đã authenticated chưa
            var user = context.HttpContext.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                context.Result = new UnauthorizedObjectResult(new
                {
                    success = false,
                    message = "Bạn cần đăng nhập để truy cập tài nguyên này"
                });
                return;
            }

            // Get UserType from claims
            var userTypeClaim = user.FindFirst("UserType")?.Value;
            if (string.IsNullOrEmpty(userTypeClaim))
            {
                context.Result = new ForbidResult();
                return;
            }

            // Parse UserType
            if (!Enum.TryParse<UserType>(userTypeClaim, out var userType))
            {
                context.Result = new ForbidResult();
                return;
            }

            // Kiểm tra quyền
            if (!_allowedUserTypes.Contains(userType))
            {
                context.Result = new ObjectResult(new
                {
                    success = false,
                    message = "Bạn không có quyền truy cập tài nguyên này"
                })
                {
                    StatusCode = 403
                };
            }
        }
    }
}
