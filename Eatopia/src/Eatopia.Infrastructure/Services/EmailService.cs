using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Eatopia.Infrastructure.Services;

public class EmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> SendAsync(string toEmail, string subject, string body)
    {
        var host = _configuration["Email:SmtpHost"];
        var portText = _configuration["Email:SmtpPort"];
        var username = _configuration["Email:Username"];
        var password = _configuration["Email:Password"];
        var fromEmail = _configuration["Email:FromEmail"];
        var fromName = _configuration["Email:FromName"] ?? "Eatopia";

        // Many users type a display name like "Eatopia" in Username. Gmail SMTP needs the real Gmail address.
        // If Username is not an email, safely fall back to FromEmail.
        if (string.IsNullOrWhiteSpace(username) || !username.Contains('@'))
            username = fromEmail;

        if (string.IsNullOrWhiteSpace(fromEmail))
            fromEmail = username;

        if (string.IsNullOrWhiteSpace(toEmail))
        {
            _logger.LogWarning("Email receiver is empty. Notification email was not sent.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(portText) ||
            string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(fromEmail))
        {
            _logger.LogWarning("Email SMTP settings are incomplete. Skipping email to {ToEmail}. Check appsettings.json Email section.", toEmail);
            return false;
        }

        if (!int.TryParse(portText, out var port))
        {
            _logger.LogWarning("Email:SmtpPort value '{Port}' is invalid.", portText);
            return false;
        }

        try
        {
            using var client = new SmtpClient(host.Trim(), port)
            {
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                EnableSsl = bool.TryParse(_configuration["Email:EnableSsl"], out var ssl) ? ssl : true,
                Credentials = new NetworkCredential(username.Trim(), password.Trim())
            };

            using var message = new MailMessage
            {
                From = new MailAddress(fromEmail.Trim(), fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = false
            };

            message.To.Add(toEmail.Trim());

            await client.SendMailAsync(message);
            _logger.LogInformation("Notification email sent from {FromEmail} to {ToEmail} with subject {Subject}.", fromEmail, toEmail, subject);
            return true;
        }
        catch (SmtpException ex)
        {
            _logger.LogWarning(ex, "SMTP failed while sending notification email to {ToEmail}. Gmail requires Email:Username/FromEmail to be the Gmail address and Email:Password to be a Gmail App Password, not the normal account password.", toEmail);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unexpected email sending failure to {ToEmail}.", toEmail);
            return false;
        }
    }
}
