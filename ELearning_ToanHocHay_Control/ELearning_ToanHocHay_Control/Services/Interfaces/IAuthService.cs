using ELearning_ToanHocHay_Control.Models.DTOs;

namespace ELearning_ToanHocHay_Control.Services.Interfaces
{
    public interface IAuthService
    {
        Task<ApiResponse<LoginResponseDto>> LoginAsync(LoginRequestDto request);
        Task<bool> ValidateTokenAsync(string token);
        Task<ApiResponse<bool>> LogoutAsync(int userId);
    }
}
