using System.Text.Json;

using Azure.Messaging.ServiceBus;

using Edgamat.Messaging.Configuration;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Edgamat.Messaging;

public class ServiceBusConsumersHostedService : IHostedService
{
    private readonly ILogger<ServiceBusConsumersHostedService> _logger;
    private readonly ServiceBusClient _client;
    private readonly IServiceProvider _provider;
    private readonly List<ServiceBusProcessor> _processors = [];


    public ServiceBusConsumersHostedService(
        ILogger<ServiceBusConsumersHostedService> logger,
        ServiceBusClient client,
        IServiceProvider provider)
    {
        _logger = logger;
        _client = client;
        _provider = provider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting ServiceBusConsumersHostedService");

        var queueMap = _provider.GetRequiredService<QueueConsumerMap>();

        foreach (var queueType in queueMap)
        {
            var processor = _client.CreateProcessor(queueType.Key, new ServiceBusProcessorOptions
            {
                MaxConcurrentCalls = queueType.Value.MaxCompetingConsumers,
                AutoCompleteMessages = false
            });

            _logger.LogInformation("Starting processor for queue '{QueueName}' '{ConsumerType}', {MaxCompetingConsumers}",
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

                if (args.Message.ApplicationProperties.Count > 0)
                {
                    _logger.LogInformation("Message properties: {@ApplicationProperties}", args.Message.ApplicationProperties);
                }
                _logger.LogInformation("Received message {MessageId} on queue {QueueName}, DeliveryAttempt {DeliveryAttempt}/{MaxDeliveryAttempts}",
                    messageContext.MessageId, messageContext.QueueName, messageContext.DeliveryAttempt, messageContext.MaxDeliveryAttempts);

                var consumer = (IConsumer<MessageContext>)scope.ServiceProvider.GetRequiredService(queueType.Value.ConsumerType);

                await consumer.ConsumeAsync(messageContext, args.CancellationToken);

                await args.CompleteMessageAsync(args.Message, args.CancellationToken);
            };

            processor.ProcessErrorAsync += args =>
            {
                if (args.ErrorSource != ServiceBusErrorSource.ProcessMessageCallback)
                {
                    _logger.LogError(args.Exception, "Processor error for queue {QueueName}: {ErrorSource}", queueType.Key, args.ErrorSource);
                }

                return Task.CompletedTask;
            };

            _processors.Add(processor);
            await processor.StartProcessingAsync(cancellationToken);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        // Stop and dispose all processors
        foreach (var p in _processors)
        {
            _logger.LogInformation("Stopping processor for queue '{QueueName}'", p.EntityPath);
            await p.StopProcessingAsync(cancellationToken);
            await p.DisposeAsync();
        }
    }
}
