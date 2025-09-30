# Azure AI Foundry Quickstart with MCP Server

This is a quick sample to show how to run an agent in Azure AI Foundry using an MCP 
server, optionally with authentication via Azure Identity.

## Prerequisites

* Azure account
* Access to an [Azure AI Foundry]() project
* Azure CLI
* .NET 9.0 SDK

## Getting Started

1. Clone this repo locally
1. Create a `config.toml` file in your local `mcp` folder, providing the AI Foundry project endpoint you found above. Use the provided template [config.template.toml](./config.template.toml) as an example.
1. Open a terminal window in the local folder where you've cloned the repo
1. Log into the Azure CLI with your Woodgrove account, with `az login --tenant=<your_tenant>`. Confirm you're logged into your expected tenant with `az account show`.
1. Run the sample, using`dotnet run --project mcp`

```
Run status: queued, started at , duration 00:00:00.0007799
Run status: in_progress, started at 9/30/2025 9:35:00 PM +00:00, duration 00:00:01.4245151
Run status: in_progress, started at 9/30/2025 9:35:00 PM +00:00, duration 00:00:02.8479365
Run status: in_progress, started at 9/30/2025 9:35:00 PM +00:00, duration 00:00:04.2830373
Run status: in_progress, started at 9/30/2025 9:35:00 PM +00:00, duration 00:00:05.6137937
2025-09-30 21:34:59 -       user: Describe Azure AI Foundry in 50 words or less
2025-09-30 21:35:05 -  assistant: Azure AI Foundry is a unified enterprise-grade AI platform that developers can use to build, test, and deploy generative AI applications responsibly. It provides streamlined management, scalability, comprehensive tooling, and supports collaboration for the entire AI application lifecycle. Learn more [here](https://learn.microsoft.com/en-us/azure/ai-foundry/what-is-azure-ai-foundry).
```

## Next Steps

Now try changing the [agent definition](./Embed/agent.toml) to reach the MCP server of your choice.

Optionally, if your MCP server is protected by an Azure identity, uncomment the `scopes` line, and
provide the scopes needed for access. In this case, the sample will first fetch an access token
with the needed scopes.

## Future Improvements

This sample runs only on the command-line. A future improvement will be run in a browser, using
an InteractiveBrowser credential, so user can log in via UI.