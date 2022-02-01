using Impulse.Models;

namespace Impulse.Chat
{
    internal class ChannelGrainState
    {
        public Queue<ChatMessage> Messages { get; } = new();
        public HashSet<string> Members { get; } = new(StringComparer.OrdinalIgnoreCase);
    }
}