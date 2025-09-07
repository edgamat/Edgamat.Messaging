using Azure.Messaging.ServiceBus;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Edgamat.Messaging.Configuration;

public class AzureServiceBusBuilder
{
    private readonly IServiceCollection _services;
    private IConfiguration? _configuration;
    private string? _configurationSection;
    private readonly QueueMap _queueMap = new();

    public AzureServiceBusBuilder(IServiceCollection services)
    {
        _services = services;
    }

    public AzureServiceBusBuilder WithConfiguration(
        IConfiguration configuration,
        string configurationSection = AzureServiceBusConfiguration.DefaultConfigurationSection)
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
        _services.AddScoped<TConsumer>();

        _queueMap.Add(queueName, new QueueDetails
        {
            ConsumerType = typeof(TConsumer),
            MaxCompetingConsumers = maxCompetingConsumers,
            MaxDeliveryAttempts = maxDeliveryAttempts
        });

        return this;
    }

    public AzureServiceBusBuilder AddConsumer<TConsumer>(string queueName) where TConsumer : class, IConsumer
    {
        return AddConsumer<TConsumer>(queueName, Environment.ProcessorCount);
    }

    public IServiceCollection Build()
    {
        if (_configuration == null || _configurationSection == null)
            throw new AzureServiceBusConfigurationException("Configuration and ConfigurationSection must be set.");

        var settings = new AzureServiceBusConfiguration();
        _configuration.GetSection(_configurationSection).Bind(settings);
        _services.AddSingleton(settings);

        // Register enabled consumers from configuration
        foreach (var queue in settings.Queues.Where(q => q.Enabled))
        {
            var consumerType = Type.GetType(queue.ConsumerType)
                ?? throw new AzureServiceBusConfigurationException($"Could not load consumer type '{queue.ConsumerType}' for queue '{queue.QueueName}'");

            _services.AddScoped(consumerType);

            _queueMap.Add(queue.QueueName, new QueueDetails
            {
                ConsumerType = consumerType,
                MaxCompetingConsumers = queue.MaxCompetingConsumers,
                MaxDeliveryAttempts = queue.MaxDeliveryAttempts
            });
        }

        _services.AddSingleton(provider =>
        {
            var settings = provider.GetRequiredService<AzureServiceBusConfiguration>();
            return new ServiceBusClient(settings.ConnectionString);
        });

        _services.AddSingleton(_queueMap);

        return _services;
    }
}
