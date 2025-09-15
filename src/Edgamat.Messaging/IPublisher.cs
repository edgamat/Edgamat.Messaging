namespace Edgamat.Messaging;

public interface IPublisher
{
    Task<string> PublishAsync<T>(string queueName, T message, CancellationToken cancellationToken = default) where T : class;
}

public interface IKeyedPublisher
{
    Task<string> PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class;
}
