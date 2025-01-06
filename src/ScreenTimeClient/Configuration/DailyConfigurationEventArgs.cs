using ScreenTime.Common;


namespace ScreenTimeClient.Configuration
{
    public class DailyConfigurationEventArgs(object Sender, DailyConfiguration Configuration) : EventArgs
    {
        public DailyConfiguration Configuration { get; } = Configuration;
        public object Sender { get; } = Sender;
    }
}
