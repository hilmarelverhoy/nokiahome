using System.Text;
using System.Text.Json;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;
using NokiaHome.Models;
using NokiaHome.Settings;

namespace NokiaHome.Services;

/// <summary>
/// Persists journal entries as a single JSON blob at "journal/entries.json".
/// </summary>
public class JournalService : IJournalService
{
    private const string BlobName = "journal/entries.json";

    private readonly BlobContainerClient _container;
    private readonly ILogger<JournalService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public JournalService(IOptions<BlobStorageSettings> options, ILogger<JournalService> logger)
    {
        var settings = options.Value;
        var serviceClient = new BlobServiceClient(settings.ConnectionString);
        _container = serviceClient.GetBlobContainerClient(settings.ContainerName);
        _logger = logger;
    }

    public async Task<IReadOnlyList<JournalEntry>> GetEntriesAsync()
    {
        var entries = await LoadAsync();
        return entries.OrderByDescending(e => e.CreatedAt).ToList();
    }

    public async Task<JournalEntry?> GetEntryAsync(string id)
    {
        var entries = await LoadAsync();
        return entries.FirstOrDefault(e => e.Id == id);
    }

    public async Task AddEntryAsync(JournalEntry entry)
    {
        var entries = await LoadAsync();
        entries.Add(entry);
        await SaveAsync(entries);
    }

    public async Task DeleteEntryAsync(string id)
    {
        var entries = await LoadAsync();
        var removed = entries.RemoveAll(e => e.Id == id);
        if (removed > 0)
            await SaveAsync(entries);
    }

    // ---------------------------------------------------------------------------
    // Storage helpers
    // ---------------------------------------------------------------------------

    private async Task<List<JournalEntry>> LoadAsync()
    {
        try
        {
            var blob = _container.GetBlobClient(BlobName);
            var response = await blob.DownloadStreamingAsync();
            using var reader = new StreamReader(response.Value.Content);
            var json = await reader.ReadToEndAsync();
            return JsonSerializer.Deserialize<List<JournalEntry>>(json, JsonOptions) ?? [];
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "BlobNotFound")
        {
            return [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load journal entries from blob storage.");
            return [];
        }
    }

    private async Task SaveAsync(List<JournalEntry> entries)
    {
        var json = JsonSerializer.Serialize(entries, JsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);
        var blob = _container.GetBlobClient(BlobName);
        await blob.UploadAsync(
            new MemoryStream(bytes),
            new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = "application/json" } });
    }
}
