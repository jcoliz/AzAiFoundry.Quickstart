namespace jcoliz.AI.Agents;

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

    /// <summary>
    /// Model name to use when creating a new agent
    /// </summary>
    public string? ModelName { get; init; } = null;

    /// <summary>
    ///  API Key for authentication
    ///  If not provided, the client will use the DefaultAzureCredential to authenticate.
    /// </summary>
    public string? ApiKey { get; init; } = null;
}
