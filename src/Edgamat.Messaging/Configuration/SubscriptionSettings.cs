namespace Edgamat.Messaging.Configuration;

public class SubscriptionSettings
{
    public string TopicName { get; set; } = string.Empty;

    public string SubscriptionName { get; set; } = string.Empty;

    public Type ConsumerType { get; set; } = null!;

    public int MaxCompetingConsumers { get; set; } = Environment.ProcessorCount;

    public int MaxDeliveryAttempts { get; set; } = 3;

    public bool Enabled { get; set; } = true;
}
