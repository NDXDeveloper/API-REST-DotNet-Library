using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

public class EmailService
{
    private readonly IConfiguration _configuration;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        var smtpClient = new SmtpClient(_configuration["EmailSettings:SmtpServer"])
        {
            //Port = int.Parse(_configuration["EmailSettings:Port"]),
            Port = int.TryParse(_configuration["EmailSettings:Port"], out int port) ? port : 25, // 25 ou un autre port par défaut
            Credentials = new NetworkCredential(
                _configuration["EmailSettings:Username"],
                _configuration["EmailSettings:Password"]
            ),
            EnableSsl = true,
        };

        var senderEmail = _configuration["EmailSettings:SenderEmail"];
        var senderName = _configuration["EmailSettings:SenderName"] ?? "Default Sender";

        if (string.IsNullOrEmpty(senderEmail))
        {
            throw new InvalidOperationException("Sender email configuration is missing.");
        }

        var mailMessage = new MailMessage
        {
            From = new MailAddress(senderEmail, senderName),
            Subject = subject,
            Body = body,
            IsBodyHtml = true,
        };

        mailMessage.To.Add(toEmail);

        await smtpClient.SendMailAsync(mailMessage);
    }
}
