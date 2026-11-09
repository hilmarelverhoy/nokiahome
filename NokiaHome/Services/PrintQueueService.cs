using Microsoft.Extensions.Options;
using NokiaHome.Models;
using NokiaHome.Settings;

namespace NokiaHome.Services
{
    public interface IPrintQueueService
    {
        Task<PrintJob> EnqueueAsync(PrintJob job, byte[] payload);
        Task<(PrintJob Job, byte[] Payload)?> LeaseNextAsync();
        /// <summary>Returns false if the job id is unknown. Idempotent.</summary>
        Task<bool> AckAsync(string id, bool success, string? error);
        Task<PrintJob?> GetAsync(string id);
    }

    public class PrintQueueService : IPrintQueueService
    {
        private const int MaxErrorLength = 200;
        private readonly IPrintJobStore _store;
        private readonly PrinterSettings _settings;
        private readonly TimeProvider _time;
        private DateTimeOffset _lastPurge = DateTimeOffset.MinValue;

        public PrintQueueService(IPrintJobStore store, IOptions<PrinterSettings> settings, TimeProvider? time = null)
        {
            _store = store;
            _settings = settings.Value;
            _time = time ?? TimeProvider.System;
        }

        public async Task<PrintJob> EnqueueAsync(PrintJob job, byte[] payload)
        {
            var now = _time.GetUtcNow();
            job.Id = $"{now.UtcDateTime:yyyyMMddHHmmssfff}{Guid.NewGuid():N}";
            job.Status = PrintJobStatus.Queued;
            job.Attempts = 0;
            job.CreatedAt = now;
            await _store.CreateAsync(job, payload);
            return job;
        }

        public async Task<(PrintJob Job, byte[] Payload)?> LeaseNextAsync()
        {
            var now = _time.GetUtcNow();
            var ids = await _store.ListIdsAsync();

            if (now - _lastPurge > TimeSpan.FromHours(1))
            {
                _lastPurge = now;
                await PurgeAsync(ids, now);
            }

            // Ids sort by creation time, so first queued is oldest.
            foreach (var id in ids)
            {
                var entry = await _store.GetAsync(id);
                if (entry == null) continue;
                var (job, etag) = entry.Value;

                if (job.Status == PrintJobStatus.Leased && job.LeasedUntil <= now)
                {
                    job.Attempts++;
                    job.LeasedUntil = null;
                    if (job.Attempts >= _settings.MaxAttempts)
                    {
                        job.Status = PrintJobStatus.Failed;
                        job.Error = "lease expired";
                    }
                    else
                    {
                        job.Status = PrintJobStatus.Queued;
                    }
                    var newTag = await _store.TryUpdateAsync(job, etag);
                    if (newTag == null) continue;
                    etag = newTag;
                }

                if (job.Status != PrintJobStatus.Queued) continue;

                job.Status = PrintJobStatus.Leased;
                job.LeasedUntil = now.AddSeconds(_settings.LeaseSeconds);
                if (await _store.TryUpdateAsync(job, etag) == null) continue; // someone else got it

                return (job, await _store.GetPayloadAsync(id));
            }
            return null;
        }

        public async Task<bool> AckAsync(string id, bool success, string? error)
        {
            for (var i = 0; i < 3; i++)
            {
                var entry = await _store.GetAsync(id);
                if (entry == null) return false;
                var (job, etag) = entry.Value;
                if (job.Status is PrintJobStatus.Done or PrintJobStatus.Failed) return true;

                job.Status = success ? PrintJobStatus.Done : PrintJobStatus.Failed;
                job.LeasedUntil = null;
                if (!success && !string.IsNullOrWhiteSpace(error))
                    job.Error = error.Length > MaxErrorLength ? error[..MaxErrorLength] : error;
                if (await _store.TryUpdateAsync(job, etag) != null) return true;
            }
            return true;
        }

        public async Task<PrintJob?> GetAsync(string id) => (await _store.GetAsync(id))?.Job;

        private async Task PurgeAsync(IReadOnlyList<string> ids, DateTimeOffset now)
        {
            var cutoff = now.AddDays(-_settings.RetentionDays);
            foreach (var id in ids)
            {
                var entry = await _store.GetAsync(id);
                if (entry != null && entry.Value.Job.CreatedAt < cutoff)
                    await _store.DeleteAsync(id);
            }
        }
    }
}
