using Edgamat.Messaging;

using Microsoft.Extensions.Logging;

namespace Edgamat.Messaging.Samples.Host;

public class MyMessageDeadLetterConsumer : JsonConsumer<MyMessage>
{
    private readonly ILogger<MyMessageDeadLetterConsumer> _logger;

    public MyMessageDeadLetterConsumer(ILogger<MyMessageDeadLetterConsumer> logger) : base(logger)
    {
        _logger = logger;
    }
    public override Task ConsumeMessageAsync(MyMessage message, CancellationToken token)
    {
        _logger.LogInformation("Dead letter message: {Batch} - {Message}", message.Batch, message.Message);

        return Task.CompletedTask;
    }
}
