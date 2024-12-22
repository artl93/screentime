using ScreenTime;

namespace ScreenTimeTest
{
    internal class MockUserConfigurationReader : IUserConfigurationReader
    {
        UserConfiguration userConfiguration;
        public MockUserConfigurationReader(UserConfiguration startingConfiguration) 
        {
            userConfiguration = startingConfiguration;
        }

        public UserConfiguration GetConfiguration()
        {
            return userConfiguration;
        }
        public void SetConfiguration(UserConfiguration configuration)
        {
            userConfiguration = configuration;
        }
    }
}