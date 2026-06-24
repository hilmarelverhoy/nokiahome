namespace NokiaHome.Settings
{
    public class GoogleMapsSettings
    {
        /// <summary>
        /// Google Maps API key. Supply via GoogleMaps__Key environment variable
        /// (or Azure App Setting) — never stored in appsettings.json.
        /// </summary>
        public string Key { get; set; } = string.Empty;
    }
}
