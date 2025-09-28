using Edgamat.Messaging;

using Microsoft.Extensions.Logging;

namespace Edgamat.Messaging.Samples.Host;

public record MyMessage(int Batch, int Message);

public class MyMessageConsumer : JsonConsumer<MyMessage>
{
    private readonly ILogger<MyMessageConsumer> _logger;

    public MyMessageConsumer(ILogger<MyMessageConsumer> logger) : base(logger)
    {
        _logger = logger;
    }

    public override async Task ConsumeMessageAsync(MyMessage message, CancellationToken token)
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
