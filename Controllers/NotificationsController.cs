using ELearning_ToanHocHay_Control.Common;
using ELearning_ToanHocHay_Control.Models.DTOs.Notification;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ELearning_ToanHocHay_Control.Controllers
{
    /// <summary>P6 — the caller's own notifications + per-rule opt-out.</summary>
    [Route("api/notifications")]
    [ApiController]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notifications;

        public NotificationsController(INotificationService notifications)
        {
            _notifications = notifications;
        }

        private int Uid => User.GetUserId()!.Value;

        [HttpGet]
        public async Task<IActionResult> GetMine(
            [FromQuery] bool unreadOnly = false, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
            => Ok(await _notifications.GetMineAsync(Uid, unreadOnly, page, pageSize));

        [HttpGet("unread-count")]
        public async Task<IActionResult> UnreadCount()
            => Ok(await _notifications.GetUnreadCountAsync(Uid));

        [HttpPost("{id:int}/read")]
        public async Task<IActionResult> MarkRead(int id)
        {
            var r = await _notifications.MarkReadAsync(Uid, id);
            return r.ToActionResult();
        }

        [HttpPost("read-all")]
        public async Task<IActionResult> MarkAllRead()
            => Ok(await _notifications.MarkAllReadAsync(Uid));

        [HttpGet("preferences")]
        public async Task<IActionResult> GetPreferences()
            => Ok(await _notifications.GetPreferencesAsync(Uid));

        [HttpPut("preferences")]
        public async Task<IActionResult> SetPreference([FromBody] SetNotificationPreferenceDto dto)
        {
            var r = await _notifications.SetPreferenceAsync(Uid, dto.RuleKey, dto.Enabled);
            return r.ToActionResult();
        }
    }
}
