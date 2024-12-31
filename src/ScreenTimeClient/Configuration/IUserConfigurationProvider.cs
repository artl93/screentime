namespace ScreenTimeClient.Configuration
{
    public interface IUserConfigurationProvider
    {
        public event EventHandler<UserConfigurationEventArgs>? OnConfigurationChanged;
        public event EventHandler<UserConfigurationResponseEventArgs>? OnExtensionResponse;

        public Task<UserConfiguration> GetUserConfigurationForDayAsync();
        public Task SaveUserConfigurationForDayAsync(UserConfiguration configuration);
        public void ResetExtensions();
        public void AddExtension(DateTimeOffset date, int minutes);
        public Task RequestExtensionAsync(int v);
    }
}
