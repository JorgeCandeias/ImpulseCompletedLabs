using CommunityToolkit.Diagnostics;
using Impulse.Models;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Concurrency;

namespace Impulse.Chat
{
    [StatelessWorker(1)]
    internal partial class ChannelLocalStatsGrain : Grain, IChannelLocalStatsGrain
    {
        private readonly ILogger _logger;

        public ChannelLocalStatsGrain(ILogger<ChannelGlobalStatsGrain> logger)
        {
            _logger = logger;
        }

        private readonly Guid _id = Guid.NewGuid();
        private readonly Dictionary<string, (ChatStats Stats, DateTime Timestamp)> _stats = new();
        private int _members;
        private int _messages;

        public override Task OnActivateAsync()
        {
            RegisterTimer(_ => LogStatsAsync(), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

            RegisterTimer(_ => CleanupAsync(), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

            RegisterTimer(_ => PushAsync(), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

            return base.OnActivateAsync();
        }

        public Task PublishAsync(ChatStats stats)
        {
            Guard.IsNotNull(stats, nameof(stats));

            if (_stats.TryGetValue(stats.Name, out var old))
            {
                _members -= old.Stats.Members;
                _messages -= old.Stats.Messages;
            }

            _members += stats.Members;
            _messages += stats.Messages;

            _stats[stats.Name] = (stats, DateTime.UtcNow);

            return Task.CompletedTask;
        }

        private Task CleanupAsync()
        {
            var limit = DateTime.UtcNow.AddSeconds(-10);
            foreach (var item in _stats)
            {
                if (item.Value.Timestamp < limit)
                {
                    _stats.Remove(item.Key);
                    _members -= item.Value.Stats.Members;
                    _messages -= item.Value.Stats.Messages;

                    return Task.CompletedTask;
                }
            }

            return Task.CompletedTask;
        }

        private Task PushAsync()
        {
            return GrainFactory
                .GetChannelGlobalStatsGrain()
                .PublishAsync(new SiloChatStats(_id, _stats.Count, _members, _messages));
        }

        private Task LogStatsAsync()
        {
            LogStats(nameof(ChannelGlobalStatsGrain), _members, _messages);

            return Task.CompletedTask;
        }

        [LoggerMessage(1, LogLevel.Information, "{Grain} reports total {Members} members and {Messages} on the local silo")]
        private partial void LogStats(string grain, int members, int messages);
    }
}