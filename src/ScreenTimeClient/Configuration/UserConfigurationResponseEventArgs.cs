using System;

namespace ScreenTimeClient.Configuration
{
    public class UserConfigurationResponseEventArgs(object Sender, int Minutes) : EventArgs
    {
        public int Minutes { get; } = Minutes;
        public object Sender { get; } = Sender;
    }
}
