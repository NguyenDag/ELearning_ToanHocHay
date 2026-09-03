using ELearning_ToanHocHay_Control.Models.DTOs;

namespace ELearning_ToanHocHay_Control.Services.Interfaces
{
    public interface IAuthService
    {
        Task<ApiResponse<LoginResponseDto>> LoginAsync(LoginRequestDto request, string? ip = null);
        Task<bool> ValidateTokenAsync(string token);
        Task<ApiResponse<bool>> LogoutAsync(int userId, string? refreshToken = null);
        Task<ApiResponse<bool>> RegisterAsync(RegisterRequestDto request);

        /// <summary>P1 — exchanges a valid refresh token for a new access + refresh pair (rotation).</summary>
        Task<ApiResponse<TokenPairDto>> RefreshTokenAsync(string refreshToken, string? ip = null);

        Task<ApiResponse<bool>> ChangePasswordAsync(int userId, ChangePasswordDto request);
        Task<ApiResponse<bool>> ConfirmEmailAsync(string token);
        Task<ApiResponse<bool>> ResendConfirmationEmailAsync(string email);

        // P1 — forgot / reset password
        Task<ApiResponse<bool>> ForgotPasswordAsync(string email);
        Task<ApiResponse<bool>> ResetPasswordAsync(string token, string newPassword);
    }
}
