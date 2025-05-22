using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using DurableTask.Core;
using Microsoft.DurableTask.Client;
using Microsoft.DurableTask.Client.AzureManaged;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;

public sealed record FailureDetails
{
    public required string ErrorMessage { get; init; }

    public string? StackTrace { get; init; }
}

public sealed record TaskHubOrchestration
{
    public DateTimeOffset CreatedAt { get; init; }

    public FailureDetails? FailureDetails { get; init; }

    public required string InstanceId { get; init; }

    public required string Name { get; init; }

    public required string Status { get; init; }
}

public sealed record TaskHubOrchestrationCreationResult
{
    public required string InstanceId { get; init; }
}

[McpServerToolType]
public static class DurableTaskHubTool
{
    [McpServerTool, Description("Create an orchestration in a Durable Task Hubs in a Durable Task Scheduler.")]
    public static async Task<TaskHubOrchestrationCreationResult> CreateOrchestration(
        [Description("The name of the task hub to create the orchestration in.")] string taskHubName,
        [Description("The endpoint of the scheduler for the task hub.")] Uri schedulerEndpoint,
        [Description("The name of the orchestration to create.")] string orchestrationName,
        [Description("The optional input to the orchestration, in JSON format.")] string? input = null)
    {
        var client = CreateTaskHubClient(taskHubName, schedulerEndpoint);

        object? inputObject = null;

        if (input is not null)
        {
            inputObject = JsonValue.Parse(input) ?? "null";
        }

        var instanceId = await client.ScheduleNewOrchestrationInstanceAsync(orchestrationName, inputObject);

        return new()
        {
            InstanceId = instanceId,
        };
    }

    [McpServerTool, Description("Delete orchestrations in a Durable Task Scheduler Task Hub.")]
    public static async Task DeleteOrchestrationsForTaskHub(
        [Description("The name of the task hub to query for orchestrations.")] string taskHubName,
        [Description("The endpoint of the scheduler for the task hub.")] Uri schedulerEndpoint,
        [Description("The instance IDs of the orchestrations to delete.")] string[] instanceIds,
        CancellationToken cancellationToken)
    {
        var client = CreateTaskHubClient(taskHubName, schedulerEndpoint);

        var tasks = instanceIds.Select(id => client.PurgeInstanceAsync(id, cancellation: cancellationToken)).ToList();

        await Task.WhenAll(tasks);
    }

    [McpServerTool, Description("Resume orchestrations in a Durable Task Scheduler Task Hub.")]
    public static async Task ResumeOrchestrationsForTaskHub(
        [Description("The name of the task hub to query for orchestrations.")] string taskHubName,
        [Description("The endpoint of the scheduler for the task hub.")] Uri schedulerEndpoint,
        [Description("The instance IDs of the orchestrations to resume.")] string[] instanceIds,
        CancellationToken cancellationToken)
    {
        var client = CreateTaskHubClient(taskHubName, schedulerEndpoint);

        var tasks = instanceIds.Select(id => client.ResumeInstanceAsync(id, cancellation: cancellationToken)).ToList();

        await Task.WhenAll(tasks);
    }

    [McpServerTool, Description("Suspend orchestrations in a Durable Task Scheduler Task Hub.")]
    public static async Task SuspendOrchestrationsForTaskHub(
        [Description("The name of the task hub to query for orchestrations.")] string taskHubName,
        [Description("The endpoint of the scheduler for the task hub.")] Uri schedulerEndpoint,
        [Description("The instance IDs of the orchestrations to suspend.")] string[] instanceIds,
        CancellationToken cancellationToken)
    {
        var client = CreateTaskHubClient(taskHubName, schedulerEndpoint);

        var tasks = instanceIds.Select(id => client.SuspendInstanceAsync(id, cancellation: cancellationToken)).ToList();

        await Task.WhenAll(tasks);
    }

    [McpServerTool, Description("Terminate orchestrations in a Durable Task Scheduler Task Hub.")]
    public static async Task TerminateOrchestrationsForTaskHub(
        [Description("The name of the task hub to query for orchestrations.")] string taskHubName,
        [Description("The endpoint of the scheduler for the task hub.")] Uri schedulerEndpoint,
        [Description("The instance IDs of the orchestrations to terminate.")] string[] instanceIds,
        CancellationToken cancellationToken)
    {
        var client = CreateTaskHubClient(taskHubName, schedulerEndpoint);

        var tasks = instanceIds.Select(id => client.TerminateInstanceAsync(id, cancellation: cancellationToken)).ToList();

        await Task.WhenAll(tasks);
    }

    [McpServerTool, Description("List orchestrations in a Durable Task Scheduler Task Hub.")]
    public static async Task<TaskHubOrchestration[]> GetOrchestrationsForTaskHub(
        [Description("The name of the task hub to query for orchestrations.")] string taskHubName,
        [Description("The endpoint of the scheduler for the task hub.")] string schedulerEndpoint)
    {
        var client = CreateTaskHubClient(taskHubName, new Uri(schedulerEndpoint));

        List<TaskHubOrchestration> orchestrations = new();

        var instances = client.GetAllInstancesAsync(new() { FetchInputsAndOutputs = true});

        await foreach (var instance in instances)
        {
            orchestrations.Add(
                new()
                {
                    CreatedAt = instance.CreatedAt,
                    FailureDetails = instance.FailureDetails is not null
                        ? new FailureDetails
                        {
                            ErrorMessage = instance.FailureDetails.ErrorMessage,
                            StackTrace = instance.FailureDetails.StackTrace
                        }
                        : null,
                    InstanceId = instance.InstanceId,
                    Name = instance.Name,
                    Status = GetOrchestrationStatus(instance.RuntimeStatus)
                });
        }

        return orchestrations.ToArray();
    }

    static DurableTaskClient CreateTaskHubClient(string taskHubName, Uri schedulerEndpoint)
    {
        var services = new ServiceCollection();

        services.AddLogging(_ => { });

        var builder = new DefaultDurableTaskClientBuilder("client", services);

        builder.UseDurableTaskScheduler($"Endpoint={schedulerEndpoint};TaskHub={taskHubName};Authentication=DefaultAzure");

        using var serviceProvider = services.BuildServiceProvider();

        return builder.Build(serviceProvider);
    }
    
    static string GetOrchestrationStatus(OrchestrationRuntimeStatus status)
    {
        return status switch
        {
            OrchestrationRuntimeStatus.Canceled => "Canceled",
            OrchestrationRuntimeStatus.Completed => "Completed",
            OrchestrationRuntimeStatus.ContinuedAsNew => "ContinuedAsNew",
            OrchestrationRuntimeStatus.Failed => "Failed",
            OrchestrationRuntimeStatus.Pending => "Pending",
            OrchestrationRuntimeStatus.Running => "Running",
            OrchestrationRuntimeStatus.Suspended => "Suspended",
            OrchestrationRuntimeStatus.Terminated => "Terminated",
            _ => "Unknown"
        };
    }
}