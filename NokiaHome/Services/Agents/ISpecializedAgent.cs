using System.Text.Json;

namespace NokiaHome.Services.Agents;

/// <summary>
/// A spoke in the hub-and-spoke voice command architecture.
/// Each implementation handles one category of user intent.
/// </summary>
public interface ISpecializedAgent
{
    /// <summary>
    /// The intent prefix this agent handles.
    /// Must match the prefix returned by the orchestrator, e.g. "calendar", "journal", "linear".
    /// </summary>
    string Intent { get; }

    /// <summary>
    /// Execute the command described by <paramref name="parameters"/> (already extracted by the hub).
    /// Returns a short human-readable Norwegian summary of what was done,
    /// e.g. "Hendelse 'Møte med Ola' opprettet 6. mai 2026 kl. 14:00".
    /// </summary>
    /// <param name="transcript">Original transcription, used as fallback body text.</param>
    /// <param name="parameters">Structured params extracted by GPT in the hub.</param>
    /// <param name="referenceNow">Request time, used for relative date resolution.</param>
    Task<string> ExecuteAsync(string transcript, JsonElement parameters, DateTime referenceNow);
}
