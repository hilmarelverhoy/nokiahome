namespace NokiaHome.Settings
{
    public class GoogleMapsSettings
    {
        /// <summary>
        /// Google Maps API key. Supply via GoogleMaps__Key environment variable
        /// (or Azure App Setting) — never stored in appsettings.json.
        /// </summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// URL signing secret (modified Base64) for legacy maps.googleapis.com APIs.
        /// Supply via GoogleMaps__SigningSecret environment variable.
        /// Leave empty if your API key does not require signing.
        /// </summary>
        public string SigningSecret { get; set; } = string.Empty;
    }
}
