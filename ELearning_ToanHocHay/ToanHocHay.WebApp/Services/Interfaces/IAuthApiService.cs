using ToanHocHay.WebApp.Models.Dtos;

namespace ToanHocHay.WebApp.Services.Interfaces
{
    public interface IAuthApiService
    {
        Task<LoginResponseDto?> LoginAsync(LoginRequestDto dto);
        Task LogoutAsync();

    }
}
