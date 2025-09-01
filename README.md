I have use several messaging solutions with .NET: Kafka, Google Cloud Pub/Sub, and RabbitMQ. For
RabbitMQ, I used the MassTransit library as an abstraction for my .NET. For Kafka and the Google Cloud
messaging, I create custom messaging libraries, inspired by the design of MassTransit. I found that
the large majority of the features in MassTransit were never used. We only ever use the basics:

- Publish a command message and have competing consumers process the messages.
- Publish an event and have multiple subscribers consume the messages.
- Use a dead-letter queue (DLQ) to handle failed messages.
- All message are published in JSON.

I wanted to explore Azure Service Bus by creating a simple messaging library. The API would be
something like this:

- Create a consumer class that inherits from a base interface (`IConsumer`). This interface exposes
  a generic `ConsumeAsync` method that accepts the object (deserialized from the JSON in the message)
- Register the class as the consumer of a message from a queue or,
- Register the class as the consumer of a message from a subscription,
- If a runtime exception occurs in the consumer, the message is sent to the dead-letter queue
- Create a publisher class that inherits from a base `IMessagePublisher` interface. This interface
  exposes a generic `PublishAsync` method that accept the name of the topic and the object being
  published

Each type of consumer is registered to a specific queue or subscription:

```csharp
services.AddAzureServiceBus()
    .WithConfiguration(services.Configuration)
    .AddConsumer<MyMessageConsumer>("queueOrTopicName")
    .Build();
```

The `MyMessageConsumer` inherits from `IConsumer` interface:

```csharp
interface IConsumer<in TMessage> where TMessage: class
{
    Task ConsumeAsync(TMessage message, CancellationToken cancellationToken);
}
```
