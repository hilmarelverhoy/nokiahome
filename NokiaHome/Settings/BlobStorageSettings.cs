namespace NokiaHome.Settings
{
    public class BlobStorageSettings
    {
        /// <summary>
        /// Azure Storage connection string. Must be supplied via the
        /// BlobStorage__ConnectionString environment variable (or Azure App Setting)
        /// — never stored in appsettings.json.
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// Name of the blob container to use. Defaults to "blobs".
        /// </summary>
        public string ContainerName { get; set; } = "blobs";
    }
}
