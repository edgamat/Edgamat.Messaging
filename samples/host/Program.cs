using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Edgamat.Messaging;
using Edgamat.Messaging.Configuration;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((ctx, cfg) =>
    {
        cfg.AddJsonFile("appsettings.json", optional: true);
    })
    .ConfigureServices((ctx, services) =>
    {
        var builder = new AzureServiceBusBuilder(services);
        builder.WithConfiguration(ctx.Configuration);
        builder.AddConsumer<MyMessageConsumer>("queue.1");
        builder.AddBusConsumersHostedService();
        builder.Build();
    })
    .Build();

await host.RunAsync();

// sample message and consumer implementation ---------------------------------
public record MyMessage(int Batch, int Message);

public class MyMessageConsumer : JsonConsumer<MyMessage>
{
    public override Task ConsumeMessageAsync(MyMessage message, CancellationToken token)
    {
        Console.WriteLine($"Consumed message: {message.Batch} - {message.Message}");
        return Task.CompletedTask;
    }
}

// // hosted service that wires ServiceBus processors to IConsumer<T> ----------------
// public class ServiceBusConsumersHostedService : IHostedService
// {
//     private readonly ServiceBusClient _client;
//     private readonly IServiceProvider _provider;
//     private readonly List<ServiceBusProcessor> _processors = new();

//     public ServiceBusConsumersHostedService(ServiceBusClient client, IServiceProvider provider)
//     {
//         _client = client;
//         _provider = provider;
//     }

//     public Task StartAsync(CancellationToken cancellationToken)
//     {
//         // Example: each queue has its own handler. Configure queue names via configuration.
//         // For demo we hardcode queue names. In real apps, read from IConfiguration.
//         var queues = new[] { "my-queue-1", "my-queue-2" };

//         foreach (var queue in queues)
//         {
//             var processor = _client.CreateProcessor(queue, new ServiceBusProcessorOptions
//             {
//                 MaxConcurrentCalls = 1,
//                 AutoCompleteMessages = false
//             });

//             processor.ProcessMessageAsync += async args =>
//             {
//                 // determine message type by queue (or message properties) and dispatch to the right consumer
//                 // Here we assume queue maps to MyMessage for simplicity.
//                 var body = args.Message.Body.ToString();
//                 var message = JsonSerializer.Deserialize<MyMessage>(body);
//                 if (message is not null)
//                 {
//                     using var scope = _provider.CreateScope();
//                     var consumer = scope.ServiceProvider.GetRequiredService<IConsumer<MyMessage>>();
//                     await consumer.ConsumeAsync(message, args.CancellationToken);
//                     await args.CompleteMessageAsync(args.Message, args.CancellationToken);
//                 }
//                 else
//                 {
//                     await args.DeadLetterMessageAsync(args.Message, cancellationToken: args.CancellationToken);
//                 }
//             };

//             processor.ProcessErrorAsync += args =>
//             {
//                 Console.WriteLine($"Processor error for queue {queue}: {args.Exception}");
//                 return Task.CompletedTask;
//             };

//             _processors.Add(processor);
//             processor.StartProcessingAsync(cancellationToken).GetAwaiter().GetResult();
//         }

//         return Task.CompletedTask;
//     }

//     public Task StopAsync(CancellationToken cancellationToken)
//     {
//         foreach (var p in _processors)
//         {
//             p.StopProcessingAsync(cancellationToken).GetAwaiter().GetResult();
//             p.DisposeAsync().AsTask().GetAwaiter().GetResult();
//         }

//         return Task.CompletedTask;
//     }
// }
