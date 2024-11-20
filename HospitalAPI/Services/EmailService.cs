using HospitalAPI.Models;
using Mailjet.Client;
using Mailjet.Client.TransactionalEmails;
using Microsoft.Extensions.Options;

namespace HospitalAPI.Services
{
    public interface IEmailService
    {
        Task SendVerificationEmailAsync(string toEmail, string code);
        Task SendPasswordResetEmailAsync(string toEmail, string newPassword);
    }

    public class EmailService : IEmailService
    {
        private readonly MailjetClient _client;
        private readonly MailjetSettings _settings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<MailjetSettings> settings, ILogger<EmailService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
            _client = new MailjetClient(_settings.ApiKey, _settings.ApiSecret);
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string newPassword)
        {
            try
            {
                var email = new TransactionalEmailBuilder()
                    .WithFrom(new SendContact(_settings.SenderEmail, _settings.SenderName))
                    .WithSubject("Password Reset")
                    .WithHtmlPart($@"
                    <h3>Password Reset</h3>
                    <p>Your password has been reset. Your new password is:</p>
                    <p style='font-size: 18px; font-weight: bold; padding: 10px; background-color: black; color:white;'>{newPassword}</p>
                    <p>For security reasons, we recommend changing this password after logging in.</p>
                    <p>If you did not request this password reset, please contact support immediately.</p>")
                    .WithTo(new SendContact(toEmail))
                    .Build();

                var response = await _client.SendTransactionalEmailAsync(email);

                if (!response.Messages.Any(m => m.Status == "success"))
                {
                    throw new Exception("Failed to send password reset email");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset email to {Email}", toEmail);
                throw new Exception("Failed to send password reset email", ex);
            }
        }

        public async Task SendVerificationEmailAsync(string toEmail, string code)
        {
            try
            {
                var email = new TransactionalEmailBuilder()
                    .WithFrom(new SendContact(_settings.SenderEmail, _settings.SenderName))
                    .WithSubject("Email Verification Code")
                    .WithHtmlPart($"<h3>Your verification code is: {code}</h3><p>This code will expire in 2 minutes.</p>")
                    .WithTo(new SendContact(toEmail))
                    .Build();

                var response = await _client.SendTransactionalEmailAsync(email);

                if (!response.Messages.Any(m => m.Status == "success"))
                {
                    throw new Exception("Failed to send email");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send verification email to {Email}", toEmail);
                throw new Exception("Failed to send verification email", ex);
            }
        }
    }

}
