using System.Security.Claims;

namespace ELearning_ToanHocHay_Control.Services.Helpers
{
    public static class ClaimsPrincipalExtensions
    {
        public static int GetUserId(this ClaimsPrincipal user)
        {
            var userIdClaim =
                user.FindFirst(ClaimTypes.NameIdentifier) ??
                user.FindFirst("userId");

            if (userIdClaim == null)
                throw new UnauthorizedAccessException("UserId not found in token");

            return int.Parse(userIdClaim.Value);
        }
    }
}
