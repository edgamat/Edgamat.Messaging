namespace Edgamat.Messaging;

using System.Text.Json;

using Microsoft.Extensions.Logging;

public abstract class JsonConsumer<T> : IConsumer<MessageContext> where T : class
{
    private readonly ILogger _logger;

    public JsonConsumer(ILogger logger)
    {
        _logger = logger;
    }

    public Task ConsumeAsync(MessageContext messageContext, CancellationToken token)
    {
        MessageContext = messageContext;

        var rawPayload = messageContext.RawPayload.ToObjectFromJson<T>() ?? throw new JsonException("Unable to deserialize message body.");

        try
        {
            return ConsumeMessageAsync(rawPayload, token);
        }
        catch (Exception ex)
        {
            if (messageContext.DeliveryAttempt >= messageContext.MaxDeliveryAttempts)
            {
                _logger.LogError(ex, "Message delivery failed after {DeliveryAttempt} attempts. MessageId: {MessageId}, CorrelationId: {CorrelationId}",
                    messageContext.DeliveryAttempt, messageContext.MessageId, messageContext.CorrelationId);
                throw;
            }

            _logger.LogWarning(ex, "Message delivery failed on attempt {DeliveryAttempt}. MessageId: {MessageId}, CorrelationId: {CorrelationId}",
                messageContext.DeliveryAttempt, messageContext.MessageId, messageContext.CorrelationId);
            throw;
        }
    }

    public MessageContext? MessageContext { get; set; }

    public abstract Task ConsumeMessageAsync(T message, CancellationToken cancellationToken);
}
