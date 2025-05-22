using Microsoft.DurableTask;

sealed class SuccessfulActivity : TaskActivity<string, string>
{
    public override Task<string> RunAsync(TaskActivityContext context, string input)
    {
        throw new NotImplementedException();
    }
}