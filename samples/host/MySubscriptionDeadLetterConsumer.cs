using Edgamat.Messaging;

using Microsoft.Extensions.Logging;

namespace Edgamat.Messaging.Samples.Host;

public class MySubscriptionDeadLetterConsumer : JsonConsumer<MyMessage>
{
    private readonly ILogger<MySubscriptionDeadLetterConsumer> _logger;

    public MySubscriptionDeadLetterConsumer(ILogger<MySubscriptionDeadLetterConsumer> logger) : base(logger)
    {
        _logger = logger;
    }
    public override Task ConsumeMessageAsync(MyMessage message, CancellationToken token)
    {
        _logger.LogInformation("Dead letter message: {Batch} - {Message}", message.Batch, message.Message);

        return Task.CompletedTask;
    }
}
