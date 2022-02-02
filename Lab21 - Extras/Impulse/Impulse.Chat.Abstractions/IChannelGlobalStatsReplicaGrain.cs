using CommunityToolkit.Diagnostics;
using Impulse.Models;
using Orleans;

namespace Impulse.Chat
{
    public interface IChannelGlobalStatsReplicaGrain : IGrainWithGuidKey
    {
        Task<ClusterChatStats> GetStatsAsync();
    }

    public static class IChannelStatReplicasGrainFactoryExtensions
    {
        public static IChannelGlobalStatsReplicaGrain GetChannelGlobalStatsReplicaGrain(this IGrainFactory factory)
        {
            Guard.IsNotNull(factory, nameof(factory));

            return factory.GetGrain<IChannelGlobalStatsReplicaGrain>(Guid.Empty);
        }
    }
}