using Microsoft.Extensions.DependencyInjection;

namespace Edgamat.Messaging.Configuration;

public class AzureServiceBusConfiguration
{
    public const string DefaultConfigurationSection = "AzureServiceBus";

    public string ConnectionString { get; set; } = string.Empty;
}
