namespace NokiaHome.Models
{
    public static class PrintJobStatus
    {
        public const string Queued = "queued";
        public const string Leased = "leased";
        public const string Done = "done";
        public const string Failed = "failed";
    }

    public class PrintJob
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = "message"; // message | bitmap
        public string Status { get; set; } = PrintJobStatus.Queued;
        public int Attempts { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? LeasedUntil { get; set; }
        public string? Error { get; set; }
        public string Align { get; set; } = "left";
        public bool Bold { get; set; }
        public int Feed { get; set; } = 2;
        public int Width { get; set; } = 384;
    }
}
