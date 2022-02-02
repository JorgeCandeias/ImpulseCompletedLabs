using Orleans.Concurrency;

namespace Impulse.Models
{
    [Immutable]
    public record ChatStats
    {
        public string Name { get; init; } = "";
        public int Members { get; init; }
        public int Messages { get; init; }
    }
}