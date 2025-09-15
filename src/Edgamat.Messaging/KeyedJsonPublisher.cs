using System.Text.Json;

using Azure.Messaging.ServiceBus;

namespace Edgamat.Messaging;

public class KeyedJsonPublisher : IKeyedPublisher, IAsyncDisposable
{
    private readonly ServiceBusSender _sender;

    public KeyedJsonPublisher(ServiceBusClient client, string queueOrTopicName)
    {
        _sender = client.CreateSender(queueOrTopicName);
    }

    public async Task<string> PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class
    {
        var jsonMessage = JsonSerializer.Serialize(message);
        var serviceBusMessage = new ServiceBusMessage(jsonMessage)
        {
            ContentType = "application/json",
            MessageId = Guid.NewGuid().ToString(),
        };

        await _sender.SendMessageAsync(serviceBusMessage, cancellationToken);

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
            await _sender.DisposeAsync();
        }
    }
}
