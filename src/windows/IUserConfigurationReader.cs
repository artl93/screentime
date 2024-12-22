namespace ScreenTime
{
    public interface IUserConfigurationReader
    {
        UserConfiguration GetConfiguration();
        void SetConfiguration(UserConfiguration configuration);
    }
}