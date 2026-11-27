using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace VisioWebTools.Mcp.Hosted;

/// <summary>
/// Signed, expiring links for uploads and downloads. The link is the credential (there are no user
/// accounts), so ids are random and every link carries an HMAC of what it grants and until when.
/// </summary>
public sealed partial class Links
{
    private readonly byte[] key;
    private readonly HostedOptions options;
    private readonly IHttpContextAccessor http;

    public Links(IOptions<HostedOptions> options, IHttpContextAccessor http)
    {
        this.options = options.Value;
        this.http = http;
        key = string.IsNullOrEmpty(this.options.SigningKey)
            ? HMACSHA256.HashData(Encoding.UTF8.GetBytes(this.options.StorageConnectionString), "visiowebtools-mcp/links"u8)
            : Convert.FromBase64String(this.options.SigningKey);
    }

    /// <summary>A new random id: 128 bits, base64url.</summary>
    public static string NewId() => Base64Url(RandomNumberGenerator.GetBytes(16));

    /// <summary>Ids are base64url, so they are safe in paths and blob names.</summary>
    public static bool IsValidId(string? id) => id is not null && IdPattern().IsMatch(id);

    [GeneratedRegex("^[A-Za-z0-9_-]{22,24}$")]
    private static partial Regex IdPattern();

    public string BaseUrl
    {
        get
        {
            if (!string.IsNullOrEmpty(options.PublicBaseUrl))
                return options.PublicBaseUrl.TrimEnd('/');
            var request = http.HttpContext?.Request ?? throw new InvalidOperationException("No request to take the server's address from; set Mcp:PublicBaseUrl.");
            return $"{request.Scheme}://{request.Host}{request.PathBase}{options.BasePath.TrimEnd('/')}";
        }
    }

    /// <summary>The server's origin, for the widget's content security policy.</summary>
    public string Origin => new Uri(BaseUrl).GetLeftPart(UriPartial.Authority);

    public string UploadUrl(string batch, DateTimeOffset expires) =>
        $"{BaseUrl}/files/upload/{batch}?{Query(Upload(batch), expires)}";

    public string DownloadUrl(string fileId, string name, DateTimeOffset expires) =>
        $"{BaseUrl}/files/{fileId}/{Uri.EscapeDataString(name)}?{Query(Download(fileId), expires)}";

    public bool VerifyUpload(string batch, string? exp, string? sig) => Verify(Upload(batch), exp, sig);

    public bool VerifyDownload(string fileId, string? exp, string? sig) => Verify(Download(fileId), exp, sig);

    private static string Upload(string batch) => $"upload|{batch}";
    private static string Download(string fileId) => $"download|{fileId}";

    private string Query(string grant, DateTimeOffset expires)
    {
        var exp = expires.ToUnixTimeSeconds().ToString();
        return $"exp={exp}&sig={Sign(grant, exp)}";
    }

    private bool Verify(string grant, string? exp, string? sig)
    {
        if (exp is null || sig is null || !long.TryParse(exp, out var seconds))
            return false;
        if (DateTimeOffset.FromUnixTimeSeconds(seconds) < DateTimeOffset.UtcNow)
            return false;
        return CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(Sign(grant, exp)), Encoding.ASCII.GetBytes(sig));
    }

    private string Sign(string grant, string exp) =>
        Base64Url(HMACSHA256.HashData(key, Encoding.UTF8.GetBytes($"{grant}|{exp}")));

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
