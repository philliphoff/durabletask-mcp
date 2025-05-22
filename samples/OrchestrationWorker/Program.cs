using Microsoft.DurableTask.Worker;
using Microsoft.DurableTask.Worker.AzureManaged;

var builder = Host.CreateApplicationBuilder(args);

string connectionString = builder.Configuration["ConnectionStrings:DurableTask"] ?? throw new InvalidOperationException("Connection string not found.");

builder.Services.AddDurableTaskWorker()
    .AddTasks(registry =>
    {
        registry.AddOrchestrator<SuccessfulOrchestrator>();
        registry.AddActivity<SuccessfulActivity>();
    })
    .UseDurableTaskScheduler(connectionString);

var host = builder.Build();

host.Run();
