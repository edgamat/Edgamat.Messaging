using Microsoft.Extensions.Hosting;
using Edgamat.Messaging.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Edgamat.Messaging.Samples.Host;
using Microsoft.Extensions.Configuration;

var builder = Host.CreateApplicationBuilder(args);

// include appsettings.json and environment variables in configuration
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.ConfigureOpenTelemetry();

builder.Services.AddAzureServiceBus()
    .WithConfiguration(builder.Configuration)
    .AddPublisher()
    .AddBusConsumersHostedService()
    .Build();

builder.Services.AddHostedService<Worker>();

var app = builder.Build();

app.Run();
