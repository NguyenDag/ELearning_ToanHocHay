using ELearning_ToanHocHay_Control.Common;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs.Student.Dashboard;
using ELearning_ToanHocHay_Control.Services.Implementations;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ELearning_ToanHocHay_Control.Controllers
{
    [Route("api/student/{studentId}/dashboard")]
    [ApiController]
    [Authorize] // the backend validates the token forwarded by the WebApp
    public class DashboardController : ControllerBase
    {
        private readonly ICoreDashboardService _coreDashboardService;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(ICoreDashboardService coreDashboardService, ILogger<DashboardController> logger)
        {
            _coreDashboardService = coreDashboardService;
            _logger = logger;
        }

        /// <summary>
        /// Checks whether the caller may view <paramref name="studentId"/>'s data.
        /// Returns an error result if not, or null when access is allowed.
        /// </summary>
        private async Task<ObjectResult?> GuardAsync(int studentId)
        {
            var currentUserId = User.GetUserId();
            if (currentUserId == null)
            {
                _logger.LogWarning("Token does not contain a valid UserId.");
                return Unauthorized(new { message = "Lỗi xác thực Token." });
            }

            var hasAccess = await _coreDashboardService.VerifyStudentAccessAsync(studentId, currentUserId.Value);
            if (!hasAccess)
            {
                _logger.LogWarning("User {UserId} attempted to access Student {StudentId}", currentUserId, studentId);
                return StatusCode(403, new { message = "Bạn không có quyền xem dữ liệu này." });
            }

            return null;
        }

        [HttpGet("overview")]
        public async Task<ActionResult<CoreDashboardDto>> GetCoreDashboard(int studentId)
        {
            try
            {
                var guard = await GuardAsync(studentId);
                if (guard != null) return guard;

                var dashboard = await _coreDashboardService.GetCoreDashboardAsync(studentId);

                if (dashboard == null)
                {
                    return NotFound(new { message = "Không tìm thấy dữ liệu học sinh." });
                }

                return Ok(dashboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi tại Dashboard API");
                return StatusCode(500, new { message = "Lỗi máy chủ." });
            }
        }

        [HttpGet("chapter-score-comparison")]
        public async Task<IActionResult> GetChapterScoreComparison(int studentId)
        {
            var guard = await GuardAsync(studentId);
            if (guard != null) return guard;

            var tier = await _coreDashboardService.GetPackageTierAsync(studentId);

            if (tier < PackageTier.Standard)
                return StatusCode(403, new { message = "Gói của bạn không hỗ trợ tính năng này." });

            var result = await _coreDashboardService.GetChapterScoreComparisonAsync(studentId);

            return Ok(result);
        }

        [HttpGet("ai-assessment")]
        public async Task<IActionResult> GetAIAssessment(int studentId)
        {
            var guard = await GuardAsync(studentId);
            if (guard != null) return guard;

            var tier = await _coreDashboardService.GetPackageTierAsync(studentId);
            if (tier < PackageTier.Premium)
                return StatusCode(403, new { message = "Tính năng này chỉ dành cho tài khoản Premium." });

            var result = await _coreDashboardService.GetAIInsightAsync(studentId);
            return result != null ? Ok(result) : NotFound(new { message = "Chưa có dữ liệu để phân tích." });
        }

        [HttpGet("ai-roadmap")]
        public async Task<IActionResult> GetAIRoadmap(int studentId)
        {
            var guard = await GuardAsync(studentId);
            if (guard != null) return guard;

            var tier = await _coreDashboardService.GetPackageTierAsync(studentId);
            if (tier < PackageTier.Premium)
                return StatusCode(403, new { message = "Tính năng này chỉ dành cho tài khoản Premium." });

            var result = await _coreDashboardService.GetAIRoadmapAsync(studentId);
            return result != null ? Ok(result) : NotFound(new { message = "Chưa có dữ liệu để phân tích lộ trình." });
        }
    }
}
