using ToanHocHay.WebApp.Models.Dtos;
using ToanHocHay.WebApp.Services.Interfaces;

namespace ToanHocHay.WebApp.Services.Implementations
{
    public class AuthApiService : BaseApiService, IAuthApiService
    {
        public AuthApiService(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContext) : base(httpClientFactory, httpContext)
        {
        }

        public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto dto)
        {
            var client = CreateClient();

            var response = await client.PostAsJsonAsync("auth/login", dto);
            if (!response.IsSuccessStatusCode)
                return null;

            var apiResponse = await response.Content
                .ReadFromJsonAsync<ApiResponse<LoginResponseDto>>();

            return apiResponse?.Data;
        }

        public Task LogoutAsync()
        {
            _httpContext.HttpContext?.Session.Clear();
            return Task.CompletedTask;
        }
    }
}
