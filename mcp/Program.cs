using System.Reflection;
using AzAiFoundry.Quickstart.Mcp;
using Azure.AI.Agents.Persistent;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Tomlyn;

//
// This program creates a new agent, supplies it with a prompt and MCP server
// sends the agent a message, then waits for and displays the response.
//
// See: https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/ai/Azure.AI.Agents.Persistent/samples/Sample32_PersistentAgents_MCP.md
//

try
{
    var fileprovider = new EmbeddedFileProvider(Assembly.GetExecutingAssembly());
    var credential = new DefaultAzureCredential();

    //
    // Load configuration
    //

    using var stream = fileprovider.GetFileInfo("config.toml").CreateReadStream();
    using var reader = new StreamReader(stream);
    string toml = await reader.ReadToEndAsync();
    var config = Toml.ToModel<AppConfiguration>(toml);

    //
    // Create MCP tool definitions and configure allowed tools
    //

    var tools = config.Agent.McpServer.Select(mcpServer =>
    {
        // Create MCP tool definitions
        MCPToolDefinition mcpTool = new(mcpServer.Label, mcpServer.Endpoint);

        // Configure allowed tools (optional)
        foreach (var allowedTool in mcpServer.Tools)
        {
            mcpTool.AllowedTools.Add(allowedTool);
        }

        return mcpTool;
    });

    //
    // Create a client to interact with the Foundry project
    //

    var client = new PersistentAgentsClient(config.AiFoundry.Endpoint, credential);

    //
    // Use provided instructions, or read the instructions from the named embedded resource
    //

    var instructions = config.Agent.Instructions;
    if (instructions.StartsWith("@"))
    {
        var instructionFile = instructions[1..];
        using var instructionStream = fileprovider.GetFileInfo(instructionFile).CreateReadStream();
        using var instructionReader = new StreamReader(instructionStream);
        instructions = await instructionReader.ReadToEndAsync();
    }

    //
    // Create a new agent with the MCP tool
    //

    var agent = await client.Administration.CreateAgentAsync(
        model: config.Agent.Model ?? "gpt-4o",
        name: config.Agent.Name,
        instructions: instructions,
        tools: tools
    );

    //
    // Get a token to access the MCP server
    //

    var scopes = config.Agent.McpServer.SelectMany(mcpServer => mcpServer.Scopes).Distinct().ToArray();
    var tokenRequestContext = new TokenRequestContext(scopes);
    AccessToken token = await credential.GetTokenAsync(tokenRequestContext);

    //
    // Create a new thread and a message to the agent on that thread
    //

    PersistentAgentThread thread = await client.Threads.CreateThreadAsync();
    await client.Messages.CreateMessageAsync(
        thread.Id,
        MessageRole.User,
        config.Agent.DefaultUserMessage
    );

    //
    // Prepare the tool resources with the MCP server authorization
    //

    // NOTE: This only works with a single MCP server. To use multiple MCP servers,
    // we would need a custom ToolResources implementation that includes
    // all MCP servers and their associated tokens.
    MCPToolResource mcpToolResource = new(config.Agent.McpServer[0].Label);
    mcpToolResource.UpdateHeader("Authorization", $"Bearer {token.Token}");
    mcpToolResource.RequireApproval = new MCPApproval("never");
    ToolResources toolResources = mcpToolResource.ToToolResources();

    //
    // Run the agent with MCP tool resources
    //

    ThreadRun run = await client.Runs.CreateRunAsync(thread, agent, toolResources);

    var startedAt = DateTime.UtcNow;
    while (run.Status == RunStatus.Queued || run.Status == RunStatus.InProgress || run.Status == RunStatus.RequiresAction)
    {
        Console.WriteLine($"Run status: {run.Status}, started at {run.StartedAt}, duration {DateTime.UtcNow - startedAt}");
        await Task.Delay(TimeSpan.FromSeconds(1));

        run = await client.Runs.GetRunAsync(thread.Id, run.Id);
    }

    //
    // Display all messages in the thread
    //
    var messages = client.Messages.GetMessagesAsync(
        threadId: thread.Id,
        order: ListSortOrder.Ascending
    );

    await foreach (PersistentThreadMessage threadMessage in messages)
    {
        DisplayMessage(threadMessage);
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}

void DisplayMessage(PersistentThreadMessage threadMessage)
{
    Console.Write($"{threadMessage.CreatedAt:yyyy-MM-dd HH:mm:ss} - {threadMessage.Role,10}: ");
    foreach (MessageContent contentItem in threadMessage.ContentItems)
    {
        if (contentItem is MessageTextContent textItem)
        {
            Console.Write(textItem.Text);
        }
        else if (contentItem is MessageImageFileContent imageFileItem)
        {
            Console.Write($"<image from ID: {imageFileItem.FileId}>");
        }
        Console.WriteLine();
    }
}
