namespace Edgamat.Messaging;

using System.Text.Json;

public interface IConsumer
{
}

/// <summary>
/// Represents a message consumer.
/// </summary>
/// <typeparam name="TMessage">The type of message being consumed.</typeparam>
public interface IConsumer<in TMessage> : IConsumer where TMessage : class
{
    /// <summary>
    /// Consumes a message asynchronously.
    /// </summary>
    /// <param name="message">The message to consume.</param>
    /// <param name="token">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ConsumeAsync(TMessage message, CancellationToken token);
}

public abstract class JsonDocumentConsumer<T> : IConsumer<MessageContext<JsonDocument>> where T : class
{
    public Task ConsumeAsync(MessageContext<JsonDocument> message, CancellationToken token)
    {
        var rawPayload = message.RawPayload.ToObjectFromJson<T>() ?? throw new JsonException("Unable to deserialize message body.");

        return ConsumeMessageAsync(rawPayload, token);
    }

    public abstract Task ConsumeMessageAsync(T message, CancellationToken cancellationToken);
}


public abstract class JsonConsumer<T> : IConsumer<MessageContext> where T : class
{
    public Task ConsumeAsync(MessageContext message, CancellationToken token)
    {
        var rawPayload = message.RawPayload.ToObjectFromJson<T>() ?? throw new JsonException("Unable to deserialize message body.");

        return ConsumeMessageAsync(rawPayload, token);
    }

    public abstract Task ConsumeMessageAsync(T message, CancellationToken cancellationToken);
}
