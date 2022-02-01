using Orleans.Concurrency;

namespace Impulse.Models
{
    [Immutable]
    public record ChatMessage
    {
        public DateTimeOffset Created { get; init; } = DateTimeOffset.Now;
        public string User { get; init; } = string.Empty;
        public string Text { get; init; } = string.Empty;
    }
}