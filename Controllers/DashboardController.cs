using ELearning_ToanHocHay_Control.Common;
using ELearning_ToanHocHay_Control.Models.DTOs.Student.Dashboard;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ELearning_ToanHocHay_Control.Controllers
{
    [Route("api/students/{studentId:int}/dashboard")]
    [ApiController]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly ICoreDashboardService _coreDashboardService;

        public DashboardController(ICoreDashboardService coreDashboardService)
        {
            _coreDashboardService = coreDashboardService;
        }

        /// <summary>Returns a 403 result if the caller may not view <paramref name="studentId"/>, else null.</summary>
        private async Task<IActionResult?> GuardAsync(int studentId)
        {
            var currentUserId = User.GetUserId();
            if (currentUserId == null)
                return Unauthorized(ApiResponse<object>.ErrorResponse("Lỗi xác thực token"));

            return await _coreDashboardService.VerifyStudentAccessAsync(studentId, currentUserId.Value)
                ? null
                : this.Forbidden("Bạn không có quyền xem dữ liệu này");
        }

        private async Task<IActionResult?> RequireTierAsync(int studentId, PackageTier min)
        {
            var tier = await _coreDashboardService.GetPackageTierAsync(studentId);
            return tier >= min ? null : this.Forbidden($"Tính năng này cần gói {min} trở lên");
        }

        [HttpGet("overview")]
        public async Task<IActionResult> GetCoreDashboard(int studentId)
        {
            if (await GuardAsync(studentId) is { } guard) return guard;

            var dashboard = await _coreDashboardService.GetCoreDashboardAsync(studentId);
            return dashboard == null
                ? ApiResponse<CoreDashboardDto>.NotFound("Không tìm thấy dữ liệu học sinh").ToActionResult()
                : ApiResponse<CoreDashboardDto>.SuccessResponse(dashboard).ToActionResult();
        }

        [HttpGet("chapter-score-comparison")]
        public async Task<IActionResult> GetChapterScoreComparison(int studentId)
        {
            if (await GuardAsync(studentId) is { } guard) return guard;
            if (await RequireTierAsync(studentId, PackageTier.Standard) is { } tierGuard) return tierGuard;

            var result = await _coreDashboardService.GetChapterScoreComparisonAsync(studentId);
            return Ok(ApiResponse<object>.SuccessResponse(result));
        }

        [HttpGet("ai-assessment")]
        public async Task<IActionResult> GetAIAssessment(int studentId)
        {
            if (await GuardAsync(studentId) is { } guard) return guard;
            if (await RequireTierAsync(studentId, PackageTier.Premium) is { } tierGuard) return tierGuard;

            var result = await _coreDashboardService.GetAIInsightAsync(studentId);
            return result != null
                ? Ok(ApiResponse<object>.SuccessResponse(result))
                : ApiResponse<object>.NotFound("Chưa có dữ liệu để phân tích").ToActionResult();
        }

        [HttpGet("ai-roadmap")]
        public async Task<IActionResult> GetAIRoadmap(int studentId)
        {
            if (await GuardAsync(studentId) is { } guard) return guard;
            if (await RequireTierAsync(studentId, PackageTier.Premium) is { } tierGuard) return tierGuard;

            var result = await _coreDashboardService.GetAIRoadmapAsync(studentId);
            return result != null
                ? Ok(ApiResponse<object>.SuccessResponse(result))
                : ApiResponse<object>.NotFound("Chưa có dữ liệu để phân tích lộ trình").ToActionResult();
        }
    }
}
