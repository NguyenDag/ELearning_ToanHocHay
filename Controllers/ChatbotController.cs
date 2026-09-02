using ELearning_ToanHocHay_Control.Common;
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
        private readonly IAIService _aiService;
        private readonly ILogger<ChatbotController> _logger;

        public ChatbotController(IAIService aiService, ILogger<ChatbotController> logger)
        {
            _aiService = aiService;
            _logger = logger;
        }

        [HttpPost("message")]
        public async Task<IActionResult> Message([FromBody] ChatbotMessageRequest request)
        {
            try
            {
                request.UserId = User.GetUserId()?.ToString();
                var result = await _aiService.SendChatbotMessageAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ChatbotController.Message");
                return StatusCode(500, new { success = false, error = "Internal server error" });
            }
        }

        [HttpPost("quick-reply")]
        public async Task<IActionResult> QuickReply([FromBody] ChatbotQuickReplyRequest request)
        {
            try
            {
                request.UserId = User.GetUserId()?.ToString();
                var result = await _aiService.SendChatbotQuickReplyAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ChatbotController.QuickReply");
                return StatusCode(500, new { success = false, error = "Internal server error" });
            }
        }

        [HttpPost("trigger")]
        public async Task<IActionResult> Trigger([FromBody] ChatbotTriggerRequest request)
        {
            try
            {
                request.UserId = User.GetUserId()?.ToString();
                var result = await _aiService.SendChatbotTriggerAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ChatbotController.Trigger");
                return StatusCode(500, new { success = false, error = "Internal server error" });
            }
        }
    }
}
