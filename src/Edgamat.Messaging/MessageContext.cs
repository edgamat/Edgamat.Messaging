namespace Edgamat.Messaging;

public class MessageContext<T> where T : class
{
    public string QueueName { get; set; } = string.Empty;

    public T Payload { get; set; } = default!;

    public BinaryData RawPayload { get; set; } = default!;
}
public class MessageContext
{
    public string QueueName { get; set; } = string.Empty;

    public BinaryData RawPayload { get; set; } = default!;
}
