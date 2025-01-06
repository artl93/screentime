using ScreenTime.Common;

namespace ScreenTimeClient.Configuration
{
    public interface IDailyConfigurationReader
    {
        DailyConfiguration GetConfiguration();
        void SetConfiguration(DailyConfiguration configuration);
    }
}