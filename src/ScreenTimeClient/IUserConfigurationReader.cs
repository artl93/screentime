namespace ScreenTimeClient
{
    public interface IUserConfigurationReader
    {
        UserConfiguration GetConfiguration();
        void SetConfiguration(UserConfiguration configuration);
    }
}