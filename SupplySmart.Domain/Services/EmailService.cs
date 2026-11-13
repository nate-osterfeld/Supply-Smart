using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace SupplySmart.Domain.Services;

public interface IEmailService
{
    Task SendInviteEmailAsync(string toEmail, string fullName, string tempPassword);
    Task SendAlertEmailAsync(IEnumerable<string> toEmails, string subject, string markdownBody);
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

        var loginUrl = $"{baseUrl}";

        var mailMessage = new MailMessage
        {
            From = new MailAddress(senderEmail, senderName),
            Subject = "You've been invited to SupplySmart",
            IsBodyHtml = true,
            Body = $@"
            <div style='font-family: Arial, sans-serif; color: #172033; line-height: 1.5;'>
                <p>Hello {fullName},</p>
                <p>You have been invited to join SupplySmart.</p>
                <p>Your login email is: <strong>{toEmail}</strong></p>
                <p>Your temporary password is: <strong>{tempPassword}</strong></p>
                <p>Please log in and update your password by clicking the link below:</p>
                <p style='margin: 24px 0;'>
                    <a href='{loginUrl}' style='background-color: #172033; color: #ffffff; padding: 10px 20px; text-decoration: none; border-radius: 6px; font-weight: bold; display: inline-block;'>
                        Access SupplySmart
                    </a>
                </p>
                <p style='color: #8994a3; font-size: 12px;'>Or copy and paste this URL into your browser:<br>{loginUrl}</p>
            </div>"
        };
        mailMessage.To.Add(toEmail);

        await client.SendMailAsync(mailMessage);
    }

    public async Task SendAlertEmailAsync(IEnumerable<string> toEmails, string subject, string markdownBody)
    {
        var recipients = toEmails
            .Where(e => !string.IsNullOrWhiteSpace(e) && e.Contains('@'))
            .Select(e => e.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (recipients.Count == 0) return;

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

        var formattedHtmlContent = ConvertSimpleMarkdownToHtml(markdownBody);

        var mailMessage = new MailMessage
        {
            From = new MailAddress(senderEmail, senderName),
            Subject = subject,
            IsBodyHtml = true,
            Body = $@"
            <div style='font-family: Arial, sans-serif; color: #172033; line-height: 1.6; max-width: 600px; border: 1px solid #e1e6ec; border-radius: 12px; padding: 24px;'>
                {formattedHtmlContent}
                <hr style='border: none; border-top: 1px solid #edf0f3; margin: 24px 0;' />
                <p style='margin: 16px 0;'>
                    <a href='{baseUrl}' style='background-color: #172033; color: #ffffff; padding: 10px 18px; text-decoration: none; border-radius: 6px; font-weight: bold; font-size: 13px; display: inline-block;'>
                        Open SupplySmart Workspace →
                    </a>
                </p>
                <p style='color: #8994a3; font-size: 11px;'>Automated inventory alert dispatched by SupplySmart.</p>
            </div>"
        };

        foreach (var email in recipients)
        {
            mailMessage.To.Add(email);
        }

        await client.SendMailAsync(mailMessage);
    }

    private static string ConvertSimpleMarkdownToHtml(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown)) return string.Empty;

        var lines = markdown.Replace("\r\n", "\n").Split('\n');
        var htmlLines = new List<string>();

        foreach (var rawLine in lines)
        {
            var line = WebUtility.HtmlEncode(rawLine);

            // Bold **text**
            line = Regex.Replace(line, @"\*\*(.+?)\*\*", "<strong>$1</strong>");

            if (line.StartsWith("# "))
            {
                htmlLines.Add($"<h2 style='margin: 0 0 12px 0; color: #172033; font-size: 18px;'>{line.Substring(2)}</h2>");
            }
            else if (line.StartsWith("## "))
            {
                htmlLines.Add($"<h3 style='margin: 12px 0 8px 0; color: #172033; font-size: 15px;'>{line.Substring(3)}</h3>");
            }
            else if (string.IsNullOrWhiteSpace(line))
            {
                htmlLines.Add("<br/>");
            }
            else
            {
                htmlLines.Add($"<p style='margin: 4px 0; font-size: 14px;'>{line}</p>");
            }
        }

        return string.Join("\n", htmlLines);
    }
}