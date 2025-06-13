namespace AzAiFoundry.Quickstart.Options;

/// <summary>
/// Options describing the identity of the app
/// </summary>
public class AiFoundryOptions
{
    /// <summary>
    /// Config file section
    /// </summary>
    public static readonly string Section = "AiFoundry";

    /// <summary>
    /// Foundry project endpoint
    /// </summary>
    public string Endpoint { get; init; } = string.Empty;

    /// <summary>
    /// Agent ID
    /// </summary>
    public string AgentId { get; init; } = string.Empty;
}
