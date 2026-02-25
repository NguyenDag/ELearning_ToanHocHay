using ELearning_ToanHocHay_Control.Models.DTOs.Student.Dashboard;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ELearning_ToanHocHay_Control.Controllers
{
    [Route("api/student/{studentId}/dashboard")]
    [ApiController]
    [Authorize] // Backend sẽ kiểm tra Token do WebApp gửi sang
    public class DashboardController : ControllerBase
    {
        private readonly ICoreDashboardService _coreDashboardService;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(ICoreDashboardService coreDashboardService, ILogger<DashboardController> logger)
        {
            _coreDashboardService = coreDashboardService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<CoreDashboardDto>> GetCoreDashboard(int studentId)
        {
            try
            {
                // LẤY USERID TỪ TOKEN (Đảm bảo khớp với AccountController WebApp)
                var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int currentUserId))
                {
                    _logger.LogWarning("Token không chứa UserId hợp lệ.");
                    return Unauthorized(new { message = "Lỗi xác thực Token." });
                }

                // KIỂM TRA QUYỀN: User này có phải chủ sở hữu của StudentId này không?
                var hasAccess = await _coreDashboardService.VerifyStudentAccessAsync(studentId, currentUserId);
                if (!hasAccess)
                {
                    _logger.LogWarning("User {UserId} cố truy cập Student {StudentId}", currentUserId, studentId);
                    return Forbid("Bạn không có quyền xem dữ liệu này.");
                }

                // LẤY DỮ LIỆU THỰC
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
                return StatusCode(500, new { message = "Lỗi máy chủ: " + ex.Message });
            }
        }
    }
}