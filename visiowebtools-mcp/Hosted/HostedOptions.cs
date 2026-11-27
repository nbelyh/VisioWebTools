namespace VisioWebTools.Mcp.Hosted;

/// <summary>Settings of the hosted server, from the "Mcp" configuration section (env: Mcp__Name).</summary>
public sealed class HostedOptions
{
    /// <summary>The server's public address, for links. Defaults to the address of the current request.</summary>
    public string? PublicBaseUrl { get; set; }

    /// <summary>Azure Storage connection string. Defaults to the local Azurite emulator.</summary>
    public string StorageConnectionString { get; set; } = "UseDevelopmentStorage=true";

    public string Container { get; set; } = "mcp-files";

    /// <summary>
    /// Base64 key that signs upload and download links. Set it when more than one instance runs, or links
    /// made by one instance fail on another (and on restart). If unset, a random key is made at start.
    /// </summary>
    public string? SigningKey { get; set; }

    /// <summary>How long uploaded and result files are kept.</summary>
    public TimeSpan FileLifetime { get; set; } = TimeSpan.FromHours(1);

    /// <summary>How long an upload link accepts files.</summary>
    public TimeSpan UploadLinkLifetime { get; set; } = TimeSpan.FromMinutes(30);

    public int MaxFileBytes { get; set; } = 25 * 1024 * 1024;

    public int MaxFilesPerUpload { get; set; } = 5;
}
