using System.Diagnostics.CodeAnalysis;
using gosarajevovol3.Services.Abstract;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace gosarajevovol3.Services.Concrete;

/// <summary>
/// Stvarno slanje e-maila preko SendGrid servisa.
/// Isključeno iz pokrivenosti koda jer zahtijeva mrežu i važeći API ključ.
/// </summary>
[ExcludeFromCodeCoverage]
public class SendGridEmailService : IEmailService
{
    private readonly IConfiguration _configuration;

    public SendGridEmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<EmailSendResult> SendAsync(string toEmail, string subject, string htmlContent, string plainTextContent)
    {
        var msg = CreateMessage(subject, htmlContent, plainTextContent);
        msg.AddTo(new EmailAddress(toEmail));
        return await SendMessageAsync(msg);
    }

    public async Task<EmailSendResult> SendToManyAsync(IEnumerable<string> recipients, string subject, string htmlContent, string plainTextContent)
    {
        var fromEmail = _configuration["SendGridSettings:FromEmail"];
        var fromName = _configuration["SendGridSettings:FromName"];

        var msg = CreateMessage(subject, htmlContent, plainTextContent);
        msg.AddTo(new EmailAddress(fromEmail, fromName));
        foreach (var email in recipients)
        {
            if (email.Equals(fromEmail, StringComparison.OrdinalIgnoreCase)) continue;
            msg.AddBcc(new EmailAddress(email));
        }
        return await SendMessageAsync(msg);
    }

    private SendGridMessage CreateMessage(string subject, string htmlContent, string plainTextContent)
    {
        var fromEmail = _configuration["SendGridSettings:FromEmail"];
        var fromName = _configuration["SendGridSettings:FromName"];
        return new SendGridMessage
        {
            From = new EmailAddress(fromEmail, fromName),
            Subject = subject,
            HtmlContent = htmlContent,
            PlainTextContent = plainTextContent
        };
    }

    private async Task<EmailSendResult> SendMessageAsync(SendGridMessage msg)
    {
        var client = new SendGridClient(_configuration["SendGridSettings:ApiKey"]);
        var response = await client.SendEmailAsync(msg);
        var body = response.IsSuccessStatusCode ? string.Empty : await response.Body.ReadAsStringAsync();
        return new EmailSendResult(response.IsSuccessStatusCode, response.StatusCode, body);
    }
}
