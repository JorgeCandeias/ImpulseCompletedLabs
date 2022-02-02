using Impulse.Models;
using Orleans;
using Orleans.Concurrency;
using System.Collections.Immutable;

namespace Impulse.Chat
{
    public interface IChannelGrain : IGrainWithStringKey
    {
        Task JoinAsync(string nickname);

        Task LeaveAsync(string nickname);

        Task MessageAsync(ChatMessage message);

        [AlwaysInterleave]
        Task<ImmutableArray<ChatMessage>> GetHistoryAsync();

        [AlwaysInterleave]
        Task<ImmutableArray<string>> GetMembersAsync();
    }
}