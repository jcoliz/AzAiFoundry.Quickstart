using System.Reflection;
using System.Runtime.CompilerServices;
using Azure.AI.Agents.Persistent;
using Azure.Core;
using Azure.Identity;
using jcoliz.AI.Agents;
using Microsoft.Extensions.Configuration;

//
// This program creates a new agent, supplies it with a prompt and MCP server
// sends the agent a message, then waits for and displays the response.
//
// See: https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/ai/Azure.AI.Agents.Persistent/samples/Sample32_PersistentAgents_MCP.md
//

//
// Load configuration
//

var configuration = new ConfigurationBuilder()
    .AddTomlFile("config.toml", optional: true)
    .Build();

var foundryOptions = new AiFoundryOptions();
configuration.Bind(AiFoundryOptions.Section, foundryOptions);

//
// Create MCP tool definitions and configure allowed tools
//

// Create MCP tool definitions
var mcpServerLabel = "SentinelDataExploration";
MCPToolDefinition mcpTool = new(mcpServerLabel, "https://sentinel.microsoft.com/mcp/data-exploration");

// Configure allowed tools (optional)
string searchApiCode = "query_lake";
mcpTool.AllowedTools.Add(searchApiCode);

//
// Create a client to interact with the Foundry project
//

var client = new PersistentAgentsClient(foundryOptions.Endpoint, new DefaultAzureCredential());

//
// Read the instructions from the embedded resource
// Using the EmbeddedFileProvider
//

var fileprovider = new Microsoft.Extensions.FileProviders.EmbeddedFileProvider(Assembly.GetExecutingAssembly());
using var stream = fileprovider.GetFileInfo("instructions.md").CreateReadStream();
using var reader = new StreamReader(stream);
string instructions = await reader.ReadToEndAsync();

//
// Create a new agent with the MCP tool
//

var agent = await client.Administration.CreateAgentAsync(
    model: foundryOptions.ModelName ?? "gpt-4o",
    name: "Infinity Mirror Summary Agent",
    instructions: instructions,
    tools: [ mcpTool ]
);

//
// Create a new thread and a message to the agent
//

PersistentAgentThread thread = await client.Threads.CreateThreadAsync();
PersistentThreadMessage message = await client.Messages.CreateMessageAsync(
    thread.Id,
    MessageRole.User,
    "Please generate the summary report as instructed.");

//
// Get a token to access the MCP server
//

var credential = new DefaultAzureCredential();
var tokenRequestContext = new TokenRequestContext(new[] { "4500ebfb-89b6-4b14-a480-7f749797bfcd" });
AccessToken token = await credential.GetTokenAsync(tokenRequestContext);

//
// Prepare the tool resources with the MCP server authorization
//

MCPToolResource mcpToolResource = new(mcpServerLabel);
mcpToolResource.UpdateHeader("Authorization", $"Bearer {token.Token}");
mcpToolResource.RequireApproval = new MCPApproval("never");
ToolResources toolResources = mcpToolResource.ToToolResources();

Console.WriteLine(token.Token);

// Run the agent with MCP tool resources
ThreadRun run = await client.Runs.CreateRunAsync(thread, agent, toolResources);

var startedAt = DateTime.UtcNow;
while (run.Status == RunStatus.Queued || run.Status == RunStatus.InProgress || run.Status == RunStatus.RequiresAction)
{
    Console.WriteLine($"Run status: {run.Status}, started at {run.StartedAt}, duration {DateTime.UtcNow - startedAt}");
    await Task.Delay(TimeSpan.FromSeconds(1));

    run = await client.Runs.GetRunAsync(thread.Id, run.Id);
}

var messages = client.Messages.GetMessagesAsync(
    threadId: thread.Id,
    order: ListSortOrder.Ascending
);

await foreach (PersistentThreadMessage threadMessage in messages)
{
    await DisplayMessageAsync(threadMessage);
}

async Task DisplayMessageAsync(PersistentThreadMessage threadMessage)
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