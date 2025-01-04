using ScreenTime.Common;

namespace ScreenTimeClient
{
    public class MessageEventArgs(UserMessage message)
    {
        public UserMessage Message { get; init; } = message;
    }
}