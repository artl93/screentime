using ScreenTimeClient.Configuration;
using ScreenTime.Common;

namespace ScreenTimeTest
{
    internal class MockUserConfigurationReader(DailyConfiguration startingConfiguration) : IDailyConfigurationReader
    {
        public DailyConfiguration GetConfiguration()
        {
            return startingConfiguration;
        }

        public void SetConfiguration(DailyConfiguration configuration)
        {
            startingConfiguration = configuration;
        }
    }
}