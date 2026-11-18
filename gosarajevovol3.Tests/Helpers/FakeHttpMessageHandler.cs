using System.Net;

namespace gosarajevovol3.Tests.Helpers;

/// <summary>
/// Zamjenski objekat za internet. HttpClient koji dobije ovaj handler ne šalje
/// ništa na mrežu, nego vraća unaprijed pripremljen odgovor.
/// Pravilo: prvi dodani odgovor čiji se tekst nalazi u URL-u zahtjeva se vraća.
/// </summary>
public class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly List<(string UrlPart, string Content, HttpStatusCode Status)> _responses = new();

    /// <summary>Svi URL-ovi koji su traženi, redom.</summary>
    public List<string> RequestedUrls { get; } = new();

    /// <summary>Vrijednost zaglavlja User-Agent iz posljednjeg zahtjeva.</summary>
    public string LastUserAgent { get; private set; } = string.Empty;

    public FakeHttpMessageHandler AddResponse(string urlPart, string content, HttpStatusCode status = HttpStatusCode.OK)
    {
        _responses.Add((urlPart, content, status));
        return this;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var url = request.RequestUri!.AbsoluteUri;
        RequestedUrls.Add(url);
        LastUserAgent = request.Headers.UserAgent.ToString();

        var match = _responses.FirstOrDefault(r => url.Contains(r.UrlPart));
        var response = match.UrlPart == null
            ? new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent(string.Empty) }
            : new HttpResponseMessage(match.Status) { Content = new StringContent(match.Content) };

        return Task.FromResult(response);
    }
}
