namespace NokiaHome.Settings;

public class OpenAiSettings
{
    /// <summary>
    /// Azure OpenAI endpoint, e.g. https://hilmar-slipper-foundry.openai.azure.com/
    /// Supplied via OpenAi__Endpoint environment variable.
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>Azure OpenAI API key. Supplied via OpenAi__ApiKey environment variable.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Deployed Whisper model name in Azure AI Foundry (e.g. "whisper").</summary>
    public string WhisperDeployment { get; set; } = "whisper";

    /// <summary>Deployed GPT model name in Azure AI Foundry (e.g. "gpt-4o-mini").</summary>
    public string ChatDeployment { get; set; } = "gpt-4o-mini";

    /// <summary>Azure OpenAI API version used for all requests.</summary>
    public string ApiVersion { get; set; } = "2024-12-01-preview";

    // Computed helpers
    public string WhisperUrl =>
        $"{Endpoint.TrimEnd('/')}/openai/deployments/{WhisperDeployment}/audio/transcriptions?api-version={ApiVersion}";

    public string ChatUrl =>
        $"{Endpoint.TrimEnd('/')}/openai/deployments/{ChatDeployment}/chat/completions?api-version={ApiVersion}";
}
