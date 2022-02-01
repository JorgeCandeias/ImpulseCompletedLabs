using Impulse.Chat;
using Impulse.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NBomber.Contracts;
using NBomber.CSharp;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Providers;
using Spectre.Console;

using var host = Host.CreateDefaultBuilder()
    .UseEnvironment("Production")
    .ConfigureServices(x => x.Configure<ConsoleLifetimeOptions>(options => options.SuppressStatusMessages = true))
    .Build();
await host.StartAsync();
var environment = host.Services.GetRequiredService<IHostEnvironment>();
var configuration = host.Services.GetRequiredService<IConfiguration>();
await host.StopAsync();

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

var client = builder
    .AddMemoryStreams<DefaultMemoryMessageBodySerializer>("Chat")
    .Build();

await AnsiConsole.Status().StartAsync("Connecting to server...", async ctx =>
{
    ctx.Spinner(Spinner.Known.Dots);
    ctx.Status = "Connecting...";

    await client.Connect(async error =>
    {
        AnsiConsole.MarkupLine("[bold red]Error:[/] error connecting to server!");
        AnsiConsole.WriteException(error);

        ctx.Status = "Waiting to retry...";

        await Task.Delay(TimeSpan.FromSeconds(1));

        ctx.Status = "Retrying connection...";
        return true;
    });

    ctx.Status = "Connected!";
});

var grain = client.GetGrain<IChannelGrain>("overload");
await grain.JoinAsync("user");

var messageStep = Step.Create("Message", async context =>
{
    await grain.MessageAsync(new ChatMessage
    {
        User = "user",
        Text = Guid.NewGuid().ToString()
    });
    return Response.Ok();
});
var messageScenario = ScenarioBuilder
    .CreateScenario("Message", messageStep)
    .WithLoadSimulations(Simulation.KeepConstant(10, TimeSpan.FromSeconds(10)));

var historyStep = Step.Create("History", async context =>
{
    await grain.GetHistoryAsync();
    return Response.Ok(statusCode: 200);
});
var historyScenario = ScenarioBuilder
    .CreateScenario("History", historyStep)
    .WithLoadSimulations(Simulation.KeepConstant(10, TimeSpan.FromSeconds(10)));

do
{
    Console.WriteLine("Type [ENTER] to start load testing or [ESC] to quit");
    var key = Console.ReadKey();
    if (key.Key == ConsoleKey.Escape)
    {
        await client.Close();
        return;
    }
    if (key.Key == ConsoleKey.Enter)
    {
        NBomberRunner.RegisterScenarios(messageScenario).DisableHintsAnalyzer().Run();
        NBomberRunner.RegisterScenarios(historyScenario).DisableHintsAnalyzer().Run();
        NBomberRunner.RegisterScenarios(messageScenario, historyScenario).DisableHintsAnalyzer().Run();
    }
} while (true);