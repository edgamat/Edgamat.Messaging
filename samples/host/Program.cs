using Microsoft.Extensions.Hosting;
using Edgamat.Messaging.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Edgamat.Messaging.Samples.Host;

var builder = Host.CreateApplicationBuilder(args);
builder.ConfigureOpenTelemetry();

builder.Services.AddAzureServiceBus()
    .WithConfiguration(builder.Configuration)
    .AddPublisher()
    .AddBusConsumersHostedService()
    .Build();

builder.Services.AddHostedService<Worker>();

var app = builder.Build();

app.Run();
