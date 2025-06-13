using Azure;
using Azure.Identity;
using Azure.AI.Agents.Persistent;
using Microsoft.Extensions.Configuration;
using AzAiFoundry.Quickstart.Options;
using Microsoft.Extensions.Azure;

//
// Load configuration
//

var configuration = new ConfigurationBuilder()
    .AddTomlFile("config.toml", optional: true)
    .Build();

var foundryOptions = new AiFoundryOptions();
configuration.Bind(AiFoundryOptions.Section, foundryOptions);

// https://learn.microsoft.com/en-us/dotnet/api/overview/azure/ai.agents.persistent-readme?view=azure-dotnet

PersistentAgentsClient projectClient = new(foundryOptions.Endpoint, new DefaultAzureCredential());

var agents = projectClient.Administration.GetAgentsAsync();
await foreach (var each in agents)
{
    Console.WriteLine("Agent ID: {0}, Name: {1}", each.Id, each.Name);
}

var myagent = await projectClient.Administration.GetAgentAsync(foundryOptions.AgentId);

PersistentAgentThread thread = await projectClient.Threads.CreateThreadAsync();

await projectClient.Messages.CreateMessageAsync(
    thread.Id,
    MessageRole.User,
    args.Length > 0 ? args[0] :
    "Please describe your function.");

ThreadRun run = await projectClient.Runs.CreateRunAsync(
    thread.Id,
    myagent.Value.Id
);

var startedAt = DateTime.UtcNow;
do
{
    await Task.Delay(TimeSpan.FromSeconds(1));
    run = await projectClient.Runs.GetRunAsync(thread.Id, run.Id);

    var elapsed = DateTime.UtcNow - startedAt;
    Console.WriteLine("{0} Run Status: {1}", elapsed, run.Status);
}
while (run.Status == RunStatus.Queued
    || run.Status == RunStatus.InProgress);

AsyncPageable<PersistentThreadMessage> messages
    = projectClient.Messages.GetMessagesAsync(
        threadId: thread.Id, order: ListSortOrder.Ascending);

await foreach (PersistentThreadMessage threadMessage in messages)
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
            Console.Write($"<image from ID: ./images/{imageFileItem.FileId}.png");

            var result = await projectClient.Files.GetFileContentAsync(imageFileItem.FileId);
            if (result.GetRawResponse().Status != 200)
            {
                Console.Write(" (error retrieving image)");
                continue;
            }
            var stream = result.Value.ToStream();
            Directory.CreateDirectory("images");
            File.Delete($"images/{imageFileItem.FileId}.png");
            using (var fileStream = File.Create($"images/{imageFileItem.FileId}.png"))
            {
                await stream.CopyToAsync(fileStream);
            }
        }
        Console.WriteLine();
    }
}
