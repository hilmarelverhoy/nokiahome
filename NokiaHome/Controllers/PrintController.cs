using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NokiaHome.Filters;
using NokiaHome.Models;
using NokiaHome.Services;
using NokiaHome.Settings;
using SixLabors.ImageSharp;

namespace NokiaHome.Controllers
{
    public record PrintMessageRequest(string? Text, string? Align, bool Bold = false, int Feed = 2);
    public record PrintQrRequest(string? Text, int Size = 300);

    [ApiController]
    [Route("api/print")]
    [SharedSecret(SharedSecretKind.Sender)]
    public class PrintController : ControllerBase
    {
        private readonly IPrintQueueService _queue;
        private readonly PrinterSettings _settings;

        public PrintController(IPrintQueueService queue, IOptions<PrinterSettings> settings)
        {
            _queue = queue;
            _settings = settings.Value;
        }

        [HttpPost("message")]
        public async Task<IActionResult> Message([FromBody] PrintMessageRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Text) || req.Text.Length > 4000) return BadRequest();
            var align = string.IsNullOrEmpty(req.Align) ? "left" : req.Align.ToLowerInvariant();
            if (align is not ("left" or "center" or "right")) return BadRequest();
            if (req.Feed < 0 || req.Feed > 10) return BadRequest();

            var payload = PrintRenderer.EncodeCp437(req.Text);
            if (payload.Length > _settings.MaxPayloadBytes) return StatusCode(413);

            var job = await _queue.EnqueueAsync(
                new PrintJob { Type = "message", Align = align, Bold = req.Bold, Feed = req.Feed }, payload);
            return Accepted(Queued(job));
        }

        [HttpPost("qr")]
        public async Task<IActionResult> Qr([FromBody] PrintQrRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Text) || req.Text.Length > 2000) return BadRequest();
            if (req.Size < 21 || req.Size > PrintRenderer.PrinterWidth) return BadRequest();

            byte[] payload;
            try { payload = PrintRenderer.RenderQr(req.Text, req.Size, _settings.MaxPayloadBytes); }
            catch (Exception) { return BadRequest(); }

            var job = await _queue.EnqueueAsync(
                new PrintJob { Type = "bitmap", Width = PrintRenderer.PrinterWidth }, payload);
            return Accepted(Queued(job));
        }

        [HttpPost("image")]
        public async Task<IActionResult> Image()
        {
            const int maxUpload = 10 * 1024 * 1024;
            if (Request.ContentLength > maxUpload) return StatusCode(413);

            using var ms = new MemoryStream();
            var buf = new byte[81920];
            int n;
            while ((n = await Request.Body.ReadAsync(buf)) > 0)
            {
                if (ms.Length + n > maxUpload) return StatusCode(413);
                ms.Write(buf, 0, n);
            }
            if (ms.Length == 0) return BadRequest();
            ms.Position = 0;

            byte[] payload;
            try { payload = PrintRenderer.RenderImage(ms, _settings.MaxPayloadBytes); }
            catch (Exception e) when (e is UnknownImageFormatException or InvalidImageContentException or NotSupportedException)
            { return BadRequest(); }

            var job = await _queue.EnqueueAsync(
                new PrintJob { Type = "bitmap", Width = PrintRenderer.PrinterWidth }, payload);
            return Accepted(Queued(job));
        }

        [HttpGet("jobs/{id}")]
        public async Task<IActionResult> GetJob(string id)
        {
            if (id.Length > 64) return NotFound();
            var job = await _queue.GetAsync(id);
            if (job == null) return NotFound();
            return Ok(new { job.Id, job.Type, job.Status, job.Attempts, job.CreatedAt, job.Error });
        }

        private static object Queued(PrintJob job) => new { id = job.Id, status = "queued" };
    }
}
