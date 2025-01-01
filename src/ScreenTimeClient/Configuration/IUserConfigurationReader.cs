using ScreenTime.Common;

namespace ScreenTimeClient.Configuration
{
    public interface IUserConfigurationReader
    {
        UserConfiguration GetConfiguration();
        void SetConfiguration(UserConfiguration configuration);
    }
}