using System.Security.Claims;
using ELearning_ToanHocHay_Control.Data.Entities;

namespace ELearning_ToanHocHay_Control.Services.Interfaces
{
    public interface IJwtService
    {
        string GenerateToken(User user, int? studentId, int? parentId);
        ClaimsPrincipal ValidateToken(string token);
        int? GetUserIdFromToken(string token);
    }
}
