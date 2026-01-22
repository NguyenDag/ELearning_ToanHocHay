using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ELearning_ToanHocHay_Control.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmailTestController : ControllerBase
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<EmailTestController> _logger;
        private readonly IOptions<AppSettings> _appSettings;

        public EmailTestController(
            IEmailService emailService,
            ILogger<EmailTestController> logger,
            IOptions<AppSettings> appSettings)
        {
            _emailService = emailService;
            _logger = logger;
            _appSettings = appSettings;
        }

        [HttpPost("send-test-email")]
        public async Task<IActionResult> SendTestEmail([FromBody] TestEmailRequest request)
        {
            try
            {
                _logger.LogInformation("📧 Testing email send to {Email}", request.ToEmail);

                var testLink = $"{_appSettings.Value.BaseUrl}/api/auth/confirm-email?token=test-token-12345";

                await _emailService.SendConfirmEmailAsync(
                    request.ToEmail,
                    request.FullName ?? "Test User",
                    testLink
                );

                return Ok(new
                {
                    success = true,
                    message = $"Email sent successfully to {request.ToEmail}",
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to send test email");

                return StatusCode(500, new
                {
                    success = false,
                    error = ex.Message,
                    innerError = ex.InnerException?.Message,
                    stackTrace = ex.StackTrace,
                    timestamp = DateTime.UtcNow
                });
            }
        }
    }
    public class TestEmailRequest
    {
        public string ToEmail { get; set; }
        public string FullName { get; set; }
    }
}
