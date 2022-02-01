using Impulse.Models;
using Orleans;
using System.Collections.Immutable;

namespace Impulse.Chat
{
    internal class ChannelGrain : Grain, IChannelGrain
    {
        private readonly Queue<ChatMessage> _messages = new();
        public readonly HashSet<string> _members = new(StringComparer.OrdinalIgnoreCase);

        public Task JoinAsync(string nickname)
        {
            _members.Add(nickname);

            return Task.CompletedTask;
        }

        public Task LeaveAsync(string nickname)
        {
            _members.Remove(nickname);

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