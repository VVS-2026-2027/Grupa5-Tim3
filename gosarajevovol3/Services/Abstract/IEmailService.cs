using System.Net;

namespace gosarajevovol3.Services.Abstract;

/// <summary>
/// Rezultat pokušaja slanja e-maila.
/// </summary>
public record EmailSendResult(bool Success, HttpStatusCode StatusCode, string Body);

/// <summary>
/// Apstrakcija slanja e-maila. Kontroleri zavise od ovog interfejsa, a ne od
/// konkretnog servisa (SendGrid), pa se u unit testovima može zamijeniti mock objektom.
/// </summary>
public interface IEmailService
{
    /// <summary>Šalje jednu poruku jednom primaocu.</summary>
    Task<EmailSendResult> SendAsync(string toEmail, string subject, string htmlContent, string plainTextContent);

    /// <summary>Šalje istu poruku na više adresa (primaoci su u skrivenoj kopiji).</summary>
    Task<EmailSendResult> SendToManyAsync(IEnumerable<string> recipients, string subject, string htmlContent, string plainTextContent);
}
