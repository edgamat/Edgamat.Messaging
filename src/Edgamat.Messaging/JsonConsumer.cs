namespace Edgamat.Messaging;

using System.Diagnostics;
using System.Text.Json;

using Microsoft.Extensions.Logging;

public abstract class JsonConsumer<T> : IConsumer<MessageContext> where T : class
{
    private readonly ILogger<JsonConsumer<T>> _logger;

    public JsonConsumer(ILogger<JsonConsumer<T>> logger)
    {
        _logger = logger;
    }

    public async Task ConsumeAsync(MessageContext messageContext, CancellationToken token)
    {
        var parentContext = ActivityContext.TryParse(messageContext.DiagnosticId, null, out var parsedContext)
            ? parsedContext
            : default;

        using var activity = DiagnosticsConfig.Source.StartActivity("MessageConsumer.Consume", ActivityKind.Consumer, parentContext);
        activity.EnrichWithContext<T>(messageContext);

        MessageContext = messageContext;

        try
        {
            var rawPayload = messageContext.RawPayload.ToObjectFromJson<T>() ?? throw new JsonException("Unable to deserialize message body.");

            await ConsumeMessageAsync(rawPayload, token);
        }
        catch (Exception ex)
        {
            if (messageContext.DeliveryAttempt >= messageContext.MaxDeliveryAttempts)
            {
                activity?.SetStatus(ActivityStatusCode.Error, "Failure to consume message");
                activity?.AddException(ex);

                _logger.LogError(ex, "Message delivery failed on attempt {DeliveryAttempt}. MessageId: {MessageId}, CorrelationId: {CorrelationId}",
                    messageContext.DeliveryAttempt, messageContext.MessageId, messageContext.CorrelationId);
                throw;
            }

            _logger.LogWarning(ex, "Message delivery failed on attempt {DeliveryAttempt}. MessageId: {MessageId}, CorrelationId: {CorrelationId}",
                messageContext.DeliveryAttempt, messageContext.MessageId, messageContext.CorrelationId);

            // add activity for retry delay
            using (var retryActivity = DiagnosticsConfig.Source.StartActivity("RetryDelay", ActivityKind.Internal))
            {
                retryActivity?.EnrichWithContext<T>(messageContext);
                retryActivity?.SetTag("messaging.retry.delay", messageContext.RetryDelay.TotalMilliseconds);
                await Task.Delay(messageContext.RetryDelay, token);
            }

            throw;
        }
    }

    public MessageContext? MessageContext { get; set; }

    public abstract Task ConsumeMessageAsync(T message, CancellationToken cancellationToken);
}
