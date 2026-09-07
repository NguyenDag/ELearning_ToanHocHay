using ELearning_ToanHocHay_Control.Attributes;
using ELearning_ToanHocHay_Control.Common;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ELearning_ToanHocHay_Control.Controllers
{
    /// <summary>P1 — admin account operations (lock / unlock / role) and the audit log.</summary>
    [Route("api/admin")]
    [ApiController]
    [AuthorizeUserType(UserType.SystemAdmin)]
    public class AdminController : ControllerBase
    {
        private readonly IAdminUserService _admin;
        private readonly INotificationRuleEngine _rules;
        private readonly ISystemConfigService _config;

        public AdminController(IAdminUserService admin, INotificationRuleEngine rules, ISystemConfigService config)
        {
            _admin = admin;
            _rules = rules;
            _config = config;
        }

        [HttpPost("notifications/run-inactivity-check")]
        public async Task<IActionResult> RunInactivityCheck()
            => Ok(new { created = await _rules.RunInactivitySweepAsync() });

        // ---------------- SystemConfig (P6/P7) ----------------
        [HttpGet("config")]
        public async Task<IActionResult> GetConfig([FromQuery] string? group)
            => (await _config.GetAllAsync(group)).ToActionResult();

        [HttpPut("config/{key}")]
        public async Task<IActionResult> SetConfig(string key, [FromBody] Models.DTOs.SetConfigDto dto)
        {
            var r = await _config.SetAsync(key, dto.Value, User.GetUserId()!.Value);
            return r.ToActionResult();
        }

        private string? Ip => HttpContext.Connection.RemoteIpAddress?.ToString();
        private int AdminId => User.GetUserId()!.Value;

        [HttpPost("users/{id:int}/lock")]
        public async Task<IActionResult> Lock(int id, [FromBody] LockUserDto dto)
        {
            var r = await _admin.LockUserAsync(id, AdminId, dto.Reason, Ip);
            return r.ToActionResult();
        }

        [HttpPost("users/{id:int}/unlock")]
        public async Task<IActionResult> Unlock(int id)
        {
            var r = await _admin.UnlockUserAsync(id, AdminId, Ip);
            return r.ToActionResult();
        }

        [HttpPost("users/{id:int}/role")]
        public async Task<IActionResult> ChangeRole(int id, [FromBody] ChangeRoleDto dto)
        {
            var r = await _admin.ChangeRoleAsync(id, dto.NewRole, AdminId, Ip);
            return r.ToActionResult();
        }

        [HttpPost("users/{id:int}/reset-password")]
        public async Task<IActionResult> ResetPassword(int id, [FromBody] ResetPasswordByAdminDto dto)
        {
            var r = await _admin.ResetPasswordAsync(id, AdminId, dto.NewPassword, Ip);
            return r.ToActionResult();
        }

        [HttpPost("users/{id:int}/confirm-email")]
        public async Task<IActionResult> ConfirmEmail(int id)
        {
            var r = await _admin.SetEmailConfirmedAsync(id, AdminId, Ip);
            return r.ToActionResult();
        }

        [HttpPost("users/{id:int}/deactivate")]
        public async Task<IActionResult> Deactivate(int id)
        {
            var r = await _admin.SetActiveAsync(id, false, AdminId, Ip);
            return r.ToActionResult();
        }

        [HttpPost("users/{id:int}/activate")]
        public async Task<IActionResult> Activate(int id)
        {
            var r = await _admin.SetActiveAsync(id, true, AdminId, Ip);
            return r.ToActionResult();
        }

        [HttpGet("roles")]
        public IActionResult GetRoles()
            => ApiResponse<object>.SuccessResponse(Common.RoleCapabilities.All).ToActionResult();

        [HttpGet("audit-logs")]
        public async Task<IActionResult> GetAuditLogs([FromQuery] AuditLogFilter filter)
            => (await _admin.GetAuditLogsAsync(filter)).ToActionResult();

        [HttpGet("audit-logs/facets")]
        public async Task<IActionResult> GetAuditFacets()
            => (await _admin.GetAuditFacetsAsync()).ToActionResult();

        [HttpGet("audit-logs/export")]
        public async Task<IActionResult> ExportAuditLogs([FromQuery] AuditLogFilter filter)
        {
            var bytes = await _admin.ExportAuditLogsCsvAsync(filter);
            return File(bytes, "text/csv", $"nhat-ky-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv");
        }
    }
}
