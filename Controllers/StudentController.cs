using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ELearning_ToanHocHay_Control.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Bắt buộc đăng nhập để lấy dữ liệu cá nhân
    public class StudentController : ControllerBase
    {
        private readonly IExerciseAttemptService _attemptService;

        public StudentController(IExerciseAttemptService attemptService)
        {
            _attemptService = attemptService;
        }

        [HttpGet("dashboard-stats")]
        public async Task<IActionResult> GetDashboardStats()
        {
            // 1. Lấy UserId từ Token của người dùng đang đăng nhập
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized();

            int userId = int.Parse(userIdClaim.Value);

            // 2. Gọi Service để lấy dữ liệu thống kê mà mình vừa viết lúc nãy
            var result = await _attemptService.GetDashboardStatsAsync(userId);

            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }
    }
}