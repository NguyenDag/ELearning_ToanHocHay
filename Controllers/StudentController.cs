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
        private readonly IParentLinkService _links;
        private readonly IResourceAccessService _access;

        public StudentController(
            IExerciseAttemptService attemptService,
            IParentLinkService links,
            IResourceAccessService access)
        {
            _attemptService = attemptService;
            _links = links;
            _access = access;
        }

        [HttpGet("dashboard-stats")]
        public async Task<IActionResult> GetDashboardStats()
        {
            var userId = User.GetUserId();
            if (userId == null) return Unauthorized(ApiResponse<object>.ErrorResponse("Token không hợp lệ"));

            return (await _attemptService.GetDashboardStatsAsync(userId.Value)).ToActionResult();
        }

        /// <summary>The parents linked to a student — owner, a linked parent, or an admin.</summary>
        [HttpGet("{studentId:int}/parents")]
        public async Task<IActionResult> GetParents(int studentId)
        {
            if (!await _access.CanAccessStudentAsync(User, studentId))
                return this.Forbidden();

            return (await _links.GetParentsForStudentAsync(studentId)).ToActionResult();
        }
    }
}
