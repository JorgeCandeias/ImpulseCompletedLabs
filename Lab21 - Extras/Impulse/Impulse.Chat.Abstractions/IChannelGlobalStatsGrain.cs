using CommunityToolkit.Diagnostics;
using Impulse.Models;
using Orleans;

namespace Impulse.Chat
{
    public interface IChannelGlobalStatsGrain : IGrainWithGuidKey
    {
        Task PublishAsync(SiloChatStats stats);

        Task<(ClusterChatStats Stats, Guid Version)> GetStatsAsync();

        Task<(ClusterChatStats Stats, Guid Version)?> PollStatsAsync(Guid current);
    }

    public static class IChannelStatsGrainFactoryExtensions
    {
        public static IChannelGlobalStatsGrain GetChannelGlobalStatsGrain(this IGrainFactory factory)
        {
            Guard.IsNotNull(factory, nameof(factory));

            return factory.GetGrain<IChannelGlobalStatsGrain>(Guid.Empty);
        }
    }
}