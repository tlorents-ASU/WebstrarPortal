using System.Xml.Linq;

namespace WebstrarPortal.Services;

public class CasTicketValidator
{
    private readonly HttpClient _http;
    private readonly string _casBaseUrl;

    public CasTicketValidator(HttpClient http, IConfiguration config)
    {
        _http = http;
        _casBaseUrl = config["CAS:BaseUrl"]
            ?? throw new InvalidOperationException("CAS:BaseUrl not configured");
    }

    /// <summary>
    /// Validates a CAS ticket and returns the authenticated ASURITE (or null if invalid).
    /// </summary>
    public async Task<string?> ValidateAsync(string ticket, string serviceUrl)
    {
        var validateUrl = $"{_casBaseUrl}/serviceValidate?ticket={Uri.EscapeDataString(ticket)}" +
                          $"&service={Uri.EscapeDataString(serviceUrl)}";

        var xml = await _http.GetStringAsync(validateUrl);

        // CAS 2.0 response is XML in the http://www.yale.edu/tp/cas namespace
        var doc = XDocument.Parse(xml);
        XNamespace cas = "http://www.yale.edu/tp/cas";

        var success = doc.Descendants(cas + "authenticationSuccess").FirstOrDefault();
        if (success == null)
            return null;

        var user = success.Element(cas + "user")?.Value?.Trim();
        return string.IsNullOrWhiteSpace(user) ? null : user.ToLowerInvariant();
    }

    /// <summary>
    /// Builds the CAS login redirect URL for the given service callback.
    /// </summary>
    public string BuildLoginUrl(string serviceUrl)
    {
        return $"{_casBaseUrl}/login?service={Uri.EscapeDataString(serviceUrl)}";
    }

    /// <summary>
    /// Builds the CAS logout URL, optionally redirecting back to a local page.
    /// </summary>
    public string BuildLogoutUrl(string? redirectUrl = null)
    {
        var url = $"{_casBaseUrl}/logout";
        if (!string.IsNullOrWhiteSpace(redirectUrl))
            url += $"?service={Uri.EscapeDataString(redirectUrl)}";
        return url;
    }
}
