using System.Net;
using System.Net.Mail;
using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace ELearning_ToanHocHay_Control.Services.Implementations
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;

        public EmailService(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings.Value;
        }
        public async Task SendConfirmEmailAsync(string toEmail, string fullName, string confirmLink)
        {
            var subject = "Xác nhận đăng ký tài khoản";

            var body = $@"
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

            var message = new MailMessage
            {
                From = new MailAddress(
                    _emailSettings.SenderEmail,
                    _emailSettings.SenderName
                ),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            message.To.Add(toEmail);

            using var smtp = new SmtpClient(
                _emailSettings.SmtpServer,
                _emailSettings.Port
            )
            {
                Credentials = new NetworkCredential(
                    _emailSettings.Username,
                    _emailSettings.Password
                ),
                EnableSsl = _emailSettings.EnableSsl
            };

            await smtp.SendMailAsync(message);
        }
    }
}
