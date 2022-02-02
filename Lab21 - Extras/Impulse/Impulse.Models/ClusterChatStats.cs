using Orleans.Concurrency;

namespace Impulse.Models
{
    [Immutable]
    public record ClusterChatStats(int Channels, int Members, int Messages);
}