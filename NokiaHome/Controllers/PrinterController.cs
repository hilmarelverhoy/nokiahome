using Microsoft.AspNetCore.Mvc;
using NokiaHome.Filters;
using NokiaHome.Services;

namespace NokiaHome.Controllers
{
    [ApiController]
    [Route("api/printer")]
    [SharedSecret(SharedSecretKind.Device)]
    public class PrinterController : ControllerBase
    {
        private readonly IPrintQueueService _queue;

        public PrinterController(IPrintQueueService queue) => _queue = queue;

        [HttpGet("next")]
        public async Task<IActionResult> Next()
        {
            var leased = await _queue.LeaseNextAsync();
            if (leased == null) return NoContent();

            var (job, payload) = leased.Value;
            Response.Headers["Cache-Control"] = "no-store";
            Response.Headers["X-Job-Id"] = job.Id;
            Response.Headers["X-Job-Type"] = job.Type;
            Response.Headers["X-Feed"] = job.Feed.ToString();
            if (job.Type == "message")
            {
                Response.Headers["X-Align"] = job.Align;
                Response.Headers["X-Bold"] = job.Bold ? "1" : "0";
            }
            else
            {
                Response.Headers["X-Width"] = job.Width.ToString();
            }
            Response.ContentLength = payload.Length;
            return File(payload, "application/octet-stream");
        }

        [HttpPost("jobs/{id}/ack")]
        public async Task<IActionResult> Ack(string id, [FromQuery] string? status, [FromQuery] string? error)
        {
            if (status != "done" && status != "failed") return BadRequest();
            if (id.Length > 64) return NotFound();
            return await _queue.AckAsync(id, status == "done", error) ? NoContent() : NotFound();
        }
    }
}
