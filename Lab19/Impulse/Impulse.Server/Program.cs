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
using Orleans.Statistics;
using System.Diagnostics;
using System.Net;

if (!TcpPortFinder.TryGetAvailablePort(11111, 11999, out var siloPort) ||
    !TcpPortFinder.TryGetAvailablePort(30000, 30999, out var gatewayPort) ||
    !TcpPortFinder.TryGetAvailablePort(40000, 40999, out var dashboardPort))
{
    throw new InvalidOperationException();
}

Console.Title = $"Silo: {siloPort}, Gateway: {gatewayPort}, Dashboard: {dashboardPort}";

await Host
    .CreateDefaultBuilder(args)
    .UseEnvironment("Development")
    .ConfigureLogging((context, logging) =>
    {
        if (context.HostingEnvironment.IsDevelopment())
        {
            // noop
        }
        else
        {
            logging.ClearProviders();
        }
    })
    .UseOrleans((context, builder) =>
    {
        if (context.HostingEnvironment.IsDevelopment())
        {
            builder
                .UseLocalhostClustering(siloPort, gatewayPort, new IPEndPoint(IPAddress.Loopback, 11111))
                .AddMemoryGrainStorageAsDefault()
                .AddMemoryGrainStorage("PubSubStore")
                .UseInMemoryReminderService();
        }
        else
        {
            // todo
        }

        builder
            .AddMemoryStreams<DefaultMemoryMessageBodySerializer>("Chat")
            .AddIncomingGrainCallFilter<ActivityGrainCallFilter>()
            .UseDashboard(options =>
            {
                options.Port = dashboardPort;
            });

        if (OperatingSystem.IsWindows())
        {
            builder.AddPerfCountersTelemetryConsumer();
        }
        else if (OperatingSystem.IsLinux())
        {
            builder.UseLinuxEnvironmentStatistics();
        }
    })
    .ConfigureServices((context, services) =>
    {
        services.AddOpenTelemetryTracing(builder =>
        {
            builder
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(nameof(Impulse)))
                .AddSource(nameof(Impulse));

            if (context.HostingEnvironment.IsDevelopment())
            {
                builder.AddConsoleExporter();
            }
        });
        services.AddSingleton(sp => new ActivitySource(nameof(Impulse)));
    })
    .RunConsoleAsync();