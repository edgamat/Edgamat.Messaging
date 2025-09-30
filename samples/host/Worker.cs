using System.Diagnostics;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Edgamat.Messaging.Samples.Host;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceProvider _serviceProvider;

    public Worker(ILogger<Worker> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

        var messageCount = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _serviceProvider.CreateScope();

            // start a new root activity for this operation
            using (var activity = DiagnosticsConfig.Source.StartActivity("DoWorkQueue", ActivityKind.Internal))
            {
                messageCount++;

                activity?.SetTag("message.batch", 1);
                activity?.SetTag("message.number", messageCount);

                var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

                var messageId = await publisher.PublishAsync("queue.1", new MyMessage(1, messageCount), CancellationToken.None);

                activity?.SetTag("message.id", messageId);

                _logger.LogInformation("Published message with ID: {messageId}", messageId);
            }

            // start a new root activity for this operation
            using (var activity = DiagnosticsConfig.Source.StartActivity("DoWorkTopic", ActivityKind.Internal))
            {
                messageCount++;

                activity?.SetTag("message.batch", 1);
                activity?.SetTag("message.number", messageCount);

                var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

                var messageId = await publisher.PublishAsync("topic.1", new MySubscriptionMessage(1, messageCount), CancellationToken.None);

                activity?.SetTag("message.id", messageId);

                _logger.LogInformation("Published message with ID: {messageId}", messageId);
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
}
