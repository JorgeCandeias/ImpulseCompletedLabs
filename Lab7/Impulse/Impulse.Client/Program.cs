using Impulse.Chat;
using Impulse.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans;
using Orleans.Hosting;
using Spectre.Console;
using System.Reflection;
using static System.String;

var host = Host.CreateDefaultBuilder(args)
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
    .Build();

PrintUsage();

await host.StartAsync();

var client = host.Services.GetRequiredService<IClusterClient>();
var currentChannel = Empty;
var userName = AnsiConsole.Ask<string>("What is your [aqua]name[/]?");
var input = Empty;

do
{
    input = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(input)) continue;

    if (input.StartsWith("/j"))
    {
        await JoinChannel(input.Replace("/j", "").Trim());
    }
    else if (input.StartsWith("/n"))
    {
        var candidate = input.Replace("/n", "").Trim();
        if (IsNullOrWhiteSpace(candidate))
        {
            AnsiConsole.MarkupLine("[bold red]Error:[/] Specify a user name");
            continue;
        }

        userName = candidate;

        AnsiConsole.MarkupLine("[dim][[STATUS]][/] Set username to [lime]{0}[/]", userName);
    }
    else if (input.StartsWith("/l"))
    {
        await LeaveChannel();
    }
    else if (input.StartsWith("/h"))
    {
        await ShowCurrentChannelHistory();
    }
    else if (input.StartsWith("/m"))
    {
        await ShowChannelMembers();
    }
    else if (!input.StartsWith("/exit"))
    {
        await SendMessage(input);
    }
    else
    {
        if (AnsiConsole.Confirm("Do you really want to exit?"))
        {
            break;
        }
    }
} while (input != "/exit");

await host.StopAsync();

void PrintUsage()
{
    AnsiConsole.WriteLine();
    using var logoStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Impulse.Client.logo.png");
    var logo = new CanvasImage(logoStream!)
    {
        MaxWidth = 25
    };

    var table = new Table()
    {
        Border = TableBorder.None,
        Expand = true,
    }.HideHeaders();
    table.AddColumn(new TableColumn("One"));

    var header = new FigletText("Orleans")
    {
        Color = Color.Fuchsia
    };
    var header2 = new FigletText("Chat Room")
    {
        Color = Color.Aqua
    };

    var markup = new Markup(
       "[bold fuchsia]/j[/] [aqua]<channel>[/] to [underline green]join[/] a specific channel\n"
       + "[bold fuchsia]/n[/] [aqua]<username>[/] to set your [underline green]name[/]\n"
       + "[bold fuchsia]/l[/] to [underline green]leave[/] the current channel\n"
       + "[bold fuchsia]/h[/] to re-read channel [underline green]history[/]\n"
       + "[bold fuchsia]/m[/] to query [underline green]members[/] in the channel\n"
       + "[bold fuchsia]/exit[/] to exit\n"
       + "[bold aqua]<message>[/] to send a [underline green]message[/]\n");
    table.AddColumn(new TableColumn("Two"));
    var rightTable = new Table().HideHeaders().Border(TableBorder.None).AddColumn(new TableColumn("Content"));
    rightTable.AddRow(header).AddRow(header2).AddEmptyRow().AddEmptyRow().AddRow(markup);
    table.AddRow(logo, rightTable);

    AnsiConsole.Write(table);
    AnsiConsole.WriteLine();
}

async Task ShowChannelMembers()
{
    if (IsNullOrWhiteSpace(currentChannel))
    {
        AnsiConsole.MarkupLine("[bold red]Error:[/] No current channel");
        return;
    }

    var channel = client.GetGrain<IChannelGrain>(currentChannel);

    var members = await channel.GetMembersAsync();

    AnsiConsole.Write(new Rule($"Members for '{currentChannel}'")
    {
        Alignment = Justify.Center,
        Style = Style.Parse("darkgreen")
    });

    foreach (var member in members)
    {
        AnsiConsole.MarkupLine("[bold yellow]{0}[/]", member);
    }

    AnsiConsole.Write(new Rule()
    {
        Alignment = Justify.Center,
        Style = Style.Parse("darkgreen")
    });
}

async Task ShowCurrentChannelHistory()
{
    if (IsNullOrWhiteSpace(currentChannel))
    {
        AnsiConsole.MarkupLine("[bold red]Error:[/] No current channel");
        return;
    }

    var channel = client.GetGrain<IChannelGrain>(currentChannel);
    var history = await channel.GetHistoryAsync();

    AnsiConsole.Write(new Rule($"History for '{currentChannel}'")
    {
        Alignment = Justify.Center,
        Style = Style.Parse("darkgreen")
    });

    foreach (var chatMsg in history)
    {
        AnsiConsole.MarkupLine("[[[dim]{0}[/]]] [bold yellow]{1}:[/] {2}", chatMsg.Created.LocalDateTime, chatMsg.User, chatMsg.Text);
    }

    AnsiConsole.Write(new Rule()
    {
        Alignment = Justify.Center,
        Style = Style.Parse("darkgreen")
    });
}

async Task SendMessage(string messageText)
{
    if (IsNullOrWhiteSpace(currentChannel))
    {
        AnsiConsole.MarkupLine("[bold red]Error:[/] No current channel");
        return;
    };

    var channel = client.GetGrain<IChannelGrain>(currentChannel);
    await channel.MessageAsync(new ChatMessage
    {
        User = userName,
        Text = messageText
    });
}

async Task JoinChannel(string channelName)
{
    if (IsNullOrWhiteSpace(channelName))
    {
        AnsiConsole.MarkupLine("[bold red]Error:[/] specify a channel name");
    }

    if (!IsNullOrEmpty(currentChannel) && !string.Equals(currentChannel, channelName, StringComparison.OrdinalIgnoreCase))
    {
        AnsiConsole.MarkupLine("[bold olive]Leaving channel [/]{0}[bold olive] before joining [/]{1}", currentChannel, channelName);
        await LeaveChannel();
    }

    AnsiConsole.MarkupLine("[bold aqua]Joining channel [/]{0}", channelName);
    currentChannel = channelName;

    await AnsiConsole.Status().StartAsync("Joining channel...", async ctx =>
    {
        // get the grain reference for the channel
        var channel = client.GetGrain<IChannelGrain>(currentChannel);

        // join the channel
        await channel.JoinAsync(userName);
    });

    AnsiConsole.MarkupLine("[bold aqua]Joined channel [/]{0}", currentChannel);
}

async Task LeaveChannel()
{
    if (IsNullOrWhiteSpace(currentChannel))
    {
        AnsiConsole.MarkupLine("[bold red]Error:[/] No current channel");
        return;
    }

    AnsiConsole.MarkupLine("[bold olive]Leaving channel [/]{0}", currentChannel);

    await AnsiConsole.Status().StartAsync("Leaving channel...", async ctx =>
    {
        // get the grain reference for the channel
        var channel = client.GetGrain<IChannelGrain>(currentChannel);

        // join the channel
        await channel.LeaveAsync(userName);
    });

    AnsiConsole.MarkupLine("[bold olive]Left channel [/]{0}", currentChannel);
}