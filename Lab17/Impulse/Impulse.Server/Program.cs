using Impulse.Chat;
using Impulse.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Orleans;
using Orleans.Hosting;
using Orleans.Providers;
using System.Diagnostics;
using System.Net;

if (!TcpPortFinder.TryGetAvailablePort(11111, 11999, out var siloPort) ||
    !TcpPortFinder.TryGetAvailablePort(30000, 30999, out var gatewayPort) ||
    !TcpPortFinder.TryGetAvailablePort(40000, 40999, out var dashboardPort))
{
    throw new InvalidOperationException();
}

Console.Title = $"Silo: {siloPort}, Gateway: {gatewayPort}, Dashboard: {dashboardPort}";

await Host.CreateDefaultBuilder(args)
    //.ConfigureLogging((context, logging) => logging.ClearProviders())
    .UseOrleans((context, builder) =>
    {
        builder
            .UseLocalhostClustering(siloPort, gatewayPort, new IPEndPoint(IPAddress.Loopback, 11111))
            .AddMemoryGrainStorageAsDefault()
            .AddMemoryGrainStorage("PubSubStore")
            .AddMemoryStreams<DefaultMemoryMessageBodySerializer>("Chat")
            .UseInMemoryReminderService()
            .AddIncomingGrainCallFilter<ActivityGrainCallFilter>()
            .UseDashboard(options =>
            {
                options.Port = dashboardPort;
            });
    })
    .ConfigureServices(services =>
    {
        services.AddOpenTelemetryTracing(builder =>
        {
            builder
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(nameof(Impulse)))
                .AddSource(nameof(Impulse))
                .AddConsoleExporter();
        });
        services.AddSingleton(sp => new ActivitySource(nameof(Impulse)));
    })
    .RunConsoleAsync();