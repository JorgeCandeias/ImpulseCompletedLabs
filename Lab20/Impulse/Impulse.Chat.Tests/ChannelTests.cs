using Impulse.Models;
using Orleans.Streams;
using Orleans.TestingHost;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Impulse.Chat.Tests
{
    [Collection(nameof(ClusterCollection))]
    public class ChannelTests
    {
        private readonly TestCluster _cluster;

        public ChannelTests(ClusterFixture cluster)
        {
            _cluster = cluster.Cluster;
        }

        [Fact]
        public async Task MemberJoinChannel()
        {
            // arrange
            var channel = Guid.NewGuid().ToString();
            var user = Guid.NewGuid().ToString();
            var grain = _cluster.Client.GetGrain<IChannelGrain>(channel);

            var completion = new TaskCompletionSource<ChatMessage>();
            var stream = await _cluster.Client
                .GetStreamProvider("Chat")
                .GetStream<ChatMessage>(Guid.Empty, channel)
                .SubscribeAsync((chat, token) =>
                {
                    completion.TrySetResult(chat);
                    return Task.CompletedTask;
                });

            // act
            await grain.JoinAsync(user);

            // assert - member list includes the user
            var members = await grain.GetMembersAsync();
            Assert.Collection(members, x => Assert.Equal(user, x));

            // assert - notification arrived
            var received = await Task.WhenAny(completion.Task, Task.Delay(1000));
            Assert.Same(completion.Task, received);
            var result = await completion.Task;
            Assert.Equal("System", result.User);
            Assert.Equal($"{user} joins channel '{channel}' ...", result.Text);

            // clean up
            await stream.UnsubscribeAsync();
        }
    }
}