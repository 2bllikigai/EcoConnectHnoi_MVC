// EcoConnect_Hanoi.Services/IEmailSender.cs
namespace EcoConnect_Hanoi.Services
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string email, string subject, string message);
    }
}