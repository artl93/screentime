using System;

namespace ScreenTime
{
    public class UserConfigurationEventArgs(object Sender, UserConfiguration Configuration) : EventArgs
    {
        public UserConfiguration Configuration { get; } = Configuration;
        public object Sender { get; } = Sender;
    }
}
