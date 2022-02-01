using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

await Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services
            .AddSingleton<OrleansClientService>()
            .AddSingleton<IHostedService>(sp => sp.GetRequiredService<OrleansClientService>())
            .AddSingleton(sp => sp.GetRequiredService<OrleansClientService>().Client);

        services
            .Configure<ConsoleLifetimeOptions>(options =>
            {
                options.SuppressStatusMessages = true;
            });
    })
    .RunConsoleAsync();