namespace Edgamat.Messaging.Configuration;

public class SubscriptionDetails
{
    public Type ConsumerType { get; set; } = null!;

    public int MaxCompetingConsumers { get; set; } = Environment.ProcessorCount;

    public int MaxDeliveryAttempts { get; set; } = 3;
}
