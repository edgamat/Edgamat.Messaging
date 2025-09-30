using Edgamat.Messaging;

using Microsoft.Extensions.Logging;

namespace Edgamat.Messaging.Samples.Host;

public record MySubscriptionMessage(int Batch, int Message);

public class MySubscriptionMessageConsumer : JsonConsumer<MySubscriptionMessage>
{
    private readonly ILogger<MySubscriptionMessageConsumer> _logger;

    public MySubscriptionMessageConsumer(ILogger<MySubscriptionMessageConsumer> logger) : base(logger)
    {
        _logger = logger;
    }

    public override async Task ConsumeMessageAsync(MySubscriptionMessage message, CancellationToken token)
    {
        _logger.LogInformation("Consuming message: {Batch} - {Message}", message.Batch, message.Message);

        using (var activity = DiagnosticsConfig.Source.StartActivity("SQLCall1"))
        {
            // Simulate some processing
            await Task.Delay(300, token);
        }

        using (var activity = DiagnosticsConfig.Source.StartActivity("SQLCall2"))
        {
            // Simulate some processing
            await Task.Delay(200, token);
        }

        // Uncomment to simulate occasional failures
        if (message.Message % 5 == 0)
        {
            // simulate a failure every 5th message
            throw new Exception($"Simulated exception processing message {message.Message}.");
        }

        _logger.LogInformation("Successfully consumed message: {Batch} - {Message}", message.Batch, message.Message);
    }
}
