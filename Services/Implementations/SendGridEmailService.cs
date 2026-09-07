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
        // B8 — dev fallback: khi email tắt, in link xác nhận ra log để không ai bị kẹt.
        if (!_isEnabled)
        {
            _logger.LogWarning(
                "📭 Email tắt (SendGrid chưa cấu hình). Link xác nhận cho {Email}: {ConfirmLink}",
                toEmail, confirmLink);
            return;
        }

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

        await SendAsync(toEmail, fullName, "Xác nhận đăng ký tài khoản", htmlContent, "xác nhận email");
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string fullName, string resetLink)
    {
        if (!_isEnabled)
        {
            _logger.LogWarning(
                "📭 Email tắt (SendGrid chưa cấu hình). Link đặt lại mật khẩu cho {Email}: {ResetLink}",
                toEmail, resetLink);
            return;
        }

        var htmlContent = $@"
            <p>Xin chào <strong>{fullName}</strong>,</p>
            <p>Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn.</p>
            <p>
                <a href='{resetLink}'
                   style='padding:10px 20px;background:#2563eb;color:white;text-decoration:none;border-radius:6px;'>
                   Đặt lại mật khẩu
                </a>
            </p>
            <p>Link có hiệu lực 1 giờ. Nếu bạn không yêu cầu, hãy bỏ qua email này.</p>
            <br/><p>E-Learning Team</p>";

        await SendAsync(toEmail, fullName, "Đặt lại mật khẩu", htmlContent, "đặt lại mật khẩu");
    }

    public async Task SendTabSwitchNotificationAsync(
        string toEmail,
        string parentName,
        string studentName,
        string exerciseName,
        DateTime switchedAt,
        int switchCount)
    {
        if (!_isEnabled)
        {
            _logger.LogInformation("📭 Tab switch email skipped (SendGrid disabled). To: {Email}", toEmail);
            return;
        }

        var timeStr = switchedAt.ToLocalTime().ToString("HH:mm:ss dd/MM/yyyy");
        var subject = $"⚠️ Cảnh báo: {studentName} đã chuyển tab trong lúc làm bài!";

        var htmlContent = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; border: 1px solid #e2e8f0; border-radius: 8px; overflow: hidden;'>
                <div style='background: linear-gradient(135deg, #ef4444, #dc2626); padding: 24px; text-align: center;'>
                    <h1 style='color: white; margin: 0; font-size: 22px;'>⚠️ Cảnh Báo Gian Lận Bài Thi</h1>
                    <p style='color: #fca5a5; margin: 4px 0 0 0;'>Hệ thống E-Learning Toán Học Hay</p>
                </div>
                <div style='padding: 28px;'>
                    <p>Kính gửi Phụ huynh <strong>{parentName}</strong>,</p>
                    <p>Hệ thống đã phát hiện hành vi <strong>chuyển tab trình duyệt</strong> trong lúc làm bài kiểm tra:</p>
                    <table style='width: 100%; border-collapse: collapse; margin: 16px 0;'>
                        <tr style='background: #f8fafc;'>
                            <td style='padding: 10px 14px; border: 1px solid #e2e8f0; font-weight: bold; width: 40%;'>👨‍🎓 Học sinh</td>
                            <td style='padding: 10px 14px; border: 1px solid #e2e8f0;'>{studentName}</td>
                        </tr>
                        <tr>
                            <td style='padding: 10px 14px; border: 1px solid #e2e8f0; font-weight: bold;'>📝 Bài thi</td>
                            <td style='padding: 10px 14px; border: 1px solid #e2e8f0;'>{exerciseName}</td>
                        </tr>
                        <tr style='background: #f8fafc;'>
                            <td style='padding: 10px 14px; border: 1px solid #e2e8f0; font-weight: bold;'>🕐 Thời điểm</td>
                            <td style='padding: 10px 14px; border: 1px solid #e2e8f0;'>{timeStr}</td>
                        </tr>
                        <tr>
                            <td style='padding: 10px 14px; border: 1px solid #e2e8f0; font-weight: bold;'>🔢 Số lần chuyển tab</td>
                            <td style='padding: 10px 14px; border: 1px solid #e2e8f0; color: #dc2626; font-weight: bold;'>{switchCount} lần</td>
                        </tr>
                    </table>
                    <div style='background: #fff7ed; border-left: 4px solid #f97316; padding: 14px 18px; border-radius: 4px; margin-top: 16px;'>
                        <p style='margin: 0; color: #92400e;'>Học sinh đã được cảnh báo trực tiếp trên màn hình. Vui lòng nhắc nhở con em về tính trung thực trong học tập.</p>
                    </div>
                </div>
                <div style='background: #f1f5f9; padding: 16px; text-align: center;'>
                    <p style='margin: 0; color: #64748b; font-size: 13px;'>Email này được gửi tự động từ hệ thống <b>E-Learning Toán Học Hay</b>.</p>
                </div>
            </div>";

        await SendAsync(toEmail, parentName, subject, htmlContent, "cảnh báo chuyển tab");
    }

    /// <summary>
    /// B4 — gửi email qua SendGrid và <b>ném exception</b> nếu không thành công, để
    /// <c>BackgroundEmailService</c> biết mà thử lại (B5) thay vì nuốt lỗi âm thầm.
    /// </summary>
    private async Task SendAsync(string toEmail, string toName, string subject, string htmlContent, string kind)
    {
        Response response;
        try
        {
            var client = new SendGridClient(_apiKey);
            var from = new EmailAddress(_senderEmail!, _senderName);
            var to = new EmailAddress(toEmail, toName);
            var msg = MailHelper.CreateSingleEmail(from, to, subject, "", htmlContent);
            response = await client.SendEmailAsync(msg);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ SendGrid ({Kind}) ném exception khi gửi tới {Email}", kind, toEmail);
            throw;
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Body.ReadAsStringAsync();
            _logger.LogError("❌ SendGrid ({Kind}) trả {StatusCode} khi gửi tới {Email}: {Body}",
                kind, response.StatusCode, toEmail, body);
            throw new InvalidOperationException(
                $"SendGrid trả {(int)response.StatusCode} khi gửi email {kind} tới {toEmail}");
        }

        _logger.LogInformation("✅ Đã gửi email ({Kind}) tới {Email}", kind, toEmail);
    }
}
