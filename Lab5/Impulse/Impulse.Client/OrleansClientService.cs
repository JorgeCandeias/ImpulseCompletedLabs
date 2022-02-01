using Microsoft.Extensions.Hosting;
using Orleans;

internal class OrleansClientService : IHostedService
{
    public IClusterClient Client { get; }

    public OrleansClientService()
    {
        Client = new ClientBuilder()
            .UseLocalhostClustering()
            .Build();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Orleans Client connecting...");

        await Client.Connect(async error =>
        {
            Console.WriteLine(error.ToString());
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            return true;
        });

        Console.WriteLine("Orleans Client connected!");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await Client.Close();

        Console.WriteLine("Orleans Client disconnected!");
    }
}