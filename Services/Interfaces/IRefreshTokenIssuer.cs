using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs;

namespace ELearning_ToanHocHay_Control.Services.Interfaces
{
    /// <summary>
    /// P1 — phát cặp access + refresh token (ghi <c>RefreshToken</c> vào kho). Tách khỏi
    /// <see cref="Implementations.AuthService"/> để tách việc phát token khỏi logic đăng nhập / làm mới.
    /// </summary>
    public interface IRefreshTokenIssuer
    {
        Task<TokenPairDto> IssueAsync(User user, int? studentId, int? parentId, string? ip);
    }
}
