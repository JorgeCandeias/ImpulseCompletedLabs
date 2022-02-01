using Impulse.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Runtime;
using System.Collections.Immutable;

namespace Impulse.Chat
{
    internal class ChannelGrain : Grain, IChannelGrain
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

        public override Task OnActivateAsync()
        {
            _name = this.GetPrimaryKeyString();

            _logger.LogInformation("{Grain}#{Key} activated", nameof(ChannelGrain), _name);

            return base.OnActivateAsync();
        }

        public override Task OnDeactivateAsync()
        {
            _logger.LogInformation("{Grain}#{Key} deactivated", nameof(ChannelGrain), _name);

            return base.OnDeactivateAsync();
        }

        public async Task JoinAsync(string nickname)
        {
            if (_state.State.Members.Add(nickname))
            {
                await _state.WriteStateAsync();
            }
        }

        public async Task LeaveAsync(string nickname)
        {
            if (_state.State.Members.Remove(nickname))
            {
                await _state.WriteStateAsync();
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
    }
}