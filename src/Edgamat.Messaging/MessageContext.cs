namespace Edgamat.Messaging;

public class MessageContext
{
    public string QueueName { get; set; } = string.Empty;

    public BinaryData RawPayload { get; set; } = default!;

    public string MessageId { get; set; } = string.Empty;

    public string CorrelationId { get; set; } = string.Empty;

    public int DeliveryAttempt { get; set; }

    public int MaxDeliveryAttempts { get; set; }
}
