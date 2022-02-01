using Impulse.Models;
using Orleans.Streams;
using Spectre.Console;

namespace Impulse.Client
{
    internal class StreamObserver : IAsyncObserver<ChatMessage>
    {
        private readonly string _channel;

        public StreamObserver(string channel)
        {
            _channel = channel;
        }

        public Task OnCompletedAsync() => Task.CompletedTask;

        public Task OnErrorAsync(Exception ex)
        {
            AnsiConsole.WriteException(ex);
            return Task.CompletedTask;
        }

        public Task OnNextAsync(ChatMessage item, StreamSequenceToken token)
        {
            AnsiConsole.MarkupLine(
                "[[[dim]{0}[/]]] [bold green]{1}[/] [bold yellow]{2}:[/] {3}",
                item.Created.LocalDateTime,
                _channel,
                item.User,
                item.Text);

            return Task.CompletedTask;
        }
    }
}