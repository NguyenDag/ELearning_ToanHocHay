using ELearning_ToanHocHay_Control.Models.DTOs.Student.Dashboard;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ELearning_ToanHocHay_Control.Controllers
{
    [Route("api/student/{studentId}/dashboard")]
    [ApiController]
    [Authorize] // Đảm bảo người dùng đã đăng nhập
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

        [HttpGet]
        public async Task<ActionResult<CoreDashboardDto>> GetCoreDashboard(int studentId)
        {
            try
            {
                // 1. Lấy UserId từ Token (Dạng String)
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                // 2. Chuyển đổi UserId sang kiểu int để khớp với tham số của Service
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int currentUserId))
                {
                    _logger.LogWarning("Không tìm thấy UserId hợp lệ trong Token.");
                    return Unauthorized(new { message = "Token không hợp lệ hoặc thiếu thông tin định danh" });
                }

                // 3. Kiểm tra quyền truy cập (Đã truyền đúng kiểu int vào Argument 2)
                var hasAccess = await _coreDashboardService.VerifyStudentAccessAsync(studentId, currentUserId);

                if (!hasAccess)
                {
                    _logger.LogWarning("User {UserId} cố gắng truy cập trái phép Student {StudentId}", currentUserId, studentId);
                    return Forbid("Bạn không có quyền xem dữ liệu của học sinh này");
                }

                // 4. Lấy dữ liệu Dashboard
                var dashboard = await _coreDashboardService.GetCoreDashboardAsync(studentId);

                if (dashboard == null)
                {
                    _logger.LogWarning("API trả về NULL cho Student {StudentId} dù đã qua bước Auth", studentId);
                    return NotFound(new { message = "Không tìm thấy dữ liệu cho học sinh này" });
                }

                return Ok(dashboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi nghiêm trọng tại Dashboard API cho Student {StudentId}", studentId);
                return StatusCode(500, new { message = "Lỗi hệ thống: " + ex.Message });
            }
        }
    }
}