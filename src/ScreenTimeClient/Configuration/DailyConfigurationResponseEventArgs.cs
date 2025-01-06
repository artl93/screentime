using ScreenTime.Common;

namespace ScreenTimeClient.Configuration
{
    public class DailyConfigurationResponseEventArgs(object Sender, int Minutes) : EventArgs
    {
        public int Minutes { get; } = Minutes;
        public object Sender { get; } = Sender;
    }
}
