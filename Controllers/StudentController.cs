using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Common;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ELearning_ToanHocHay_Control.Controllers
{
    [Route("api/students")]
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
            var userId = User.GetUserId();
            if (userId == null) return Unauthorized(ApiResponse<object>.ErrorResponse("Token không hợp lệ"));

            return (await _attemptService.GetDashboardStatsAsync(userId.Value)).ToActionResult();
        }
    }
}