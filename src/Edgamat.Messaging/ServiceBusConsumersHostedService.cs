using System.Text.Json;

using Azure.Messaging.ServiceBus;

using Edgamat.Messaging.Configuration;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Edgamat.Messaging;

public class ServiceBusConsumersHostedService : IHostedService
{
    private readonly ServiceBusClient _client;
    private readonly IServiceProvider _provider;
    private readonly List<ServiceBusProcessor> _processors = [];

    private readonly QueueConsumerMap _queueMap;


    public ServiceBusConsumersHostedService(
        ServiceBusClient client,
        IServiceProvider provider)
    {
        _client = client;
        _provider = provider;
        _queueMap = _provider.GetRequiredService<QueueConsumerMap>();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var logger = _provider.GetRequiredService<ILogger<ServiceBusConsumersHostedService>>();
        logger.LogInformation("Starting ServiceBusConsumersHostedService");

        foreach (var queueType in _queueMap)
        {
            var processor = _client.CreateProcessor(queueType.Key, new ServiceBusProcessorOptions
            {
                MaxConcurrentCalls = queueType.Value.MaxCompetingConsumers,
                AutoCompleteMessages = false
            });

            logger.LogInformation("Starting processor for queue '{QueueName}' with consumer '{ConsumerType}', Max {MaxConcurrentCalls}",
                queueType.Key, queueType.Value.ConsumerType.FullName, queueType.Value.MaxCompetingConsumers);

            processor.ProcessMessageAsync += async args =>
            {
                var messageContext = new MessageContext
                {
                    QueueName = queueType.Key,
                    RawPayload = args.Message.Body,
                    MessageId = args.Message.MessageId,
                    CorrelationId = args.Message.CorrelationId,
                    DeliveryAttempt = args.Message.DeliveryCount,
                    MaxDeliveryAttempts = queueType.Value.MaxDeliveryAttempts
                };

                using var scope = _provider.CreateScope();

                var logger = scope.ServiceProvider.GetRequiredService<ILogger<ServiceBusConsumersHostedService>>();
                logger.LogInformation("Message properties: {MessageContext}", JsonSerializer.Serialize(args.Message.ApplicationProperties));
                logger.LogInformation("Received message {MessageId} on queue {QueueName}, DeliveryAttempt {DeliveryAttempt}/{MaxDeliveryAttempts}",
                    messageContext.MessageId, messageContext.QueueName, messageContext.DeliveryAttempt, messageContext.MaxDeliveryAttempts);

                var consumer = (IConsumer<MessageContext>)scope.ServiceProvider.GetRequiredService(queueType.Value.ConsumerType);

                await consumer.ConsumeAsync(messageContext, args.CancellationToken);

                await args.CompleteMessageAsync(args.Message, args.CancellationToken);
            };

            processor.ProcessErrorAsync += args =>
            {
                Console.WriteLine($"Processor error for queue {queueType.Key}: {args.Exception}");
                return Task.CompletedTask;
            };

            _processors.Add(processor);
            processor.StartProcessingAsync(cancellationToken).GetAwaiter().GetResult();
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var p in _processors)
        {
            p.StopProcessingAsync(cancellationToken).GetAwaiter().GetResult();
            p.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        return Task.CompletedTask;
    }
}
