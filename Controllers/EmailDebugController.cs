using System.Net;
using System.Net.Mail;
using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ELearning_ToanHocHay_Control.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmailDebugController : ControllerBase
    {
        private readonly IOptions<EmailSettings> _emailSettings;
        private readonly ILogger<EmailDebugController> _logger;

        public EmailDebugController(
            IOptions<EmailSettings> emailSettings,
            ILogger<EmailDebugController> logger)
        {
            _emailSettings = emailSettings;
            _logger = logger;
        }

        [HttpGet("config")]
        public IActionResult GetConfig()
        {
            var config = _emailSettings.Value;

            return Ok(new
            {
                smtpServer = config.SmtpServer,
                port = config.Port,
                enableSsl = config.EnableSsl,
                senderEmail = config.SenderEmail,
                username = config.Username,
                hasPassword = !string.IsNullOrEmpty(config.Password),
                passwordLength = config.Password?.Length ?? 0,
                // ⚠️ KHÔNG BAO GIỜ LOG PASSWORD THẬT
                passwordPreview = config.Password?.Substring(0, Math.Min(4, config.Password.Length)) + "****"
            });
        }

        [HttpPost("test-smtp")]
        public async Task<IActionResult> TestSmtp()
        {
            try
            {
                var config = _emailSettings.Value;

                _logger.LogInformation("Testing SMTP connection...");
                _logger.LogInformation("Server: {Server}:{Port}", config.SmtpServer, config.Port);
                _logger.LogInformation("Username: {Username}", config.Username);
                _logger.LogInformation("SSL: {Ssl}", config.EnableSsl);

                using var client = new SmtpClient(config.SmtpServer, config.Port)
                {
                    Credentials = new NetworkCredential(config.Username, config.Password),
                    EnableSsl = config.EnableSsl,
                    Timeout = 10000
                };

                // Test connection
                var testMessage = new MailMessage
                {
                    From = new MailAddress(config.SenderEmail, config.SenderName),
                    Subject = "Test Connection",
                    Body = "This is a test email from Railway deployment.",
                    IsBodyHtml = false
                };
                testMessage.To.Add(config.SenderEmail); // Gửi cho chính mình

                await client.SendMailAsync(testMessage);

                _logger.LogInformation("✅ SMTP test successful!");

                return Ok(new
                {
                    success = true,
                    message = "SMTP connection successful! Email sent to " + config.SenderEmail
                });
            }
            catch (SmtpException ex)
            {
                _logger.LogError(ex, "❌ SMTP test failed");

                return StatusCode(500, new
                {
                    success = false,
                    error = "SMTP Error",
                    statusCode = ex.StatusCode.ToString(),
                    message = ex.Message,
                    innerError = ex.InnerException?.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Test failed");

                return StatusCode(500, new
                {
                    success = false,
                    error = ex.GetType().Name,
                    message = ex.Message,
                    innerError = ex.InnerException?.Message
                });
            }
        }

        [HttpGet("check-sendgrid-config")]
        public IActionResult CheckSendGridConfig(
            [FromServices] IConfiguration config,
            [FromServices] IEmailService emailService)
        {
            var apiKey = config["SendGrid:ApiKey"];
            var senderEmail = config["SendGrid:SenderEmail"];
            var senderName = config["SendGrid:SenderName"];

            return Ok(new
            {
                hasApiKey = !string.IsNullOrEmpty(apiKey),
                apiKeyPrefix = apiKey?.Substring(0, 10) + "...",
                senderEmail = senderEmail,
                senderName = senderName,
                emailServiceType = emailService.GetType().Name
            });
        }

        [HttpGet("current-config")]
        public IActionResult GetCurrentConfig([FromServices] IConfiguration config)
        {
            return Ok(new
            {
                senderEmail = config["SendGrid:SenderEmail"],
                senderName = config["SendGrid:SenderName"],
                hasApiKey = !string.IsNullOrEmpty(config["SendGrid:ApiKey"]),
                apiKeyLength = config["SendGrid:ApiKey"]?.Length ?? 0
            });
        }
    }
}
