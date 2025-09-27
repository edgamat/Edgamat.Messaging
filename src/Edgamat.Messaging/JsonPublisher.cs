using System.Collections.Concurrent;
using System.Text.Json;

using Azure.Messaging.ServiceBus;

using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;

namespace Edgamat.Messaging;

public class JsonPublisher : IPublisher, IAsyncDisposable
{
    private readonly ConcurrentDictionary<string, Lazy<ServiceBusSender>> _senders = new();

    private readonly ServiceBusClient _client;
    private readonly ILogger<JsonPublisher> _logger;

    public JsonPublisher(ServiceBusClient client, ILogger<JsonPublisher> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<string> PublishAsync<T>(string queueOrTopicName, T message, CancellationToken cancellationToken = default) where T : class
    {
        var lazySender = _senders.GetOrAdd(
            queueOrTopicName,
            name => new Lazy<ServiceBusSender>(() => _client.CreateSender(name), LazyThreadSafetyMode.ExecutionAndPublication));

        ServiceBusSender sender;
        try
        {
            sender = lazySender.Value;
        }
        catch
        {
            // Remove faulted Lazy to allow retry on next call
            _senders.TryRemove(queueOrTopicName, out _);
            throw;
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
            foreach (var lazy in _senders.Values)
            {
                if (lazy.IsValueCreated)
                {
                    await lazy.Value.DisposeAsync();
                }
            }
        }
    }
}

/// <summary>
/// Factory for creating JsonServiceBusSender instances using a Lazy<T> to ensure
/// that the ServiceBusSender is created only when needed and only once per queue or topic name
/// </summary>
public class ServiceBusSenderFactory : IServiceBusSenderFactory, IAsyncDisposable, IDisposable
{
    private readonly ConcurrentDictionary<string, Lazy<ServiceBusSender>> _senders = new();
    private readonly ServiceBusClient _client;

    public ServiceBusSenderFactory(ServiceBusClient client)
    {
        _client = client;
    }

    public ServiceBusSender GetSender(string queueOrTopicName)
    {
        var lazySender = _senders.GetOrAdd(
            queueOrTopicName,
            name => new Lazy<ServiceBusSender>(() =>
            {
                Console.WriteLine($"Creating ServiceBusSender for {name}");
                return _client.CreateSender(name);
            }, LazyThreadSafetyMode.ExecutionAndPublication));

        try
        {
            return lazySender.Value;
        }
        catch
        {
            // Remove faulted Lazy to allow retry on next call
            _senders.TryRemove(queueOrTopicName, out _);
            throw;
        }
    }

    public void Dispose() => GC.SuppressFinalize(this);

    public async ValueTask DisposeAsync()
    {
        Console.WriteLine($"Disposing ServiceBusSenderFactory");
        await DisposeCoreAsync(true).ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    protected virtual async ValueTask DisposeCoreAsync(bool disposing)
    {
        if (disposing)
        {
            foreach (var lazy in _senders.Values)
            {
                if (lazy.IsValueCreated)
                {
                    await lazy.Value.DisposeAsync();
                }
            }
        }
    }
}

public interface IServiceBusSenderFactory
{
    ServiceBusSender GetSender(string queueOrTopicName);
}

public class Json3Publisher : IPublisher
{
    private readonly IAzureClientFactory<ServiceBusSender> _factory;

    public Json3Publisher(IAzureClientFactory<ServiceBusSender> senderFactory)
    {
        _factory = senderFactory;
    }

    public async Task<string> PublishAsync<T>(string queueOrTopicName, T message, CancellationToken cancellationToken = default) where T : class
    {
        var sender = _factory.CreateClient(queueOrTopicName);

        var jsonMessage = JsonSerializer.Serialize(message);
        var serviceBusMessage = new ServiceBusMessage(jsonMessage)
        {
            ContentType = "application/json",
            MessageId = Guid.NewGuid().ToString(),
        };

        await sender.SendMessageAsync(serviceBusMessage, cancellationToken);

        return serviceBusMessage.MessageId;
    }
}


public class Json2Publisher
{
    private readonly ServiceBusClient _client;

    public Json2Publisher(ServiceBusClient client)
    {
        _client = client;
    }

    public async Task<string> PublishAsync<T>(string queueOrTopicName, T message, CancellationToken cancellationToken = default) where T : class
    {
        await using var sender = _client.CreateSender(queueOrTopicName);

        var jsonMessage = JsonSerializer.Serialize(message);
        var serviceBusMessage = new ServiceBusMessage(jsonMessage)
        {
            ContentType = "application/json",
            MessageId = Guid.NewGuid().ToString(),
        };

        await sender.SendMessageAsync(serviceBusMessage, cancellationToken);

        return serviceBusMessage.MessageId;
    }
}


