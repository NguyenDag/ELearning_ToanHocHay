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

        public AdminController(IAdminUserService admin, INotificationRuleEngine rules)
        {
            _admin = admin;
            _rules = rules;
        }

        [HttpPost("notifications/run-inactivity-check")]
        public async Task<IActionResult> RunInactivityCheck()
            => Ok(new { created = await _rules.RunInactivitySweepAsync() });

        private string? Ip => HttpContext.Connection.RemoteIpAddress?.ToString();
        private int AdminId => User.GetUserId()!.Value;

        [HttpPost("users/{id:int}/lock")]
        public async Task<IActionResult> Lock(int id, [FromBody] LockUserDto dto)
        {
            var r = await _admin.LockUserAsync(id, AdminId, dto.Reason, Ip);
            return r.Success ? Ok(r) : BadRequest(r);
        }

        [HttpPost("users/{id:int}/unlock")]
        public async Task<IActionResult> Unlock(int id)
        {
            var r = await _admin.UnlockUserAsync(id, AdminId, Ip);
            return r.Success ? Ok(r) : BadRequest(r);
        }

        [HttpPost("users/{id:int}/role")]
        public async Task<IActionResult> ChangeRole(int id, [FromBody] ChangeRoleDto dto)
        {
            var r = await _admin.ChangeRoleAsync(id, dto.NewRole, AdminId, Ip);
            return r.Success ? Ok(r) : BadRequest(r);
        }

        [HttpGet("audit-logs")]
        public async Task<IActionResult> GetAuditLogs(
            [FromQuery] string? entityType,
            [FromQuery] int? entityId,
            [FromQuery] int? userId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
            => Ok(await _admin.GetAuditLogsAsync(entityType, entityId, userId, page, pageSize));
    }
}
