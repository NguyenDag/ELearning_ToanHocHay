using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Services.Implementations;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ELearning_ToanHocHay_Control.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IJwtService _jwtService;

        public AuthController(IAuthService authService, IJwtService jwtService)
        {
            _authService = authService;
            _jwtService = jwtService;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(ApiResponse<LoginResponseDto>.ErrorResponse(
                    "Dữ liệu không hợp lệ",
                    errors
                ));
            }

            var result = await _authService.LoginAsync(request);

            if (!result.Success)
            {
                return Unauthorized(result);
            }

            return Ok(result);
        }
        /// <summary>
        /// Validate token
        /// </summary>
        [HttpPost("validate-token")]
        [AllowAnonymous]
        public async Task<IActionResult> ValidateToken([FromBody] string token)
        {
            var isValid = await _authService.ValidateTokenAsync(token);

            if (isValid)
            {
                return Ok(ApiResponse<bool>.SuccessResponse(true, "Token hợp lệ"));
            }

            return Unauthorized(ApiResponse<bool>.ErrorResponse("Token không hợp lệ hoặc đã hết hạn"));
        }

        /// <summary>
        /// Lấy thông tin user hiện tại từ token
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        public IActionResult GetCurrentUser()
        {
            var userId = GetUserIdFromToken();
            if (!userId.HasValue)
            {
                return Unauthorized(ApiResponse<object>.ErrorResponse("Token không hợp lệ"));
            }

            var user = new
            {
                UserId = userId.Value,
                Email = User.FindFirst("Email")?.Value,
                FullName = User.Identity?.Name,
                UserType = User.FindFirst("UserType")?.Value
            };

            return Ok(ApiResponse<object>.SuccessResponse(user, "Lấy thông tin thành công"));
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var userId = GetUserIdFromToken();
            if (!userId.HasValue)
            {
                return Unauthorized(ApiResponse<bool>.ErrorResponse("Token không hợp lệ"));
            }

            var result = await _authService.LogoutAsync(userId.Value);
            return Ok(result);
        }

        private int? GetUserIdFromToken()
        {
            var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            return _jwtService.GetUserIdFromToken(token);
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register(RegisterRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<bool>.ErrorResponse(
                    "Dữ liệu không hợp lệ",
                    ModelState.Values.SelectMany(v => v.Errors)
                                     .Select(e => e.ErrorMessage)
                                     .ToList()
                ));
            }

            var result = await _authService.RegisterAsync(request);
            return result.Success ? Ok(result) : BadRequest(result);
        }
        [HttpPost("refresh-token")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshToken(RefreshTokenRequestDto request)
        {
            var result = await _authService.RefreshTokenAsync(request.RefreshToken);
            return result.Success ? Ok(result) : Unauthorized(result);
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordDto request)
        {
            var userId = GetUserIdFromToken();
            if (!userId.HasValue)
                return Unauthorized();

            var result = await _authService.ChangePasswordAsync(userId.Value, request);
            return result.Success ? Ok(result) : BadRequest(result);
        }

    }
}
