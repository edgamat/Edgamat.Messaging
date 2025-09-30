namespace Edgamat.Messaging;

public class MessageContext
{
    public string QueueOrTopicName { get; set; } = string.Empty;

    public string? SubscriptionName { get; set; }

    public BinaryData RawPayload { get; set; } = default!;

    public string MessageId { get; set; } = string.Empty;

    public string CorrelationId { get; set; } = string.Empty;

    public string DiagnosticId { get; set; } = string.Empty;

    public int DeliveryAttempt { get; set; }

    public int MaxDeliveryAttempts { get; set; }

    public TimeSpan RetryDelay { get; set; } = TimeSpan.Zero;
}
