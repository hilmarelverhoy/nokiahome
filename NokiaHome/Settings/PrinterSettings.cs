namespace NokiaHome.Settings
{
    public class PrinterSettings
    {
        /// <summary>Secret for /api/printer/* (X-Device-Key). Supply via Printer__DeviceKey.</summary>
        public string DeviceKey { get; set; } = string.Empty;

        /// <summary>Secret for /api/print/* (X-Api-Key). Supply via Printer__ApiKey.</summary>
        public string ApiKey { get; set; } = string.Empty;

        public int LeaseSeconds { get; set; } = 60;
        public int MaxAttempts { get; set; } = 3;
        public int MaxPayloadBytes { get; set; } = 128 * 1024;
        public int RetentionDays { get; set; } = 7;

        /// <summary>Blob container holding the queue. Uses BlobStorage:ConnectionString.</summary>
        public string ContainerName { get; set; } = "printqueue";
    }
}
