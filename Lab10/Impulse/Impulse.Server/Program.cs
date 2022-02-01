using Impulse.Server;
using Microsoft.Extensions.Hosting;
using Orleans;
using Orleans.Hosting;
using System.Net;

if (!TcpPortFinder.TryGetAvailablePort(11111, 11999, out var siloPort) ||
    !TcpPortFinder.TryGetAvailablePort(30000, 30999, out var gatewayPort) ||
    !TcpPortFinder.TryGetAvailablePort(40000, 40999, out var dashboardPort))
{
    throw new InvalidOperationException();
}

Console.Title = $"Silo: {siloPort}, Gateway: {gatewayPort}, Dashboard: {dashboardPort}";

await Host.CreateDefaultBuilder(args)
    .UseOrleans((context, builder) =>
    {
        builder
            .UseLocalhostClustering(siloPort, gatewayPort, new IPEndPoint(IPAddress.Loopback, 11111))
            .AddMemoryGrainStorageAsDefault()
            .UseDashboard(options =>
            {
                options.Port = dashboardPort;
            });
    })
    .RunConsoleAsync();