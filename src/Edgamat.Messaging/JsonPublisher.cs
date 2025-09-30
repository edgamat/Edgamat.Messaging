using System.Diagnostics;
using System.Text.Json;

using Azure.Messaging.ServiceBus;

using Microsoft.Extensions.Azure;

namespace Edgamat.Messaging;

public class JsonPublisher : IPublisher
{
    private readonly IAzureClientFactory<ServiceBusSender> _factory;

    public JsonPublisher(IAzureClientFactory<ServiceBusSender> senderFactory)
    {
        _factory = senderFactory;
    }

    public async Task<string> PublishAsync<T>(string queueOrTopicName, T message, CancellationToken cancellationToken = default) where T : class
    {
        using var activity = DiagnosticsConfig.Source.StartActivity("MessagePublisher.Publish", ActivityKind.Producer);
        activity.EnrichWithContext<T>(queueOrTopicName);

        try
        {
            var sender = _factory.CreateClient(queueOrTopicName);

            var jsonMessage = JsonSerializer.Serialize(message);
            var serviceBusMessage = new ServiceBusMessage(jsonMessage)
            {
                ContentType = "application/json",
                MessageId = Guid.NewGuid().ToString(),
            };

            await sender.SendMessageAsync(serviceBusMessage, cancellationToken);

            activity.EnrichWithMessage(serviceBusMessage);

            return serviceBusMessage.MessageId;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Failure to publish message");
            activity?.AddException(ex);

            throw;
        }
    }
}
