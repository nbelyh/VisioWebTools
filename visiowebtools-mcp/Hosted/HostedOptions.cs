namespace VisioWebTools.Mcp.Hosted;

/// <summary>Settings of the hosted server, from the "Mcp" configuration section (env: Mcp__Name).</summary>
public sealed class HostedOptions
{
    /// <summary>
    /// The server's public address for links, without <see cref="BasePath"/> (e.g. https://example.com).
    /// Defaults to https on the app's host name in Azure, and to the current request's address elsewhere.
    /// </summary>
    public string? PublicBaseUrl { get; set; }

    /// <summary>
    /// Path the endpoints are mapped under, e.g. "/api" in Azure Functions (its route prefix). Links include it.
    /// </summary>
    public string BasePath { get; set; } = "";

    /// <summary>Azure Storage connection string. Defaults to the local Azurite emulator.</summary>
    public string StorageConnectionString { get; set; } = "UseDevelopmentStorage=true";

    public string Container { get; set; } = "mcp-files";

    /// <summary>
    /// Base64 key that signs upload and download links. If unset, a key is derived from
    /// <see cref="StorageConnectionString"/>: secret as long as it is, and the same on every instance.
    /// </summary>
    public string? SigningKey { get; set; }

    /// <summary>How long uploaded and result files are kept.</summary>
    public TimeSpan FileLifetime { get; set; } = TimeSpan.FromHours(1);

    /// <summary><see cref="FileLifetime"/> for people: "1 hour", "90 minutes".</summary>
    public string FileLifetimeText
    {
        get
        {
            var minutes = (int)Math.Round(FileLifetime.TotalMinutes);
            if (minutes >= 60 && minutes % 60 == 0)
                return minutes == 60 ? "1 hour" : $"{minutes / 60} hours";
            return minutes == 1 ? "1 minute" : $"{minutes} minutes";
        }
    }

    /// <summary>Running in Azure App Service or Azure Functions (not locally, not under func start).</summary>
    public static bool RunningInAzure => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID"));

    /// <summary>The storage emulator's well-known account: fine locally, never in Azure.</summary>
    public bool UsesDevelopmentStorage =>
        StorageConnectionString.Contains("UseDevelopmentStorage=true", StringComparison.OrdinalIgnoreCase)
        || StorageConnectionString.Contains("devstoreaccount1", StringComparison.OrdinalIgnoreCase);

    /// <summary>How long an upload link accepts files.</summary>
    public TimeSpan UploadLinkLifetime { get; set; } = TimeSpan.FromMinutes(30);

    public int MaxFileBytes { get; set; } = 25 * 1024 * 1024;

    public int MaxFilesPerUpload { get; set; } = 5;
}
