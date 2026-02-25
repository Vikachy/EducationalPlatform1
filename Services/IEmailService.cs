using EducationalPlatform.Models;

namespace EducationalPlatform.Services
{
    public interface IEmailService
    {
        Task<bool> SendPasswordResetCodeAsync(string recipientEmail, string recipientName, string resetCode);
        Task<bool> SendSupportTicketEmailAsync(string recipientEmail, string subject, string body);
        Task<bool> SendNewsletterAsync(string toEmail, string subject, string body);
    }
}