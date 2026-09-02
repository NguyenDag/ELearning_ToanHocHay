using System.Security.Claims;
using ELearning_ToanHocHay_Control.Data.Entities;

namespace ELearning_ToanHocHay_Control.Common
{
    /// <summary>
    /// Consistent access to JWT claims. Tokens are issued in
    /// <see cref="Services.Implementations.JwtService.GenerateToken"/>.
    /// </summary>
    public static class ClaimsPrincipalExtensions
    {
        public static int? GetUserId(this ClaimsPrincipal? user)
        {
            if (user == null) return null;

            // Prefer the custom claim, fall back to "sub" (mapped to NameIdentifier).
            var raw = user.FindFirst(CustomJwtClaims.UserId)?.Value
                      ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return int.TryParse(raw, out var id) ? id : null;
        }

        public static int? GetStudentId(this ClaimsPrincipal? user)
        {
            var raw = user?.FindFirst(CustomJwtClaims.StudentId)?.Value;
            return int.TryParse(raw, out var id) ? id : null;
        }

        public static int? GetParentId(this ClaimsPrincipal? user)
        {
            var raw = user?.FindFirst(CustomJwtClaims.ParentId)?.Value;
            return int.TryParse(raw, out var id) ? id : null;
        }

        public static UserType? GetUserType(this ClaimsPrincipal? user)
        {
            var raw = user?.FindFirst(CustomJwtClaims.UserType)?.Value;
            return Enum.TryParse<UserType>(raw, out var t) ? t : null;
        }

        public static string? GetEmail(this ClaimsPrincipal? user)
            => user?.FindFirst(ClaimTypes.Email)?.Value
               ?? user?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email)?.Value;

        public static bool HasUserType(this ClaimsPrincipal? user, params UserType[] allowed)
        {
            var t = user.GetUserType();
            return t.HasValue && allowed.Contains(t.Value);
        }

        public static bool IsSystemAdmin(this ClaimsPrincipal? user)
            => user.GetUserType() == UserType.SystemAdmin;
    }
}
