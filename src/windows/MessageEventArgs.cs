namespace ScreenTime
{
    public class MessageEventArgs
    {
        public UserMessage Message { get; init; }

        public MessageEventArgs(UserMessage message)
        {
            this.Message = message;
        }
    }
}