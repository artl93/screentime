namespace ScreenTime
{
    public class ExtensionResponseArgs(object Sender, string Message) : EventArgs
    {
        public object Sender { get; } = Sender;
        public string Message { get; } = Message;

    }
}