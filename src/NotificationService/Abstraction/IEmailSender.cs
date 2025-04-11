namespace EmailService.Abstraction;

public interface IEmailSender
{
    Task SendEmailAsync(string email, string subject, string body);
}