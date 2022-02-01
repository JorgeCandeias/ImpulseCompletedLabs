using Microsoft.Extensions.Configuration;
using Orleans;
using Orleans.Hosting;
using Orleans.Providers;
using Orleans.TestingHost;
using System;

namespace Impulse.Chat.Tests
{
    public class ClusterFixture : IDisposable
    {
        public ClusterFixture()
        {
            Cluster = new TestClusterBuilder()
                .AddSiloBuilderConfigurator<TestSiloConfigurator>()
                .AddClientBuilderConfigurator<TestClientConfigurator>()
                .Build();

            Cluster.Deploy();
        }

        public void Dispose()
        {
            Cluster.StopAllSilos();
            GC.SuppressFinalize(this);
        }

        public TestCluster Cluster { get; }
    }

    public class TestSiloConfigurator : ISiloConfigurator
    {
        public void Configure(ISiloBuilder siloBuilder)
        {
            siloBuilder
                .AddMemoryGrainStorageAsDefault()
                .AddMemoryGrainStorage("PubSubStore")
                .AddMemoryStreams<DefaultMemoryMessageBodySerializer>("Chat")
                .UseInMemoryReminderService();
        }
    }

    public class TestClientConfigurator : IClientBuilderConfigurator
    {
        public void Configure(IConfiguration configuration, IClientBuilder clientBuilder)
        {
            clientBuilder
                .AddMemoryStreams<DefaultMemoryMessageBodySerializer>("Chat");
        }
    }
}