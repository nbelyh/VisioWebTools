using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;

namespace VisioWebTools.Mcp.Hosted;

public sealed record StoredFile(string Id, string Name, byte[] Content, DateTimeOffset Created);

/// <summary>
/// Uploaded files and results, in Azure Blob Storage, kept for <see cref="HostedOptions.FileLifetime"/>.
/// Expired files read as missing; a lifecycle rule on the container deletes them for good.
/// </summary>
public sealed class FileStore
{
    private readonly BlobContainerClient container;
    private readonly HostedOptions options;
    private readonly Lazy<Task> created;

    public FileStore(IOptions<HostedOptions> options)
    {
        this.options = options.Value;
        container = new BlobContainerClient(this.options.StorageConnectionString, this.options.Container);
        created = new Lazy<Task>(() => container.CreateIfNotExistsAsync());
    }

    public TimeSpan Lifetime => options.FileLifetime;

    /// <summary>Stores a file under a new id. False if the id is taken: each id is written once.</summary>
    public async Task<bool> TryAddAsync(string id, string name, byte[] content, CancellationToken ct = default)
    {
        await created.Value;
        try
        {
            await container.GetBlobClient(id).UploadAsync(new BinaryData(content), new BlobUploadOptions
            {
                Conditions = new BlobRequestConditions { IfNoneMatch = ETag.All },
                Metadata = new Dictionary<string, string> { ["name"] = Uri.EscapeDataString(name) },
            }, ct);
            return true;
        }
        catch (RequestFailedException ex) when (ex.Status is 409 or 412)
        {
            return false;
        }
    }

    /// <summary>A stored file, or null if there is none or it expired.</summary>
    public async Task<StoredFile?> GetAsync(string id, CancellationToken ct = default)
    {
        if (!Links.IsValidId(id))
            return null;
        await created.Value;
        var blob = container.GetBlobClient(id);
        try
        {
            var result = await blob.DownloadContentAsync(ct);
            var details = result.Value.Details;
            var createdOn = details.CreatedOn;
            if (createdOn + options.FileLifetime < DateTimeOffset.UtcNow)
            {
                await blob.DeleteIfExistsAsync(cancellationToken: ct);
                return null;
            }
            var name = details.Metadata.TryGetValue("name", out var n) ? Uri.UnescapeDataString(n) : id;
            return new StoredFile(id, name, result.Value.Content.ToArray(), createdOn);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    /// <summary>Deletes expired files. Returns how many.</summary>
    public async Task<int> DeleteExpiredAsync(CancellationToken ct = default)
    {
        await created.Value;
        var cutoff = DateTimeOffset.UtcNow - options.FileLifetime;
        var deleted = 0;
        await foreach (var blob in container.GetBlobsAsync(cancellationToken: ct))
        {
            if (blob.Properties.CreatedOn < cutoff)
            {
                await container.DeleteBlobIfExistsAsync(blob.Name, cancellationToken: ct);
                deleted++;
            }
        }
        return deleted;
    }
}

/// <summary>Deletes expired files every few minutes, so they are gone soon after their hour.</summary>
public sealed class FileCleanup(FileStore store, ILogger<FileCleanup> log) : BackgroundService
{
    public static readonly TimeSpan Interval = TimeSpan.FromMinutes(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        do
        {
            try
            {
                var n = await store.DeleteExpiredAsync(stoppingToken);
                if (n > 0)
                    log.LogInformation("Deleted {Count} expired files.", n);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                log.LogWarning(ex, "Deleting expired files failed; trying again later.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
