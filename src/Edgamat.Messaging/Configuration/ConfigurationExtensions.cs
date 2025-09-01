using Microsoft.Extensions.DependencyInjection;

namespace Edgamat.Messaging.Configuration;

public static class ConfigurationExtensions
{
    public static AzureServiceBusBuilder AddAzureServiceBus(this IServiceCollection services)
    {
        return new AzureServiceBusBuilder(services);
    }
}
