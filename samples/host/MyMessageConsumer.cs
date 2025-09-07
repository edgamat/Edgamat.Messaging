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

    public override Task ConsumeMessageAsync(MyMessage message, CancellationToken token)
    {
        _logger.LogInformation("Consuming message: {Batch} - {Message}", message.Batch, message.Message);

        if (message.Message % 5 == 0)
        {
            // simulate a failure every 5th message
            throw new Exception($"Simulated exception processing message {message.Message}.");
        }

        _logger.LogInformation("Successfully consumed message: {Batch} - {Message}", message.Batch, message.Message);

        return Task.CompletedTask;
    }
}
