using ELearning_ToanHocHay_Control.Common;
using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ELearning_ToanHocHay_Control.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        private string? ClientIp => HttpContext.Connection.RemoteIpAddress?.ToString();

        [HttpPost("login")]
        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<LoginResponseDto>.ErrorResponse("Dữ liệu không hợp lệ", ModelErrors()));

            var result = await _authService.LoginAsync(request, ClientIp);
            return result.Success ? Ok(result) : Unauthorized(result);
        }

        [HttpPost("validate-token")]
        [AllowAnonymous]
        public async Task<IActionResult> ValidateToken([FromBody] string token)
        {
            var isValid = await _authService.ValidateTokenAsync(token);
            return isValid
                ? Ok(ApiResponse<bool>.SuccessResponse(true, "Token hợp lệ"))
                : Unauthorized(ApiResponse<bool>.ErrorResponse("Token không hợp lệ hoặc đã hết hạn"));
        }

        /// <summary>Current user's info, read straight from the validated token's claims (A1-11).</summary>
        [HttpGet("me")]
        [Authorize]
        public IActionResult GetCurrentUser()
        {
            var userId = User.GetUserId();
            if (!userId.HasValue)
                return Unauthorized(ApiResponse<object>.ErrorResponse("Token không hợp lệ"));

            return Ok(ApiResponse<object>.SuccessResponse(new
            {
                UserId = userId.Value,
                Email = User.GetEmail(),
                FullName = User.Identity?.Name,
                UserType = User.GetUserType()?.ToString(),
                StudentId = User.GetStudentId(),
                ParentId = User.GetParentId()
            }, "Lấy thông tin thành công"));
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout([FromBody] LogoutDto? request)
        {
            var userId = User.GetUserId();
            if (!userId.HasValue)
                return Unauthorized(ApiResponse<bool>.ErrorResponse("Token không hợp lệ"));

            return (await _authService.LogoutAsync(userId.Value, request?.RefreshToken)).ToActionResult();
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register(RegisterRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<bool>.ErrorResponse("Dữ liệu không hợp lệ", ModelErrors()));

            var result = await _authService.RegisterAsync(request);
            return result.ToActionResult();
        }

        [HttpPost("refresh-token")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshToken(RefreshTokenRequestDto request)
        {
            var result = await _authService.RefreshTokenAsync(request.RefreshToken, ClientIp);
            return result.Success ? Ok(result) : Unauthorized(result);
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordDto request)
        {
            var userId = User.GetUserId();
            if (!userId.HasValue) return Unauthorized(ApiResponse<object>.ErrorResponse("Token không hợp lệ"));

            var result = await _authService.ChangePasswordAsync(userId.Value, request);
            return result.ToActionResult();
        }

        [HttpGet("confirm-email")]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail([FromQuery] string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return BadRequest(ApiResponse<bool>.ErrorResponse("Token không hợp lệ"));

            var result = await _authService.ConfirmEmailAsync(token);
            return result.ToActionResult();
        }

        [HttpPost("resend-confirmation")]
        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> ResendConfirmation([FromBody] ResendConfirmationDto request)
        {
            var result = await _authService.ResendConfirmationEmailAsync(request.Email);
            return result.ToActionResult();
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto request)
        {
            return (await _authService.ForgotPasswordAsync(request.Email)).ToActionResult();
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto request)
        {
            var result = await _authService.ResetPasswordAsync(request.Token, request.NewPassword);
            return result.ToActionResult();
        }

        private List<string> ModelErrors() =>
            ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
    }
}
