using Impulse.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Runtime;
using Orleans.Streams;
using System.Collections.Immutable;

namespace Impulse.Chat
{
    internal class ChannelGrain : Grain, IChannelGrain, IRemindable
    {
        private readonly ILogger _logger;
        private readonly ChannelOptions _options;
        private readonly IPersistentState<ChannelGrainState> _state;

        public ChannelGrain(
            ILogger<ChannelGrain> logger,
            IOptions<ChannelOptions> options,
            [PersistentState("State")] IPersistentState<ChannelGrainState> state)
        {
            _logger = logger;
            _options = options.Value;
            _state = state;
        }

        private string _name = null!;

        private IAsyncStream<ChatMessage> _stream = null!;

        private IGrainReminder _reminder = null!;

        public override async Task OnActivateAsync()
        {
            _name = this.GetPrimaryKeyString();

            _stream = GetStreamProvider("Chat").GetStream<ChatMessage>(Guid.Empty, _name);

            RegisterTimer(state =>
            {
                _logger.LogInformation(
                    "Channel '{Channel}' active with {Members} members and {Messages} cached messages",
                    _name, _state.State.Members.Count, _state.State.Messages.Count);

                return Task.CompletedTask;
            }, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));

            _reminder = await RegisterOrUpdateReminder("Clock", TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));

            _logger.LogInformation(
                "{Grain}#{Key} activated {ActivityId}/{ClientId}",
                nameof(ChannelGrain),
                _name,
                RequestContext.ActivityId,
                RequestContext.Get("ClientId"));

            await base.OnActivateAsync();
        }

        public override async Task OnDeactivateAsync()
        {
            await UnregisterReminder(_reminder);

            _logger.LogInformation("{Grain}#{Key} deactivated", nameof(ChannelGrain), _name);

            await base.OnDeactivateAsync();
        }

        public async Task JoinAsync(string nickname)
        {
            if (_state.State.Members.Add(nickname))
            {
                await _state.WriteStateAsync();

                await _stream.OnNextAsync(new ChatMessage
                {
                    User = "System",
                    Text = $"{nickname} joins channel '{_name}' ..."
                });
            }
        }

        public async Task LeaveAsync(string nickname)
        {
            if (_state.State.Members.Remove(nickname))
            {
                await _state.WriteStateAsync();

                await _stream.OnNextAsync(new ChatMessage
                {
                    User = "System",
                    Text = $"{nickname} leaves channel '{_name}'..."
                });
            }

            if (_state.State.Members.Count == 0)
            {
                DeactivateOnIdle();
            }
        }

        public async Task MessageAsync(ChatMessage message)
        {
            // cache the new message
            _state.State.Messages.Enqueue(message);

            // clear any excess messages
            while (_state.State.Messages.Count > _options.MaxCachedMessages)
            {
                _state.State.Messages.Dequeue();
            }

            await _state.WriteStateAsync();

            await _stream.OnNextAsync(message);
        }

        public Task<ImmutableArray<string>> GetMembersAsync()
        {
            var result = _state.State.Members.ToImmutableArray();

            return Task.FromResult(result);
        }

        public Task<ImmutableArray<ChatMessage>> GetHistoryAsync()
        {
            var result = _state.State.Messages.ToImmutableArray();

            return Task.FromResult(result);
        }

        private Task SendClockAsync()
        {
            return _stream.OnNextAsync(new ChatMessage
            {
                User = "System",
                Text = $"{DateTime.UtcNow:u}: {_name} online with {_state.State.Members.Count} members active"
            });
        }

        public Task ReceiveReminder(string reminderName, TickStatus status)
        {
            switch (reminderName)
            {
                case "Clock":
                    return SendClockAsync();

                default:
                    break;
            }

            return Task.CompletedTask;
        }
    }
}