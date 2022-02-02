using Impulse.Models;
using Orleans;
using Orleans.Concurrency;

namespace Impulse.Chat
{
    [Reentrant]
    [StatelessWorker(1)]
    internal class ChannelGlobalStatsReplicaGrain : Grain, IChannelGlobalStatsReplicaGrain
    {
        private ClusterChatStats _stats = null!;
        private Guid _version;

        public override async Task OnActivateAsync()
        {
            (_stats, _version) = await GrainFactory.GetChannelGlobalStatsGrain().GetStatsAsync();

            RegisterTimer(async _ =>
            {
                var result = await GrainFactory.GetChannelGlobalStatsGrain().PollStatsAsync(_version);
                if (result.HasValue)
                {
                    (_stats, _version) = result.Value;
                }
            }, null, TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(1));

            await base.OnActivateAsync();
        }

        public Task<ClusterChatStats> GetStatsAsync()
        {
            return Task.FromResult(_stats);
        }
    }
}