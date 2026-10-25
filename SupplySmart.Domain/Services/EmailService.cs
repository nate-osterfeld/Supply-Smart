using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace SupplySmart.Domain.Services;

public interface IEmailService
{
    Task SendInviteEmailAsync(string toEmail, string fullName, string tempPassword);
}

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendInviteEmailAsync(string toEmail, string fullName, string tempPassword)
    {
        var host = _configuration["EmailSettings:Host"] ?? "localhost";
        var port = int.Parse(_configuration["EmailSettings:Port"] ?? "1025");
        var senderEmail = _configuration["EmailSettings:SenderEmail"] ?? "no-reply@supplysmart.io";
        var senderName = _configuration["EmailSettings:SenderName"] ?? "SupplySmart";
        var baseUrl = _configuration["EmailSettings:BaseUrl"] ?? "http://localhost:5173";
        var apiKey = _configuration["EmailSettings:ApiKey"];
        var username = _configuration["EmailSettings:Username"] ?? "apikey";

        using var client = new SmtpClient(host, port);

        if (!string.IsNullOrEmpty(apiKey))
        {
            client.Credentials = new NetworkCredential(username, apiKey);
            client.EnableSsl = true;
        }

        var loginUrl = $"{baseUrl}"; // TODO: add login route so url reads $"{baseUrl}/login"

        var mailMessage = new MailMessage
        {
            From = new MailAddress(senderEmail, senderName),
            Subject = "You've been invited to SupplySmart",
            IsBodyHtml = true,
            Body = $@"
            <p>Hello {fullName},</p>
            <p>You have been invited to join SupplySmart.</p>
            <p>Your temporary password is: <strong>{tempPassword}</strong></p>
            <p>Please log in and update your password
            <br>
            <br>
            <a href='{loginUrl}'>{loginUrl}</a></p>
            "
        };
        mailMessage.To.Add(toEmail);

        await client.SendMailAsync(mailMessage);
    }
}