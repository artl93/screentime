using ScreenTimeClient.Configuration;
using ScreenTime.Common;

namespace ScreenTimeTest
{
    internal class MockUserConfigurationReader(UserConfiguration startingConfiguration) : IUserConfigurationReader
    {
        public UserConfiguration GetConfiguration()
        {
            return startingConfiguration;
        }

        public void SetConfiguration(UserConfiguration configuration)
        {
            startingConfiguration = configuration;
        }
    }
}