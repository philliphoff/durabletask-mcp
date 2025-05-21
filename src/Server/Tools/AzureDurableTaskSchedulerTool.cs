using System.ComponentModel;
using System.Reflection.Metadata.Ecma335;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.DurableTask;
using Azure.ResourceManager.Resources;
using Microsoft.Extensions.Configuration;
using ModelContextProtocol.Server;

namespace DurableTask.Mcp.Tools;

public sealed record AzureDurableTaskHub
{
    public required string Name { get; init; }

    public required Uri DashboardEndpoint { get; init; }
}

public sealed record AzureDurableTaskScheduler
{
    public required Uri Endpoint { get; init; }

    public required string Name { get; init; }

    public required string ResourceGroupName { get; init; }

    public required string SubscriptionId { get; init; }

    public AzureDurableTaskHub[] TaskHubs { get; init; } = [];
}

[McpServerToolType]
public static class AzureDurableTaskSchedulerTool
{
    [McpServerTool, Description("List all Durable Task Schedulers in Azure.")]
    public static async Task<AzureDurableTaskScheduler[]> GetSchedulersForSubscription(
        [Description("The ID of the subscription to query for schedulers.")] string subscriptionId)
    {
        ArmClient armClient = new ArmClient(new DefaultAzureCredential());

        SubscriptionResource defaultSubscription = armClient.GetSubscriptionResource(SubscriptionResource.CreateResourceIdentifier(subscriptionId));

        List<AzureDurableTaskScheduler> schedulers = new List<AzureDurableTaskScheduler>();

        await foreach (DurableTaskSchedulerResource resource in defaultSubscription.GetDurableTaskSchedulersAsync())
        {
            schedulers.Add(
                new AzureDurableTaskScheduler
                {
                    Endpoint = new Uri(resource.Data.Properties.Endpoint),
                    Name = resource.Data.Name,
                    ResourceGroupName = resource.Id.ResourceGroupName!,
                    SubscriptionId = resource.Id.SubscriptionId!,
                    TaskHubs =
                        resource.GetDurableTaskHubs().Select(hub => new AzureDurableTaskHub
                        {
                            Name = hub.Data.Name,
                            DashboardEndpoint = hub.Data.Properties.DashboardUri,
                        }).ToArray(),
                });
        }

        return schedulers.ToArray();
    }
}
