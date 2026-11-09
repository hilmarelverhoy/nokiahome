using FluentAssertions;
using Microsoft.Extensions.Options;
using NokiaHome.Models;
using NokiaHome.Services;
using NokiaHome.Settings;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace NokiaHome.Tests.Services;

public class PrintQueueServiceTests
{
    private class FakeTime : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private class MemStore : IPrintJobStore
    {
        private readonly SortedDictionary<string, (PrintJob Job, int Ver)> _jobs = new(StringComparer.Ordinal);
        private readonly Dictionary<string, byte[]> _payloads = new();
        private static PrintJob Clone(PrintJob j) => System.Text.Json.JsonSerializer.Deserialize<PrintJob>(System.Text.Json.JsonSerializer.Serialize(j))!;
        public Task CreateAsync(PrintJob job, byte[] payload) { _jobs[job.Id] = (Clone(job), 1); _payloads[job.Id] = payload; return Task.CompletedTask; }
        public Task<IReadOnlyList<string>> ListIdsAsync() => Task.FromResult<IReadOnlyList<string>>(_jobs.Keys.ToList());
        public Task<(PrintJob, string)?> GetAsync(string id) =>
            Task.FromResult<(PrintJob, string)?>(_jobs.TryGetValue(id, out var e) ? (Clone(e.Job), e.Ver.ToString()) : null);
        public Task<string?> TryUpdateAsync(PrintJob job, string etag)
        {
            if (!_jobs.TryGetValue(job.Id, out var e) || e.Ver.ToString() != etag) return Task.FromResult<string?>(null);
            _jobs[job.Id] = (Clone(job), e.Ver + 1);
            return Task.FromResult<string?>((e.Ver + 1).ToString());
        }
        public Task<byte[]> GetPayloadAsync(string id) => Task.FromResult(_payloads[id]);
        public Task DeleteAsync(string id) { _jobs.Remove(id); _payloads.Remove(id); return Task.CompletedTask; }
    }

    private readonly FakeTime _time = new();
    private readonly PrintQueueService _q;

    public PrintQueueServiceTests() =>
        _q = new PrintQueueService(new MemStore(), Options.Create(new PrinterSettings()), _time);

    [Fact]
    public async Task Leases_oldest_first_one_at_a_time()
    {
        var a = await _q.EnqueueAsync(new PrintJob(), [1]);
        _time.Now = _time.Now.AddSeconds(1);
        var b = await _q.EnqueueAsync(new PrintJob(), [2]);

        (await _q.LeaseNextAsync())!.Value.Job.Id.Should().Be(a.Id);
        (await _q.LeaseNextAsync())!.Value.Job.Id.Should().Be(b.Id);
        (await _q.LeaseNextAsync()).Should().BeNull();
    }

    [Fact]
    public async Task Expired_lease_requeues_same_id_with_attempts_incremented()
    {
        var a = await _q.EnqueueAsync(new PrintJob(), [1]);
        await _q.LeaseNextAsync();
        _time.Now = _time.Now.AddSeconds(61);

        var again = (await _q.LeaseNextAsync())!.Value.Job;
        again.Id.Should().Be(a.Id);
        again.Attempts.Should().Be(1);
    }

    [Fact]
    public async Task Fails_after_three_attempts()
    {
        var a = await _q.EnqueueAsync(new PrintJob(), [1]);
        for (var i = 0; i < 3; i++)
        {
            (await _q.LeaseNextAsync()).Should().NotBeNull();
            _time.Now = _time.Now.AddSeconds(61);
        }
        (await _q.LeaseNextAsync()).Should().BeNull();
        var job = (await _q.GetAsync(a.Id))!;
        job.Status.Should().Be("failed");
        job.Error.Should().Be("lease expired");
    }

    [Fact]
    public async Task Ack_is_idempotent_and_unknown_id_is_false()
    {
        var a = await _q.EnqueueAsync(new PrintJob(), [1]);
        await _q.LeaseNextAsync();
        (await _q.AckAsync(a.Id, true, null)).Should().BeTrue();
        (await _q.AckAsync(a.Id, false, "x")).Should().BeTrue();
        (await _q.GetAsync(a.Id))!.Status.Should().Be("done");
        (await _q.AckAsync("nope", true, null)).Should().BeFalse();
    }

    [Fact]
    public async Task Old_jobs_are_purged()
    {
        var a = await _q.EnqueueAsync(new PrintJob(), [1]);
        _time.Now = _time.Now.AddDays(8);
        await _q.LeaseNextAsync();
        (await _q.GetAsync(a.Id)).Should().BeNull();
    }
}

public class PrintRendererTests
{
    [Fact]
    public void Cp437_replaces_unencodable_characters()
    {
        var bytes = PrintRenderer.EncodeCp437("æ😀\r\nx");
        bytes[0].Should().Be(0x91);
        bytes.Last().Should().Be((byte)'x');
        bytes.Should().Contain((byte)'\n').And.NotContain((byte)'\r');
    }

    [Fact]
    public void Pack_is_msb_first_with_one_as_black()
    {
        using var img = new Image<L8>(8, 1, new L8(255));
        img[0, 0] = new L8(0);
        PrintRenderer.Pack(img, 1000).Should().Equal(0x80);
    }

    [Fact]
    public void Qr_is_384_wide_and_within_limit()
    {
        var data = PrintRenderer.RenderQr("https://example.com", 300, 131072);
        (data.Length % 48).Should().Be(0);
        data.Length.Should().BeLessThanOrEqualTo(131072);
    }

    [Fact]
    public void Tall_images_are_truncated()
    {
        using var img = new Image<Rgba32>(100, 5000, new Rgba32(0, 0, 0, 255));
        using var ms = new MemoryStream();
        img.SaveAsPng(ms);
        ms.Position = 0;
        PrintRenderer.RenderImage(ms, 131072).Length.Should().BeLessThanOrEqualTo(131072);
    }
}
