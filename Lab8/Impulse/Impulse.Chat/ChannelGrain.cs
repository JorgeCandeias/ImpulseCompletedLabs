using Impulse.Models;
using Microsoft.Extensions.Logging;
using Orleans;
using System.Collections.Immutable;

namespace Impulse.Chat
{
    internal class ChannelGrain : Grain, IChannelGrain
    {
        private readonly Queue<ChatMessage> _messages = new();
        private readonly HashSet<string> _members = new(StringComparer.OrdinalIgnoreCase);
        private readonly ILogger _logger;

        public ChannelGrain(ILogger<ChannelGrain> logger)
        {
            _logger = logger;
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

        public Task JoinAsync(string nickname)
        {
            _members.Add(nickname);

            return Task.CompletedTask;
        }

        public Task LeaveAsync(string nickname)
        {
            _members.Remove(nickname);

            if (_members.Count == 0)
            {
                DeactivateOnIdle();
            }

            return Task.CompletedTask;
        }

        public Task MessageAsync(ChatMessage message)
        {
            // cache the new message
            _messages.Enqueue(message);

            // clear any excess messages
            while (_messages.Count > 100)
            {
                _messages.Dequeue();
            }

            // we cant broadcast the message yet...
            return Task.CompletedTask;
        }

        public Task<ImmutableArray<string>> GetMembersAsync()
        {
            var result = _members.ToImmutableArray();

            return Task.FromResult(result);
        }

        public Task<ImmutableArray<ChatMessage>> GetHistoryAsync()
        {
            var result = _messages.ToImmutableArray();

            return Task.FromResult(result);
        }
    }
}