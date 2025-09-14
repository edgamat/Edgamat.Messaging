namespace Edgamat.Messaging.Configuration;

public class AzureServiceBusSettings
{
    public const string DefaultConfigurationSection = "AzureServiceBus";

    public string ConnectionString { get; set; } = string.Empty;

    public List<QueueSettings> Queues { get; set; } = [];

    public List<SubscriptionSettings> Subscriptions { get; set; } = [];
}
