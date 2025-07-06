// EcoConnect_Hanoi.Services/EmailSender.cs
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace EcoConnect_Hanoi.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly EmailSettings _emailSettings;

        public EmailSender(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings.Value;
        }

        public async Task SendEmailAsync(string email, string subject, string message)
        {
            // Kiểm tra các cài đặt cơ bản trước khi gửi
            if (string.IsNullOrEmpty(_emailSettings.SenderEmail) ||
                string.IsNullOrEmpty(_emailSettings.SenderPassword) ||
                string.IsNullOrEmpty(_emailSettings.SmtpHost) ||
                _emailSettings.SmtpPort == 0)
            {
                throw new InvalidOperationException("Email service is not configured correctly.");
            }

            var mailMessage = new MailMessage(_emailSettings.SenderEmail, email)
            {
                Subject = subject,
                Body = message,
                IsBodyHtml = true // Đặt true nếu nội dung email của bạn là HTML
            };

            using var smtp = new SmtpClient(_emailSettings.SmtpHost, _emailSettings.SmtpPort)
            {
                Credentials = new NetworkCredential(_emailSettings.SenderEmail, _emailSettings.SenderPassword),
                EnableSsl = _emailSettings.EnableSsl
            };

            await smtp.SendMailAsync(mailMessage);
        }
    }
}