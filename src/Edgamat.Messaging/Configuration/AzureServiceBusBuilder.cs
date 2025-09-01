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
    private readonly QueueMap _queueConsumers = new();

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

    public AzureServiceBusBuilder AddConsumer<TConsumer>(string queueName) where TConsumer : class, IConsumer
    {
        _services.AddScoped<TConsumer>();
        _queueConsumers.Add(queueName, typeof(TConsumer));

        return this;
    }

    public AzureServiceBusBuilder AddConsumer<TConsumer, TMessage>(string queueName)
        where TConsumer : class, IConsumer<TMessage>
        where TMessage : class
    {
        _services.AddScoped<IConsumer<TMessage>, TConsumer>();
        _queueConsumers.Add(queueName, typeof(TConsumer));

        return this;
    }

    public IServiceCollection Build()
    {
        if (_configuration != null && _configurationSection != null)
        {
            _services.Configure<AzureServiceBusConfiguration>(_configuration.GetSection(_configurationSection));
        }

        _services.AddSingleton(provider =>
        {
            var options = provider.GetRequiredService<IOptions<AzureServiceBusConfiguration>>().Value;
            return new ServiceBusClient(options.ConnectionString);
        });

        _services.AddSingleton(_queueConsumers);

        foreach (var _queueConsumer in _queueConsumers)
        {
            var messageType = _queueConsumer.Value?.BaseType?.GenericTypeArguments[0] ?? throw new AzureServiceBusConfigurationException($"Could not determine message type for consumer '{_queueConsumer.Value?.FullName}'");

            var messageConsumerType = typeof(IConsumer<>).MakeGenericType(messageType);
            _services.AddScoped(messageConsumerType, _queueConsumer.Value);
        }

        return _services;
    }
}
