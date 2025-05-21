# Durable Task Model Context Protocol Server

An experimental MCP server that exposes Azure Durable Task resources to AI agents.

## Prerequisites

 - You have the .NET 9 SDK installed.
 - You have the Azure CLI installed.
 - You are logged into subscriptions via the Azure CLI.

## Use

### In VS Code

1. Open the repo folder in VS Code.
1. Open the `.vscode/mcp.json` file.
1. Select `Start` above the `durabletask-mcp-server` MCP server.
1. Open the Copilot Chat window and select `Agent` mode.
1. Ask questions like:

   "What schedulers do I have in my subscription?"
   
   "What orchestrations do I have in task hub `<name>`?

