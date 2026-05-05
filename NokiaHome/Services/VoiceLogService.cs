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
/// Persists voice command logs as a single JSON blob at "voice/commands.json".
/// Each entry captures the transcript, the action taken, and optional user feedback,
/// providing a dataset for future prompt refinement.
/// </summary>
public class VoiceLogService : IVoiceLogService
{
    private const string BlobName = "voice/commands.json";

    private readonly BlobContainerClient _container;
    private readonly ILogger<VoiceLogService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public VoiceLogService(IOptions<BlobStorageSettings> options, ILogger<VoiceLogService> logger)
    {
        var settings = options.Value;
        var serviceClient = new BlobServiceClient(settings.ConnectionString);
        _container = serviceClient.GetBlobContainerClient(settings.ContainerName);
        _logger = logger;
    }

    public async Task<IReadOnlyList<VoiceCommandLog>> GetLogsAsync()
    {
        var logs = await LoadAsync();
        return logs.OrderByDescending(l => l.CreatedAt).ToList();
    }

    public async Task<VoiceCommandLog?> GetLogAsync(string id)
    {
        var logs = await LoadAsync();
        return logs.FirstOrDefault(l => l.Id == id);
    }

    public async Task AddLogAsync(VoiceCommandLog entry)
    {
        var logs = await LoadAsync();
        logs.Add(entry);
        await SaveAsync(logs);
    }

    public async Task SaveActionFeedbackAsync(string logId, int actionIndex, string rating, string? comment)
    {
        var logs  = await LoadAsync();
        var entry = logs.FirstOrDefault(l => l.Id == logId);

        if (entry is null)
        {
            _logger.LogWarning("VoiceLogService: no entry found for id {Id}", logId);
            return;
        }

        if (actionIndex < 0 || actionIndex >= entry.Actions.Count)
        {
            _logger.LogWarning("VoiceLogService: action index {Index} out of range for log {Id}", actionIndex, logId);
            return;
        }

        var action = entry.Actions[actionIndex];
        action.FeedbackRating  = rating;
        action.FeedbackComment = comment?.Trim();
        action.FeedbackAt      = DateTime.UtcNow;

        await SaveAsync(logs);
    }

    private async Task<List<VoiceCommandLog>> LoadAsync()
    {
        try
        {
            var blob = _container.GetBlobClient(BlobName);
            var response = await blob.DownloadStreamingAsync();
            using var reader = new StreamReader(response.Value.Content);
            var json = await reader.ReadToEndAsync();
            return JsonSerializer.Deserialize<List<VoiceCommandLog>>(json, JsonOptions) ?? [];
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "BlobNotFound")
        {
            return [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load voice command logs from blob storage.");
            return [];
        }
    }

    private async Task SaveAsync(List<VoiceCommandLog> logs)
    {
        var json  = JsonSerializer.Serialize(logs, JsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);
        var blob  = _container.GetBlobClient(BlobName);
        await blob.UploadAsync(
            new MemoryStream(bytes),
            new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = "application/json" } });
    }
}
