using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Edgamat.Messaging.Configuration;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((ctx, cfg) =>
    {
        cfg.AddJsonFile("appsettings.json", optional: true);
    })
    .ConfigureServices((ctx, services) =>
    {
        services.AddAzureServiceBus()
            .WithConfiguration(ctx.Configuration)
            .AddBusConsumersHostedService()
            .Build();
    })
    .Build();

await host.RunAsync();
