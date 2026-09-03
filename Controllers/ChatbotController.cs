using ELearning_ToanHocHay_Control.Attributes;
using ELearning_ToanHocHay_Control.Common;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Models.DTOs.Chatbot;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ELearning_ToanHocHay_Control.Controllers
{
    [Route("api/chatbot")]
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
            return r.ToActionResult();
        }

        [HttpPost("quick-reply")]
        public async Task<IActionResult> QuickReply([FromBody] SendChatMessageDto request)
        {
            var r = await _chat.SendUserTurnAsync(Uid, User.GetStudentId(), request.Text, isQuickReply: true);
            return r.ToActionResult();
        }

        [HttpPost("trigger")]
        public async Task<IActionResult> Trigger([FromBody] ChatbotTriggerRequest request)
        {
            try
            {
                request.UserId = Uid.ToString();
                return Ok(ApiResponse<object>.SuccessResponse(await _aiService.SendChatbotTriggerAsync(request)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Chatbot trigger failed");
                return StatusCode(503, ApiResponse<object>.ErrorResponse("AI service unavailable"));
            }
        }

        [HttpGet("conversations")]
        public async Task<IActionResult> Conversations()
            => (await _chat.GetMyConversationsAsync(Uid)).ToActionResult();

        [HttpGet("conversations/{id:int}/messages")]
        public async Task<IActionResult> Messages(int id)
        {
            var r = await _chat.GetMessagesAsync(Uid, id);
            return r.ToActionResult();
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
            => (await _chat.RequestHumanAsync(Uid)).ToActionResult();

        [HttpPost("conversations/{id:int}/close")]
        public async Task<IActionResult> Close(int id)
        {
            var isStaff = User.HasUserType(UserType.SupportStaff, UserType.SystemAdmin);
            var r = await _chat.CloseAsync(Uid, id, isStaff);
            return r.ToActionResult();
        }

        // ---------------- staff ----------------
        [HttpGet("staff/queue")]
        [AuthorizeUserType(UserType.SupportStaff, UserType.SystemAdmin)]
        public async Task<IActionResult> Queue()
            => (await _chat.GetQueueAsync()).ToActionResult();

        [HttpPost("staff/conversations/{id:int}/assign")]
        [AuthorizeUserType(UserType.SupportStaff, UserType.SystemAdmin)]
        public async Task<IActionResult> Assign(int id)
        {
            var r = await _chat.AssignToMeAsync(Uid, id);
            return r.ToActionResult();
        }

        [HttpPost("staff/conversations/{id:int}/reply")]
        [AuthorizeUserType(UserType.SupportStaff, UserType.SystemAdmin)]
        public async Task<IActionResult> StaffReply(int id, [FromBody] SendChatMessageDto dto)
        {
            var r = await _chat.StaffReplyAsync(Uid, id, dto.Text, User.IsSystemAdmin());
            return r.ToActionResult();
        }
    }
}
