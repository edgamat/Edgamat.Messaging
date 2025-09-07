namespace Edgamat.Messaging.Configuration;

public partial class AzureServiceBusConfiguration
{
    public const string DefaultConfigurationSection = "AzureServiceBus";

    public string ConnectionString { get; set; } = string.Empty;

    public List<QueueConfiguration> Queues { get; set; } = [];

    public List<SubscriptionConfiguration> Subscriptions { get; set; } = [];

    public class SubscriptionConfiguration
    {
        public string TopicName { get; set; } = string.Empty;

        public string SubscriptionName { get; set; } = string.Empty;

        public Type ConsumerType { get; set; } = null!;

        public int MaxCompetingConsumers { get; set; } = Environment.ProcessorCount;

        public bool Enabled { get; set; } = true;
    }
}
