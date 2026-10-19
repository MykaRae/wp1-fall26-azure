// Add current stable Azure.Identity and Azure.Storage.Blobs NuGet packages.
// Register UploadStore as a service; invoke only after your app authenticates and authorizes the caller.
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

public sealed class UploadStore
{
    private readonly BlobContainerClient container;
    public UploadStore(string blobServiceUri, string containerName, string? runtimeClientId)
    {
        var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            ManagedIdentityClientId = runtimeClientId
        });
        container = new BlobServiceClient(new Uri(blobServiceUri), credential)
            .GetBlobContainerClient(containerName);
    }
    public async Task<string> SaveAsync(Stream content, long length, CancellationToken ct = default)
    {
        if (length < 1 || length > 5 * 1024 * 1024)
            throw new ArgumentOutOfRangeException(nameof(length), "Upload limit is 5 MiB.");
        // Also enforce request/stream length in the HTTP endpoint; never trust a client-supplied length alone.
        string key = Guid.NewGuid().ToString("N");
        await container.GetBlobClient(key).UploadAsync(content, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = "application/octet-stream" }
        }, ct);
        return key; // Save key plus authenticated owner ID in SQL. No public Blob URL is granted.
    }
    public async Task DownloadAsync(string key, Stream destination, CancellationToken ct = default)
    {
        // The calling endpoint must first verify this authenticated user owns/is allowed to read the key.
        await container.GetBlobClient(key).DownloadToAsync(destination, ct);
    }
}
