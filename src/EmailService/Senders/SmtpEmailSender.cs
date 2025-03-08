using System.Net;
using System.Net.Mail;
using EmailService.Abstraction;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;

namespace EmailService.Senders;

public class SmtpEmailSender : IEmailSender
{
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<SmtpEmailSender> _logger;
    private readonly AsyncRetryPolicy _retryPolicy;

    public SmtpEmailSender(IOptions<EmailSettings> emailSettings, ILogger<SmtpEmailSender> logger)
    {
        _emailSettings = emailSettings.Value;
        _logger = logger;

        // Define a Polly retry policy with exponential backoff (2, 4, 8 seconds)
        _retryPolicy = Policy
            .Handle<SmtpException>()  // Retry on SMTP failures
            .Or<TaskCanceledException>()  // Retry if the SMTP server is unreachable
            .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)), (exception, time, retryCount, context) =>
            {
                logger.LogWarning($"Retry {retryCount} for sending email to {context["recipient"]}. Error: {exception.Message}");
            });
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        await _retryPolicy.ExecuteAsync(async (context) =>
        {
            try
            {
                var message = new MailMessage
                {
                    From = new MailAddress(_emailSettings.From),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };
                message.To.Add(new MailAddress(to));

                using var smtpClient = new SmtpClient(_emailSettings.Host, _emailSettings.Port)
                {
                    Credentials = new NetworkCredential(_emailSettings.Username, _emailSettings.Password),
                    EnableSsl = _emailSettings.EnableSsl
                };
            
                await smtpClient.SendMailAsync(message);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Email sending failed: {ex.Message}");
                throw; // Polly will retry if this exception is in the handled exceptions list
            }
        }, new Context { { "recipient", to } }); // Pass metadata for logging
    }
}