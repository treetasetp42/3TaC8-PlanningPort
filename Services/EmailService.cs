using System.Net;
using System.Net.Mail;
using _3TaC8_PlanningPort.Models;
using Microsoft.Extensions.Options;

namespace _3TaC8_PlanningPort.Services
{
    public class EmailService : IEmailService
    {
        private readonly SmtpSettings _settings;

        public EmailService(IOptions<SmtpSettings> settings)
        {
            _settings = settings.Value;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            using var client = new SmtpClient(_settings.Server, _settings.Port)
            {
                Credentials = new NetworkCredential(_settings.Username, _settings.Password),
                EnableSsl = true
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_settings.SenderEmail, _settings.SenderName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            mailMessage.To.Add(to);

            await client.SendMailAsync(mailMessage);
        }

        public async Task SendPasswordResetEmailAsync(string to, string resetLink)
        {
            const string subject = "Password Reset Request - Invest Planner";
            string body = $@"
                <div style='font-family: sans-serif; padding: 20px; text-align: center; color: #333;'>
                    <h2 style='color: #1976d2;'>Invest Planner</h2>
                    <p>You requested to reset your password. Click the button below to proceed:</p>
                    <a href='{resetLink}' style='display: inline-block; padding: 12px 24px; background-color: #1976d2; color: white; text-decoration: none; border-radius: 8px; font-weight: bold; margin: 20px 0;'>Reset Password</a>
                    <p style='font-size: 0.8rem; color: #777;'>If you did not request this, please ignore this email.</p>
                </div>";

            await SendEmailAsync(to, subject, body);
        }
    }
}
