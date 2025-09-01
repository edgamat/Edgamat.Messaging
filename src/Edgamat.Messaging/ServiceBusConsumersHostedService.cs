using System.Text.Json;

using Azure.Messaging.ServiceBus;

using Edgamat.Messaging.Configuration;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Edgamat.Messaging;

public class ServiceBusConsumersHostedService : IHostedService
{
    private readonly ServiceBusClient _client;
    private readonly IServiceProvider _provider;
    private readonly List<ServiceBusProcessor> _processors = [];

    private readonly QueueMap _queueConsumers;


    public ServiceBusConsumersHostedService(ServiceBusClient client, IServiceProvider provider)
    {
        _client = client;
        _provider = provider;
        _queueConsumers = _provider.GetRequiredService<QueueMap>();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var queueConsumer in _queueConsumers)
        {
            var processor = _client.CreateProcessor(queueConsumer.Key, new ServiceBusProcessorOptions
            {
                MaxConcurrentCalls = Environment.ProcessorCount,
                AutoCompleteMessages = false
            });

            processor.ProcessMessageAsync += async args =>
            {
                var messageContext = new MessageContext
                {
                    QueueName = queueConsumer.Key,
                    RawPayload = args.Message.Body,
                };

                using var scope = _provider.CreateScope();

                var consumer = (IConsumer<MessageContext>)scope.ServiceProvider.GetRequiredService(queueConsumer.Value);

                await consumer.ConsumeAsync(messageContext, args.CancellationToken);

                await args.CompleteMessageAsync(args.Message, args.CancellationToken);
            };

            processor.ProcessErrorAsync += args =>
            {
                Console.WriteLine($"Processor error for queue {queueConsumer.Key}: {args.Exception}");
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
