using Azure;
using Azure.Identity;
using Azure.AI.Agents.Persistent;
using Azure.Core;

namespace jcoliz.AI.Agents;

// https://learn.microsoft.com/en-us/dotnet/api/overview/azure/ai.agents.persistent-readme?view=azure-dotnet

/// <summary>
///  Client for interacting with a Persistent Agent in Azure AI Foundry.
///  This client allows you to create threads, send messages, and retrieve file content
/// </summary>
public class ChatClient(AiFoundryOptions options, TokenCredential credential)
{
    // Fields
    private readonly PersistentAgentsClient _projectClient = new PersistentAgentsClient(options.Endpoint, credential);
    private readonly string AgentId = options.AgentId;

    /// <summary>
    ///  Creates a new thread for the agent.
    ///  A thread is a conversation with the agent where messages can be sent and received.
    /// </summary>
    public async Task<PersistentAgentThread> CreateThreadAsync()
    {
        return await _projectClient.Threads.CreateThreadAsync();
    }

    /// <summary>
    /// Sends a message to the agent in a specific thread and waits for the agent's response.
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

        await WaitForRunCompletionAsync(thread.Id, run.Id);

        return _projectClient.Messages.GetMessagesAsync(
            threadId: thread.Id, order: ListSortOrder.Ascending);
    }

    /// <summary>
    /// Waits for the agent run to complete, polling status and displaying progress.
    /// </summary>
    private async Task WaitForRunCompletionAsync(string threadId, string runId)
    {
        var startedAt = DateTime.UtcNow;
        ThreadRun run;
        do
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            run = await _projectClient.Runs.GetRunAsync(threadId, runId);

            var elapsed = DateTime.UtcNow - startedAt;
            Console.WriteLine("{0} Run Status: {1}", elapsed, run.Status);
        }
        while (run.Status == RunStatus.Queued
            || run.Status == RunStatus.InProgress);
    }

    /// <summary>
    ///  Displays a message from the thread in the console.
    ///  This method formats the message content and handles different types of content,
    /// </summary>
    /// <param name="threadMessage"></param>
    /// <returns></returns>
    public async Task DisplayMessageAsync(
        PersistentThreadMessage threadMessage)
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
                await DisplayImageContentAsync(imageFileItem.FileId);
            }
            Console.WriteLine();
        }
    }

    /// <summary>
    /// Downloads and displays image file content.
    /// </summary>
    private async Task DisplayImageContentAsync(string fileId)
    {
        Console.Write($"<image from ID: ./images/{fileId}.png");
        var result = await GetFileContentAsync(fileId);
        var stream = result.ToStream();
        Directory.CreateDirectory("images");
        File.Delete($"images/{fileId}.png");
        using (var fileStream = File.Create($"images/{fileId}.png"))
        {
            await stream.CopyToAsync(fileStream);
        }
    }

    // Private methods
    private async Task<PersistentAgent> GetAgentAsync()
    {
        return await _projectClient.Administration.GetAgentAsync(AgentId);
    }

    private async Task<BinaryData> GetFileContentAsync(
        string fileId)
    {
        return await _projectClient.Files.GetFileContentAsync(fileId);
    }
}