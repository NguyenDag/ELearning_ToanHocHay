using ELearning_ToanHocHay_Control.Services.Interfaces;
using SendGrid;
using SendGrid.Helpers.Mail;

public class SendGridEmailService : IEmailService
{
    private readonly string? _apiKey;
    private readonly string? _senderEmail;
    private readonly string _senderName;
    private readonly ILogger<SendGridEmailService> _logger;
    private readonly bool _isEnabled;

    public SendGridEmailService(
        IConfiguration configuration,
        ILogger<SendGridEmailService> logger)
    {
        _logger = logger;

        _apiKey = configuration["SendGrid:ApiKey"];
        _senderEmail = configuration["SendGrid:SenderEmail"];
        _senderName = configuration["SendGrid:SenderName"] ?? "E-Learning Team";

        _isEnabled = !string.IsNullOrEmpty(_apiKey)
                     && !string.IsNullOrEmpty(_senderEmail);

        if (!_isEnabled)
        {
            _logger.LogWarning("⚠️ SendGrid is NOT configured. Email service disabled.");
        }
    }

    public async Task SendConfirmEmailAsync(string toEmail, string fullName, string confirmLink)
    {
        if (!_isEnabled)
        {
            _logger.LogInformation("📭 Email skipped (SendGrid disabled). To: {Email}", toEmail);
            return;
        }

        try
        {
            _logger.LogInformation("🚀 Sending email via SendGrid to {Email}", toEmail);

            var client = new SendGridClient(_apiKey);
            var from = new EmailAddress(_senderEmail!, _senderName);
            var to = new EmailAddress(toEmail, fullName);
            var subject = "Xác nhận đăng ký tài khoản";

            var htmlContent = $@"
                <p>Xin chào <strong>{fullName}</strong>,</p>
                <p>Bạn vừa đăng ký tài khoản trên <b>E-Learning Toán Học Hay</b>.</p>
                <p>Nhấn vào nút dưới để xác nhận:</p>
                <p>
                    <a href='{confirmLink}'
                       style='padding:10px 20px;background:#2563eb;color:white;text-decoration:none;border-radius:6px;'>
                       Xác nhận email
                    </a>
                </p>
                <p>Link có hiệu lực 24 giờ.</p>
                <br/>
                <p>E-Learning Team</p>";

            var msg = MailHelper.CreateSingleEmail(from, to, subject, "", htmlContent);
            var response = await client.SendEmailAsync(msg);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Body.ReadAsStringAsync();
                _logger.LogError("❌ SendGrid error {StatusCode}: {Body}",
                    response.StatusCode, body);
            }
            else
            {
                _logger.LogInformation("✅ Email sent to {Email}", toEmail);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ SendGrid send failed to {Email}", toEmail);
        }
    }
}
