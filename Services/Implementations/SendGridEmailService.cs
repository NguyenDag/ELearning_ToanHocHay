using ELearning_ToanHocHay_Control.Services.Interfaces;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace ELearning_ToanHocHay_Control.Services.Implementations
{
    public class SendGridEmailService : IEmailService
    {
        private readonly string _apiKey;
        private readonly string _senderEmail;
        private readonly string _senderName;
        private readonly ILogger<SendGridEmailService> _logger;

        public SendGridEmailService(
            IConfiguration configuration,
            ILogger<SendGridEmailService> logger)
        {
            _apiKey = configuration["SendGrid:ApiKey"]
                ?? throw new Exception("SendGrid:ApiKey not configured");
            _senderEmail = configuration["SendGrid:SenderEmail"]
                ?? throw new Exception("SendGrid:SenderEmail not configured");
            _senderName = configuration["SendGrid:SenderName"] ?? "E-Learning Team";
            _logger = logger;
        }
        public async Task SendConfirmEmailAsync(string toEmail, string fullName, string confirmLink)
        {
            try
            {
                _logger.LogInformation("🚀 Sending email via SendGrid to {Email}", toEmail);

                var client = new SendGridClient(_apiKey);
                var from = new EmailAddress(_senderEmail, _senderName);
                var to = new EmailAddress(toEmail, fullName);
                var subject = "Xác nhận đăng ký tài khoản";

                var htmlContent = $@"
                <p>Xin chào <strong>{fullName}</strong>,</p>
                <p>Bạn vừa đăng ký tài khoản trên hệ thống <b>E-Learning Toán Học Hay</b>.</p>
                <p>Vui lòng nhấn vào nút bên dưới để xác nhận email:</p>
                <p>
                    <a href='{confirmLink}'
                       style='display:inline-block;
                              padding:10px 20px;
                              background:#2563eb;
                              color:#ffffff;
                              text-decoration:none;
                              border-radius:6px;'>
                       Xác nhận email
                    </a>
                </p>
                <p>Link có hiệu lực trong <b>24 giờ</b>.</p>
                <p>Nếu bạn không thực hiện đăng ký, vui lòng bỏ qua email này.</p>
                <br/>
                <p>Trân trọng,<br/>E-Learning Team</p>
            ";

                var msg = MailHelper.CreateSingleEmail(from, to, subject, "", htmlContent);
                var response = await client.SendEmailAsync(msg);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("✅ Email sent successfully via SendGrid to {Email}", toEmail);
                }
                else
                {
                    var body = await response.Body.ReadAsStringAsync();
                    _logger.LogError("❌ SendGrid error: {StatusCode} - {Body}",
                        response.StatusCode, body);
                    throw new Exception($"SendGrid error: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to send email via SendGrid to {Email}", toEmail);
                throw;
            }
        }
    }
}
