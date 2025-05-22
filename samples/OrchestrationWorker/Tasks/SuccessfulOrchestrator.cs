using Microsoft.DurableTask;

sealed class SuccessfulOrchestrator : TaskOrchestrator<string, string>
{
    public override async Task<string> RunAsync(TaskOrchestrationContext context, string input)
    {
        return await context.CallActivityAsync<string>(nameof(SuccessfulActivity), input);
    }
}