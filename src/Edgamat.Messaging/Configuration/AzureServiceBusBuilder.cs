using Azure.Messaging.ServiceBus;

using Microsoft.Extensions.Azure;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Edgamat.Messaging.Configuration;

public class AzureServiceBusBuilder
{
    private readonly IServiceCollection _services;
    private IConfiguration? _configuration;
    private string? _configurationSection;
    private readonly QueueConsumerMap _queueMap = new();

    public AzureServiceBusBuilder(IServiceCollection services)
    {
        _services = services;
    }

    public AzureServiceBusBuilder WithConfiguration(
        IConfiguration configuration,
        string configurationSection = AzureServiceBusSettings.DefaultConfigurationSection)
    {
        _configuration = configuration;
        _configurationSection = configurationSection;

        return this;
    }

    public AzureServiceBusBuilder AddBusConsumersHostedService()
    {
        _services.AddHostedService<ServiceBusConsumersHostedService>();

        return this;
    }

    public AzureServiceBusBuilder AddConsumer<TConsumer>(string queueName, int maxCompetingConsumers = 1, int maxDeliveryAttempts = 3) where TConsumer : class, IConsumer
    {
        MapConsumerToQueue(typeof(TConsumer), queueName, maxCompetingConsumers, maxDeliveryAttempts);

        return this;
    }

    public AzureServiceBusBuilder AddConsumer<TConsumer>(string queueName) where TConsumer : class, IConsumer
    {
        return AddConsumer<TConsumer>(queueName, Environment.ProcessorCount);
    }

    public AzureServiceBusBuilder AddPublisher()
    {
        //_services.AddScoped<IServiceBusSenderFactory, ServiceBusSenderFactory>();
        _services.AddScoped<IPublisher, Json3Publisher>();

        return this;
    }

    public AzureServiceBusBuilder AddPublisher(string queueOrTopicName)
    {
        _services.AddKeyedSingleton<IKeyedPublisher>(queueOrTopicName, (sp, _) =>
        {
            var client = sp.GetRequiredService<ServiceBusClient>();
            return new KeyedJsonPublisher(client, queueOrTopicName);
        });

        return this;
    }

    public IServiceCollection Build()
    {
        if (_configuration == null || _configurationSection == null)
            throw new AzureServiceBusConfigurationException("Configuration and ConfigurationSection must be set.");

        var settings = new AzureServiceBusSettings();
        _configuration.GetSection(_configurationSection).Bind(settings);
        _services.AddSingleton(settings);

        // Register enabled consumers from configuration
        foreach (var queue in settings.Queues.Where(q => q.Enabled))
        {
            var consumerType = Type.GetType(queue.ConsumerType)
                ?? throw new AzureServiceBusConfigurationException($"Could not load consumer type '{queue.ConsumerType}' for queue '{queue.QueueName}'");

            MapConsumerToQueue(consumerType, queue.QueueName, queue.MaxCompetingConsumers, queue.MaxDeliveryAttempts);
        }

        _services.AddSingleton(_queueMap);

        // _services.AddSingleton(provider =>
        // {
        //     var settings = provider.GetRequiredService<AzureServiceBusSettings>();
        //     return new ServiceBusClient(settings.ConnectionString);
        // });

        _services.AddAzureClients(builder =>
        {
            builder.AddServiceBusClient(settings.ConnectionString);

            foreach (var queueName in settings.Queues.Where(q => q.Enabled).Select(q => q.QueueName))
            {
                builder.AddClient<ServiceBusSender, ServiceBusClientOptions>((_, _, provider) =>
                    provider
                        .GetRequiredService<ServiceBusClient>()
                        .CreateSender(queueName)
                ).WithName(queueName);
            }
        });

        return _services;
    }

    private void MapConsumerToQueue(Type consumerType, string queueName, int maxCompetingConsumers, int maxDeliveryAttempts)
    {
        if (!typeof(IConsumer<MessageContext>).IsAssignableFrom(consumerType))
            throw new AzureServiceBusConfigurationException($"Consumer type '{consumerType.FullName}' does not implement IConsumer interface.");

        _services.AddScoped(consumerType);

        _queueMap.Add(queueName, new QueueDetails
        {
            ConsumerType = consumerType,
            MaxCompetingConsumers = maxCompetingConsumers,
            MaxDeliveryAttempts = maxDeliveryAttempts
        });
    }
}
