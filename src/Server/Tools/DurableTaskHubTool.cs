using System.ComponentModel;
using Microsoft.DurableTask.Client;
using Microsoft.DurableTask.Client.AzureManaged;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;

public sealed record TaskHubOrchestration
{
    public required string InstanceId { get; init; }

    public required string Name { get; init; }
}

[McpServerToolType]
public static class DurableTaskHubTool
{
    [McpServerTool, Description("List orchestrations in a Durable Task Scheduler Task Hub.")]
    public static async Task<TaskHubOrchestration[]> GetOrchestrationsForTaskHub(
        [Description("The name of the task hub to query for orchestrations.")] string taskHubName,
        [Description("The endpoint of the scheduler for the task hub.")] Uri schedulerEndpoint)
    {
        var services = new ServiceCollection();

        services.AddLogging(_ => { });

        var builder = new DefaultDurableTaskClientBuilder("client", services);

        builder.UseDurableTaskScheduler($"Endpoint={schedulerEndpoint};TaskHub={taskHubName};Authentication=DefaultAzure");

        using var serviceProvider = services.BuildServiceProvider();

        var client = builder.Build(serviceProvider);

        List<TaskHubOrchestration> orchestrations = new();

        var instances = client.GetAllInstancesAsync();

        await foreach (var instance in instances)
        {
            orchestrations.Add(
                new()
                {
                    InstanceId = instance.InstanceId,
                    Name = instance.Name,
                });
        }

        return orchestrations.ToArray();
    }
}