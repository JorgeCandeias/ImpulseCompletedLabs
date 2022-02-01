using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Providers;
using Spectre.Console;

internal class OrleansClientService : IHostedService
{
    public IClusterClient Client { get; }

    public OrleansClientService(IHostEnvironment environment, IConfiguration configuration)
    {
        var builder = new ClientBuilder();

        if (environment.IsDevelopment())
        {
            builder
                .UseLocalhostClustering();
        }
        else
        {
            builder
                .UseAdoNetClustering(options =>
                {
                    options.Invariant = "Microsoft.Data.SqlClient";
                    options.ConnectionString = configuration.GetConnectionString("Orleans");
                })
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = nameof(Impulse);
                    options.ServiceId = nameof(Impulse);
                });
        }

        Client = builder
            .AddMemoryStreams<DefaultMemoryMessageBodySerializer>("Chat")
            .Build();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await AnsiConsole.Status().StartAsync("Connecting to server...", async ctx =>
        {
            ctx.Spinner(Spinner.Known.Dots);
            ctx.Status = "Connecting...";

            await Client.Connect(async error =>
            {
                AnsiConsole.MarkupLine("[bold red]Error:[/] error connecting to server!");
                AnsiConsole.WriteException(error);

                ctx.Status = "Waiting to retry...";

                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);

                ctx.Status = "Retrying connection...";
                return true;
            });

            ctx.Status = "Connected!";
        });
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await AnsiConsole.Status().StartAsync("Disconnecting...", async ctx =>
        {
            ctx.Spinner(Spinner.Known.Dots);
            await Client.Close();
        });
    }
}