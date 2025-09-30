using System.Diagnostics;

using Azure.Messaging.ServiceBus;

namespace Edgamat.Messaging;

public static class DiagnosticActivityExtensions
{
    public static void EnrichWithContext<T>(this Activity? activity, string queueOrTopicName) where T : class
    {
        if (activity == null) return;

        activity.SetTag("messaging.system", "edgamat.azureservicebus");
        activity.SetTag("messaging.destination.name", queueOrTopicName);
        activity.SetTag("messaging.message.type", typeof(T).FullName);
    }

    public static void EnrichWithMessage(this Activity? activity, ServiceBusMessage message)
    {
        if (activity == null) return;

        activity?.SetTag("messaging.message.id", message.MessageId);
    }

    public static void EnrichWithContext<T>(this Activity? activity, MessageContext messageContext) where T : class
    {
        if (activity == null) return;

        activity.SetTag("messaging.system", "edgamat.azureservicebus");
        activity.SetTag("messaging.destination.name", messageContext.QueueOrTopicName);
        activity.SetTag("messaging.message.type", typeof(T).FullName);
        activity.SetTag("messaging.message.id", messageContext.MessageId);
        activity.SetTag("messaging.delivery_attempt", messageContext.DeliveryAttempt);
        activity.SetTag("messaging.max_delivery_attempts", messageContext.MaxDeliveryAttempts);
    }
}
