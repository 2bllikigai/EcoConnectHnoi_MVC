// EcoConnect_Hanoi.Services/EmailSettings.cs
namespace EcoConnect_Hanoi.Services
{
    public class EmailSettings
    {
        public string SenderEmail { get; set; } = string.Empty;
        public string SenderPassword { get; set; } = string.Empty; // App password for Gmail
        public string SmtpHost { get; set; } = string.Empty;
        public int SmtpPort { get; set; }
        public bool EnableSsl { get; set; }
    }
}