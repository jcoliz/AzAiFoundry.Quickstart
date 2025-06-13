using Azure;
using Azure.Identity;
using Azure.AI.Agents.Persistent;

namespace AzAiFoundry.Quickstart.Options;

// https://learn.microsoft.com/en-us/dotnet/api/overview/azure/ai.agents.persistent-readme?view=azure-dotnet

/// <summary>
///  Client for interacting with a Persistent Agent in Azure AI Foundry.
///  This client allows you to create threads, send messages, and retrieve file content
/// </summary>
public class ChatClient
{
    private readonly PersistentAgentsClient _projectClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatClient"/> class.
    /// </summary>
    /// <param name="endpoint">The endpoint of the Azure AI Foundry project.</param>
    public ChatClient(string endpoint, string agentId)
    {
        _projectClient = new PersistentAgentsClient(endpoint, new DefaultAzureCredential());
        AgentId = agentId;
    }

    private string AgentId { get; }

    private async Task<PersistentAgent> GetAgentAsync()
    {
        return await _projectClient.Administration.GetAgentAsync(AgentId);
    }

    /// <summary>
    ///  Creates a new thread for the agent.
    ///  A thread is a conversation with the agent where messages can be sent and received.
    /// </summary>
    public async Task<PersistentAgentThread> CreateThreadAsync()
    {
        return await _projectClient.Threads.CreateThreadAsync();
    }

    /// <summary>
    /// Sends a message to the agent in a specific thread.
    /// The message is sent as a user message, and the agent will respond based on its configuration.
    /// </summary>
    /// <param name="thread">Thread containing the chat to send into</param>
    /// <param name="content">Message to send to agent</param>
    /// <returns>
    /// Messages in the thread, including the agent's response.
    /// </returns>
    public async Task<AsyncPageable<PersistentThreadMessage>> SendMessageAsync(
        PersistentAgentThread thread,
        string content)
    {
        await _projectClient.Messages.CreateMessageAsync(
            thread.Id,
            MessageRole.User,
            content);

        var agent = await GetAgentAsync();

        ThreadRun run = await _projectClient.Runs.CreateRunAsync(
            thread.Id,
            agent.Id
        );

        var startedAt = DateTime.UtcNow;
        do
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            run = await _projectClient.Runs.GetRunAsync(thread.Id, run.Id);

            var elapsed = DateTime.UtcNow - startedAt;
            Console.WriteLine("{0} Run Status: {1}", elapsed, run.Status);
        }
        while (run.Status == RunStatus.Queued
            || run.Status == RunStatus.InProgress);

        return _projectClient.Messages.GetMessagesAsync(
            threadId: thread.Id, order: ListSortOrder.Ascending);
    }

    public async Task<BinaryData> GetFileContentAsync(
        string fileId)
    {
        return await _projectClient.Files.GetFileContentAsync(fileId);
    }
}