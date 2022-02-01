using Impulse.Models;
using Orleans;
using System.Collections.Immutable;

namespace Impulse.Chat
{
    public interface IChannelGrain : IGrainWithStringKey
    {
        Task JoinAsync(string nickname);

        Task LeaveAsync(string nickname);

        Task MessageAsync(ChatMessage message);

        Task<ImmutableArray<ChatMessage>> GetHistoryAsync();

        Task<ImmutableArray<string>> GetMembersAsync();
    }
}