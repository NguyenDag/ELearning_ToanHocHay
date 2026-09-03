using ELearning_ToanHocHay_Control.Attributes;
using ELearning_ToanHocHay_Control.Common;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs.Chatbot;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ELearning_ToanHocHay_Control.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [EnableRateLimiting("ai")]
    public class ChatbotController : ControllerBase
    {
        private readonly IChatService _chat;
        private readonly IAIService _aiService;
        private readonly ILogger<ChatbotController> _logger;

        public ChatbotController(IChatService chat, IAIService aiService, ILogger<ChatbotController> logger)
        {
            _chat = chat;
            _aiService = aiService;
            _logger = logger;
        }

        private int Uid => User.GetUserId()!.Value;

        [HttpPost("message")]
        public async Task<IActionResult> Message([FromBody] SendChatMessageDto request)
        {
            var r = await _chat.SendUserTurnAsync(Uid, User.GetStudentId(), request.Text, isQuickReply: false);
            return r.Success ? Ok(r) : BadRequest(r);
        }

        [HttpPost("quick-reply")]
        public async Task<IActionResult> QuickReply([FromBody] SendChatMessageDto request)
        {
            var r = await _chat.SendUserTurnAsync(Uid, User.GetStudentId(), request.Text, isQuickReply: true);
            return r.Success ? Ok(r) : BadRequest(r);
        }

        [HttpPost("trigger")]
        public async Task<IActionResult> Trigger([FromBody] ChatbotTriggerRequest request)
        {
            try
            {
                request.UserId = Uid.ToString();
                return Ok(await _aiService.SendChatbotTriggerAsync(request));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Chatbot trigger failed");
                return StatusCode(503, new { success = false, error = "AI service unavailable" });
            }
        }

        [HttpGet("conversations")]
        public async Task<IActionResult> Conversations()
            => Ok(await _chat.GetMyConversationsAsync(Uid));

        [HttpGet("conversations/{id:int}/messages")]
        public async Task<IActionResult> Messages(int id)
        {
            var r = await _chat.GetMessagesAsync(Uid, id);
            return r.Success ? Ok(r) : NotFound(r);
        }

        [HttpGet("health")]
        [AllowAnonymous]
        public async Task<IActionResult> Health()
        {
            var ok = await _aiService.IsHealthyAsync();
            return ok
                ? Ok(new { status = "healthy" })
                : StatusCode(503, new { status = "unavailable" });
        }

        // ---------------- escalation ----------------
        [HttpPost("request-human")]
        public async Task<IActionResult> RequestHuman()
            => Ok(await _chat.RequestHumanAsync(Uid));

        [HttpPost("conversations/{id:int}/close")]
        public async Task<IActionResult> Close(int id)
        {
            var isStaff = User.HasUserType(UserType.SupportStaff, UserType.SystemAdmin);
            var r = await _chat.CloseAsync(Uid, id, isStaff);
            return r.Success ? Ok(r) : BadRequest(r);
        }

        // ---------------- staff ----------------
        [HttpGet("staff/queue")]
        [AuthorizeUserType(UserType.SupportStaff, UserType.SystemAdmin)]
        public async Task<IActionResult> Queue()
            => Ok(await _chat.GetQueueAsync());

        [HttpPost("staff/conversations/{id:int}/assign")]
        [AuthorizeUserType(UserType.SupportStaff, UserType.SystemAdmin)]
        public async Task<IActionResult> Assign(int id)
        {
            var r = await _chat.AssignToMeAsync(Uid, id);
            return r.Success ? Ok(r) : BadRequest(r);
        }

        [HttpPost("staff/conversations/{id:int}/reply")]
        [AuthorizeUserType(UserType.SupportStaff, UserType.SystemAdmin)]
        public async Task<IActionResult> StaffReply(int id, [FromBody] SendChatMessageDto dto)
        {
            var r = await _chat.StaffReplyAsync(Uid, id, dto.Text, User.IsSystemAdmin());
            return r.Success ? Ok(r) : BadRequest(r);
        }
    }
}
