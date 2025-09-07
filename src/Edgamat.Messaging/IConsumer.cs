namespace Edgamat.Messaging;

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
