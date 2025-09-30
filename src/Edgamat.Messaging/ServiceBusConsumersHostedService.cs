using System.Diagnostics;

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

        await ProcessQueuesAsync(cancellationToken);

        await ProcessSubscriptionsAsync(cancellationToken);
    }

    private async Task ProcessQueuesAsync(CancellationToken cancellationToken)
    {
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
                await ProcessMessageAsync(args, queueType.Key, null, queueType.Value.MaxDeliveryAttempts, queueType.Value.ConsumerType);

            processor.ProcessErrorAsync += async args =>
                await ProcessErrorAsync(args, queueType.Key);

            _processors.Add(processor);
            await processor.StartProcessingAsync(cancellationToken);
        }
    }

    private async Task ProcessSubscriptionsAsync(CancellationToken cancellationToken)
    {
        var subscriptionMap = _provider.GetRequiredService<SubscriptionConsumerMap>();

        foreach (var subscriptionType in subscriptionMap)
        {
            var processor = _client.CreateProcessor(subscriptionType.Key.TopicName, subscriptionType.Key.SubscriptionName, new ServiceBusProcessorOptions
            {
                MaxConcurrentCalls = subscriptionType.Value.MaxCompetingConsumers,
                AutoCompleteMessages = false
            });

            _logger.LogInformation("Starting processor for topic '{TopicName}' using subscription '{SubscriptionName}' {ConsumerType}, {MaxCompetingConsumers}",
                subscriptionType.Key.TopicName, subscriptionType.Key.SubscriptionName, subscriptionType.Value.ConsumerType.FullName, subscriptionType.Value.MaxCompetingConsumers);

            processor.ProcessMessageAsync += async args =>
                await ProcessMessageAsync(args, subscriptionType.Key.TopicName, subscriptionType.Key.SubscriptionName, subscriptionType.Value.MaxDeliveryAttempts, subscriptionType.Value.ConsumerType);

            processor.ProcessErrorAsync += async args =>
                await ProcessErrorAsync(args, subscriptionType.Key.TopicName);

            _processors.Add(processor);
            await processor.StartProcessingAsync(cancellationToken);
        }
    }

    private Task ProcessErrorAsync(ProcessErrorEventArgs args, string queueOrTopicName)
    {
        if (args.ErrorSource != ServiceBusErrorSource.ProcessMessageCallback)
        {
            _logger.LogError(args.Exception, "Processor error for {QueueOrTopicName}: {ErrorSource}", queueOrTopicName, args.ErrorSource);
        }

        return Task.CompletedTask;
    }

    private async Task ProcessMessageAsync(
        ProcessMessageEventArgs args,
        string queueOrTopicName,
        string? subscriptionName,
        int maxDeliveryAttempts,
        Type consumerType)
    {
        var messageContext = new MessageContext
        {
            QueueOrTopicName = queueOrTopicName,
            SubscriptionName = subscriptionName,
            RawPayload = args.Message.Body,
            MessageId = args.Message.MessageId,
            CorrelationId = args.Message.CorrelationId,
            DeliveryAttempt = args.Message.DeliveryCount,
            MaxDeliveryAttempts = maxDeliveryAttempts,
        };

        if (args.Message.ApplicationProperties.TryGetValue("Diagnostic-Id", out var objectId) && objectId is string diagnosticId)
        {
            messageContext.DiagnosticId = diagnosticId;
            _logger.LogDebug("Linking to existing activity with Diagnostic-Id: {DiagnosticId}", diagnosticId);
        }

        using var scope = _provider.CreateScope();

        if (args.Message.ApplicationProperties.Count > 0)
        {
            _logger.LogDebug("Message properties: {@ApplicationProperties}", args.Message.ApplicationProperties);
        }
        _logger.LogDebug("Received message {MessageId} on queue {QueueName}, DeliveryAttempt {DeliveryAttempt}/{MaxDeliveryAttempts}",
            messageContext.MessageId, messageContext.QueueOrTopicName, messageContext.DeliveryAttempt, messageContext.MaxDeliveryAttempts);

        var consumer = (IConsumer<MessageContext>)scope.ServiceProvider.GetRequiredService(consumerType);

        await consumer.ConsumeAsync(messageContext, args.CancellationToken);

        await args.CompleteMessageAsync(args.Message, args.CancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        // Stop and dispose all processors
        foreach (var p in _processors)
        {
            _logger.LogInformation("Stopping processor for queue/subscription '{EntityPath}'", p.EntityPath);
            await p.StopProcessingAsync(cancellationToken);
            await p.DisposeAsync();
        }
    }
}
