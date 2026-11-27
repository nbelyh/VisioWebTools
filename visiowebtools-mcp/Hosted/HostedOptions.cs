namespace VisioWebTools.Mcp.Hosted;

/// <summary>Settings of the hosted server, from the "Mcp" configuration section (env: Mcp__Name).</summary>
public sealed class HostedOptions
{
    /// <summary>The server's public address, for links. Defaults to the address of the current request.</summary>
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

    /// <summary>How long an upload link accepts files.</summary>
    public TimeSpan UploadLinkLifetime { get; set; } = TimeSpan.FromMinutes(30);

    public int MaxFileBytes { get; set; } = 25 * 1024 * 1024;

    public int MaxFilesPerUpload { get; set; } = 5;
}
