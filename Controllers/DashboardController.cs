using ELearning_ToanHocHay_Control.Models.DTOs.Student.Dashboard;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ELearning_ToanHocHay_Control.Controllers
{
    [Route("api/student/{studentId}/dashboard")]
    [ApiController]
    [Authorize(Roles = "Student")]
    public class DashboardController : ControllerBase
    {
        private readonly ICoreDashboardService _coreDashboardService;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(
            ICoreDashboardService coreDashboardService,
            ILogger<DashboardController> logger)
        {
            _coreDashboardService = coreDashboardService;
            _logger = logger;
        }

        /// <summary>
        /// Get core dashboard data (essential info only)
        /// </summary>
        /// <param name="studentId">Student ID from route</param>
        /// <returns>Core dashboard with stats, recent lessons, and chapter progress</returns>
        [HttpGet]
        [ResponseCache(Duration = 180)] // Cache 3 minutes
        [ProducesResponseType(typeof(CoreDashboardDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<CoreDashboardDto>> GetCoreDashboard(int studentId)
        {
            try
            {
                // Verify authorization (student can only access their own dashboard)
                var currentUserId = GetCurrentUserId();
                if (!await VerifyStudentAccess(studentId, currentUserId))
                {
                    return Unauthorized(new { message = "Bạn không có quyền truy cập dashboard này" });
                }

                var dashboard = await _coreDashboardService.GetCoreDashboardAsync(studentId);

                if (dashboard == null)
                {
                    return NotFound(new { message = "Không tìm thấy thông tin học sinh" });
                }

                return Ok(dashboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting core dashboard for student {StudentId}", studentId);
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi tải dashboard" });
            }
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        private async Task<bool> VerifyStudentAccess(int studentId, int userId)
        {
            // Implementation: Check if userId matches the student's userId
            // For now, simplified version
            return await _coreDashboardService.VerifyStudentAccessAsync(studentId, userId);
        }
    }
}
