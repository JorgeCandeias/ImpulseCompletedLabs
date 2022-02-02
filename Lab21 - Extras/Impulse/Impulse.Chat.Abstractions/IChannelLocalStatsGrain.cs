using CommunityToolkit.Diagnostics;
using Impulse.Models;
using Orleans;

namespace Impulse.Chat
{
    public interface IChannelLocalStatsGrain : IGrainWithGuidKey
    {
        Task PublishAsync(ChatStats stats);
    }

    public static class IChannelLocalStatsGrainFactoryExtensions
    {
        public static IChannelLocalStatsGrain GetChannelLocalStatsGrain(this IGrainFactory factory)
        {
            Guard.IsNotNull(factory, nameof(factory));

            return factory.GetGrain<IChannelLocalStatsGrain>(Guid.Empty);
        }
    }
}