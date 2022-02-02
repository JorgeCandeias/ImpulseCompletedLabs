using CommunityToolkit.Diagnostics;
using Impulse.Models;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Concurrency;
using OrleansDashboard;

namespace Impulse.Chat
{
    [Reentrant]
    internal partial class ChannelGlobalStatsGrain : Grain, IChannelGlobalStatsGrain
    {
        private readonly ILogger _logger;

        public ChannelGlobalStatsGrain(ILogger<ChannelGlobalStatsGrain> logger)
        {
            _logger = logger;
        }

        private readonly Dictionary<Guid, (SiloChatStats Stats, DateTime Timestamp)> _stats = new();
        private int _channels;
        private int _members;
        private int _messages;
        private Guid _version = Guid.NewGuid();
        private TaskCompletionSource<(ClusterChatStats Stats, Guid Version)> _completion = new();

        public override Task OnActivateAsync()
        {
            RegisterTimer(_ => LogStatsAsync(), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

            RegisterTimer(_ => CleanupAsync(), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

            return base.OnActivateAsync();
        }

        public Task PublishAsync(SiloChatStats stats)
        {
            Guard.IsNotNull(stats, nameof(stats));

            if (_stats.TryGetValue(stats.Id, out var old))
            {
                _channels -= old.Stats.Channels;
                _members -= old.Stats.Members;
                _messages -= old.Stats.Messages;
            }

            _channels += stats.Channels;
            _members += stats.Members;
            _messages += stats.Messages;

            _stats[stats.Id] = (stats, DateTime.UtcNow);

            // fullfill pending reactive requests
            _version = Guid.NewGuid();
            _completion.SetResult((new ClusterChatStats(_channels, _members, _messages), _version));
            _completion = new();

            return Task.CompletedTask;
        }

        public Task<(ClusterChatStats Stats, Guid Version)> GetStatsAsync()
        {
            return Task.FromResult((new ClusterChatStats(_channels, _members, _messages), _version));
        }

        [NoProfiling]
        public async Task<(ClusterChatStats Stats, Guid Version)?> PollStatsAsync(Guid current)
        {
            // resolve the request immediately if the caller has a different version
            if (current != _version)
            {
                return await GetStatsAsync();
            }

            // pin the completion to avoid reentrancy issues
            var completion = _completion;

            // wait for the completion to resolve or a timeout
            if (await Task.WhenAny(completion.Task, Task.Delay(TimeSpan.FromSeconds(10))) == completion.Task)
            {
                // fullfill the request with the new data version
                return await completion.Task;
            }
            else
            {
                // this means we dont have anything new
                return null;
            }
        }

        private Task CleanupAsync()
        {
            var limit = DateTime.UtcNow.AddSeconds(-10);
            foreach (var item in _stats)
            {
                if (item.Value.Timestamp < limit)
                {
                    _stats.Remove(item.Key);
                    _channels -= item.Value.Stats.Channels;
                    _members -= item.Value.Stats.Members;
                    _messages -= item.Value.Stats.Messages;

                    return Task.CompletedTask;
                }
            }

            return Task.CompletedTask;
        }

        private Task LogStatsAsync()
        {
            LogStats(nameof(ChannelGlobalStatsGrain), _members, _messages);

            return Task.CompletedTask;
        }

        [LoggerMessage(1, LogLevel.Information, "{Grain} reports total {Members} members and {Messages} across the cluster")]
        private partial void LogStats(string grain, int members, int messages);
    }
}