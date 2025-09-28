namespace AzAiFoundry.Quickstart.Mcp;

/// <summary>
/// Configuration controlling the behavior of the app
/// </summary>
public class AppConfiguration
{
    /// <summary>
    /// Foundry project configuration
    /// </summary>
    public AiFoundryConfiguration AiFoundry { get; init; } = new();

    /// <summary>
    /// Agent configuration
    /// </summary>
    public AgentConfiguration Agent { get; init; } = new();
}

public class AiFoundryConfiguration
{
    /// <summary>
    /// Foundry project configuration
    /// </summary>
    public string Endpoint { get; init; } = string.Empty;

}

public class AgentConfiguration
{
    /// <summary>
    /// Agent name
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Model name to use when creating a new agent
    /// </summary>
    public string? Model { get; init; } = null;

    /// <summary>
    /// Instructions for the agent
    /// </summary>
    public string Instructions { get; init; } = string.Empty;

    /// <summary>
    /// MCP server configurations
    /// </summary>
    public List<McpServerConfiguration> McpServer { get; init; } = new();

    /// <summary>
    /// Default user message to send to the agent
    /// </summary>
    public string DefaultUserMessage { get; init; } = string.Empty;
}

public class McpServerConfiguration
{
    /// <summary>
    /// MCP server label
    /// </summary>
    public string Label { get; init; } = string.Empty;

    /// <summary>
    /// MCP server endpoint
    /// </summary>
    public string Endpoint { get; init; } = string.Empty;

    /// <summary>
    /// List of allowed tools
    /// </summary>
    public List<string> Tools { get; init; } = new();

    /// <summary>
    /// List of scopes required to access the MCP server
    /// </summary>
    public List<string> Scopes { get; init; } = new();
}