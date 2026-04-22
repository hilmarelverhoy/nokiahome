namespace NokiaHome.Settings
{
    public class LinearSettings
    {
        /// <summary>
        /// Linear personal API key. Must be supplied via the Linear__ApiKey environment
        /// variable (or Azure App Setting) — never stored in appsettings.json.
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// The Linear team ID whose issues this app will display and manage.
        /// Set via the Linear__TeamId environment variable or Azure App Setting.
        /// </summary>
        public string TeamId { get; set; } = string.Empty;
    }
}
