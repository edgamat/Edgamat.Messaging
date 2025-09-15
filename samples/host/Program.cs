using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Edgamat.Messaging.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Edgamat.Messaging;
using Edgamat.Messaging.Samples.Host;

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true)
    .Build();

var services = new ServiceCollection();
services
    .AddLogging(configure => configure.SetMinimumLevel(LogLevel.Debug).AddConsole());

services.AddAzureServiceBus()
    .WithConfiguration(configuration)
    .AddPublisher("queue.1")
    .AddBusConsumersHostedService()
    .Build();

var serviceProvider = services.BuildServiceProvider();

using (var scope = serviceProvider.CreateScope())
{
    var scopedServices = scope.ServiceProvider;
    var logger = scopedServices.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Starting application");
    var publisher = scopedServices.GetRequiredKeyedService<IKeyedPublisher>("queue.1");
    var messageId = await publisher.PublishAsync(new MyMessage(1, 1), CancellationToken.None);
    logger.LogInformation("Published message with ID: {messageId}", messageId);
}

static async Task ConsumerTestsAsync(ServiceProvider serviceProvider)
{
    // Set up a cancellation token source that cancels when a user presses CTRL+C in the console window
    CancellationTokenSource cts = new();
    Console.CancelKeyPress += (_, e) =>
    {
        e.Cancel = true; // prevent the process from terminating.
        cts.Cancel();
    };

    var hostedServices = new List<IHostedService>();

    try
    {
        // Start the Consumer Lifecycle Service
        foreach (var hostedService in serviceProvider.GetServices<IHostedService>())
        {
            await hostedService.StartAsync(cts.Token);
            hostedServices.Add(hostedService);
        }

        Console.WriteLine("Ready...... Press CTRL+C to cancel");

        await Task.Delay(Timeout.Infinite, cts.Token);
    }
    catch (OperationCanceledException)
    {
        // User pressed CTRL+C
    }

    // Shutdown gracefully
    foreach (var hostedService in hostedServices)
    {
        await hostedService.StopAsync(CancellationToken.None);
    }
}
await ConsumerTestsAsync(serviceProvider);

