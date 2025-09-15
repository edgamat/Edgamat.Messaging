using System.Text.Json;

using Azure.Messaging.ServiceBus;

namespace Edgamat.Messaging;

public class JsonPublisher : IPublisher, IAsyncDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private readonly Dictionary<string, ServiceBusSender> _senders = [];

    private readonly ServiceBusClient _client;

    public JsonPublisher(ServiceBusClient client)
    {
        _client = client;
    }

    public async Task<string> PublishAsync<T>(string queueOrTopicName, T message, CancellationToken cancellationToken = default) where T : class
    {
        if (!_senders.TryGetValue(queueOrTopicName, out var sender))
        {
            await _semaphore.WaitAsync(cancellationToken);

            try
            {
                if (!_senders.TryGetValue(queueOrTopicName, out sender))
                {
                    sender = _client.CreateSender(queueOrTopicName);
                    _senders[queueOrTopicName] = sender;
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        var jsonMessage = JsonSerializer.Serialize(message);
        var serviceBusMessage = new ServiceBusMessage(jsonMessage)
        {
            ContentType = "application/json",
            MessageId = Guid.NewGuid().ToString(),
        };

        await sender.SendMessageAsync(serviceBusMessage, cancellationToken);

        return serviceBusMessage.MessageId;
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsync(true);
        GC.SuppressFinalize(this);
    }

    protected virtual async ValueTask DisposeAsync(bool disposing)
    {
        if (disposing)
        {
            foreach (var sender in _senders.Values)
            {
                await sender.DisposeAsync();
            }
            _semaphore.Dispose();
        }
    }
}
