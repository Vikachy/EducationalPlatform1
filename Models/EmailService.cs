using System.Net;
using System.Net.Mail;
using EducationalPlatform.Models;
using Microsoft.Maui.Storage;

namespace EducationalPlatform.Services
{
    public interface IEmailService
    {
        Task<bool> SendPasswordResetCodeAsync(string recipientEmail, string recipientName, string resetCode);
    }

    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;

        public EmailService()
        {
            _emailSettings = new EmailSettings
            {
                SmtpServer = "smtp.gmail.com",
                SmtpPort = 587,
                SenderEmail = "mituxina85@gmail.com",
                SenderName = "Educational Platform",
                SmtpUsername = "mituxina85@gmail.com",
                SmtpPassword = "uexa rvjo zcrb kvvx", // Пароль приложения Gmail
                EnableSsl = true
            };
        }

        public async Task<bool> SendPasswordResetCodeAsync(string recipientEmail, string recipientName, string resetCode)
        {
            var subject = "Код восстановления пароля - Educational Platform";

            var body = $@"Код восстановления пароля

Здравствуйте, {recipientName}!

Вы запросили восстановление пароля для вашего аккаунта в Educational Platform.

Ваш код подтверждения: {resetCode}

Код действителен в течение 60 минут.

Внимание:
• Никому не сообщайте этот код
• Если вы не запрашивали восстановление пароля, проигнорируйте это письмо

С уважением,
Команда Educational Platform

Это письмо отправлено автоматически. Пожалуйста, не отвечайте на него.";

            return await SendPlainTextEmailAsync(recipientEmail, subject, body);
        }

        private async Task<bool> SendPlainTextEmailAsync(string recipientEmail, string subject, string body)
        {
            try
            {
                using var smtpClient = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.SmtpPort);
                smtpClient.Credentials = new NetworkCredential(_emailSettings.SmtpUsername, _emailSettings.SmtpPassword);
                smtpClient.EnableSsl = _emailSettings.EnableSsl;
                smtpClient.Timeout = 30000;

                using var mailMessage = new MailMessage();
                mailMessage.From = new MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName);
                mailMessage.To.Add(recipientEmail);
                mailMessage.Subject = subject;
                mailMessage.Body = body;
                mailMessage.IsBodyHtml = false;

                await smtpClient.SendMailAsync(mailMessage);

                Console.WriteLine($"✅ Email отправлен на {recipientEmail}");
                return true;
            }
            catch (SmtpException ex)
            {
                Console.WriteLine($"❌ SMTP ошибка: {ex.StatusCode} - {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка отправки email: {ex.Message}");
                return false;
            }
        }
    }

    public class EmailSettings
    {
        public string SmtpServer { get; set; } = "smtp.gmail.com";
        public int SmtpPort { get; set; } = 587;
        public string SenderEmail { get; set; } = "mituxina85@gmail.com";
        public string SenderName { get; set; } = "Образовательная платформа";
        public string SmtpUsername { get; set; } = "mituxina85@gmail.com";
        public string SmtpPassword { get; set; } = "";
        public bool EnableSsl { get; set; } = true;
    }
}