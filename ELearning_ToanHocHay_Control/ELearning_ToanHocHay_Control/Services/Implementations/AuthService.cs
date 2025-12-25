using System;
using ELearning_ToanHocHay_Control.Data;
using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ELearning_ToanHocHay_Control.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IJwtService _jwtService;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IConfiguration _configuration;

        public AuthService(
            IUserRepository userRepository,
            IJwtService jwtService,
            IPasswordHasher passwordHasher,
            IConfiguration configuration)
        {
            _userRepository = userRepository;
            _jwtService = jwtService;
            _passwordHasher = passwordHasher;
            _configuration = configuration;
        }

        public async Task<ApiResponse<LoginResponseDto>> LoginAsync(LoginRequestDto request)
        {
            try
            {
                var user = await _userRepository.GetByEmailAsync(request.Email);
                // Check user exist or not
                if (user == null)
                {
                    return ApiResponse<LoginResponseDto>.ErrorResponse("Email hoặc mật khẩu không đúng", new List<string> { "Thông tin đăng nhập không hợp lệ" });
                }

                // Check account active or not
                if (!user.IsActive)
                {
                    return ApiResponse<LoginResponseDto>.ErrorResponse(
                        "Tài khoản đã bị vô hiệu hóa",
                        new List<string> { "Vui lòng liên hệ quản trị viên" }
                    );
                }

                // Verify password
                if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
                {
                    return ApiResponse<LoginResponseDto>.ErrorResponse(
                        "Email hoặc mật khẩu không đúng",
                        new List<string> { "Thông tin đăng nhập không hợp lệ" }
                    );
                }

                // Generate JWT token
                var token = _jwtService.GenerateToken(user);
                var expirationMinutes = int.Parse(_configuration["JwtSettings:ExpirationMinutes"]);

                // Update last login
                await _userRepository.UpdateLastLoginAsync(user.UserId);

                // Tạo response
                var response = new LoginResponseDto
                {
                    UserId = user.UserId,
                    Email = user.Email,
                    FullName = user.FullName,
                    UserType = user.UserType.ToString(),
                    Token = token,
                    TokenExpiration = DateTime.UtcNow.AddMinutes(expirationMinutes),
                    AvatarUrl = user.AvatarUrl
                };

                return ApiResponse<LoginResponseDto>.SuccessResponse(response, "Đăng nhập thành công");

            }
            catch (Exception ex)
            {
                return ApiResponse<LoginResponseDto>.ErrorResponse("Đã xảy ra lỗi trong quá trình đăng nhập", new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<bool>> LogoutAsync(int userId)
        {
            try
            {
                // Có thể thêm logic blacklist token hoặc invalidate token ở đây
                // Hiện tại chỉ return success
                return ApiResponse<bool>.SuccessResponse(true, "Đăng xuất thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.ErrorResponse(
                    "Đã xảy ra lỗi khi đăng xuất",
                    new List<string> { ex.Message }
                );
            }
        }

        public async Task<bool> ValidateTokenAsync(string token)
        {
            try
            {
                var principal = _jwtService.ValidateToken(token);
                if (principal == null)
                    return false;

                var userId = _jwtService.GetUserIdFromToken(token);
                if (!userId.HasValue)
                    return false;

                var user = await _userRepository.GetByIdAsync(userId.Value);
                return user != null && user.IsActive;
            }
            catch
            {
                return false;
            }
        }
    }
}
