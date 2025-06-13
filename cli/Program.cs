using Azure.AI.Agents.Persistent;
using Microsoft.Extensions.Configuration;
using AzAiFoundry.Quickstart.Options;

//
// Load configuration
//

var configuration = new ConfigurationBuilder()
    .AddTomlFile("config.toml", optional: true)
    .Build();

var foundryOptions = new AiFoundryOptions();
configuration.Bind(AiFoundryOptions.Section, foundryOptions);

//
// Create a client to interact with the Foundry project
//

var client = new ChatClient(foundryOptions.Endpoint, foundryOptions.AgentId);

//
// Create a new thread and send a message to the agent
//

var thread = await client.CreateThreadAsync();
var message = args.Length > 0 ? args[0] : "Please describe your function.";
var results = await client.SendMessageAsync(thread, message);

//
// Display the results returned by the agent
//

await foreach (PersistentThreadMessage threadMessage in results)
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

            var result = await client.GetFileContentAsync(imageFileItem.FileId);
            var stream = result.ToStream();
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
