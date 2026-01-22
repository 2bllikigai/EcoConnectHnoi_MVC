using EcoConnect_Hanoi.Models; // Đảm bảo bạn có lớp EmailSettings ở đây, hoặc điều chỉnh namespace cho EmailSettings
using Microsoft.Extensions.Options;
using System.Net; // Thêm namespace này
using System.Net.Mail; // Thêm namespace này
using System.Threading.Tasks;
using System; // Thêm namespace này cho InvalidOperationException, Console.WriteLine

namespace EcoConnect_Hanoi.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly EmailSettings _emailSettings;

        public EmailSender(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings.Value;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string message) // Đổi tham số 'email' thành 'toEmail' cho rõ ràng
        {
            // Kiểm tra các cài đặt cơ bản trước khi gửi
            if (string.IsNullOrEmpty(_emailSettings.SenderEmail) ||
                string.IsNullOrEmpty(_emailSettings.SenderPassword) ||
                string.IsNullOrEmpty(_emailSettings.SmtpHost) ||
                _emailSettings.SmtpPort == 0)
            {
                // Thay vì throw luôn, bạn có thể log lỗi và trả về Task.CompletedTask
                // hoặc throw một exception chi tiết hơn.
                Console.WriteLine("Lỗi cấu hình EmailSettings: Thông tin người gửi hoặc SMTP bị thiếu.");
                throw new InvalidOperationException("Email service is not configured correctly. Missing sender or SMTP details.");
            }

            // Tạo đối tượng MailMessage
            var mailMessage = new MailMessage
            {
                From = new MailAddress(_emailSettings.SenderEmail!), // Đảm bảo SenderEmail không null (dùng ! vì đã kiểm tra ở trên)
                Subject = subject,
                Body = message,
                IsBodyHtml = true // Quan trọng: Đặt true nếu nội dung email là HTML
            };
            mailMessage.To.Add(toEmail); // Thêm địa chỉ người nhận

            // Tạo đối tượng SmtpClient
            using (var smtp = new SmtpClient(_emailSettings.SmtpHost, _emailSettings.SmtpPort))
            {
                smtp.Credentials = new NetworkCredential(_emailSettings.SenderEmail, _emailSettings.SenderPassword);
                smtp.EnableSsl = _emailSettings.EnableSsl;
                smtp.DeliveryMethod = SmtpDeliveryMethod.Network; // Rất quan trọng
                smtp.UseDefaultCredentials = false; // Rất quan trọng, phải là false để dùng Credentials
                // smtp.Timeout = 20000; // Tùy chọn: Đặt timeout (20 giây)

                try
                {
                    await smtp.SendMailAsync(mailMessage);
                    Console.WriteLine($"Email đã gửi thành công tới: {toEmail}");
                }
                catch (SmtpException ex)
                {
                    Console.WriteLine($"Lỗi SMTP khi gửi email tới {toEmail}: {ex.StatusCode} - {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                    }
                    throw new InvalidOperationException($"Không thể gửi email tới {toEmail}. Lỗi SMTP: {ex.Message}", ex);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Lỗi không xác định khi gửi email tới {toEmail}: {ex.Message}");
                    throw; // Ném lại lỗi nếu không thể xử lý
                }
            }
        }
    }
}