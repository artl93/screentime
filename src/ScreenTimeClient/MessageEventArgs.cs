namespace ScreenTime
{
    public class MessageEventArgs(UserMessage message)
    {
        public UserMessage Message { get; init; } = message;
    }
}