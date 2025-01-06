using ScreenTime.Common;

namespace ScreenTimeClient.Configuration
{
    public interface IDailyConfigurationProvider
    {
        public event EventHandler<DailyConfigurationEventArgs>? OnConfigurationChanged;
        public event EventHandler<DailyConfigurationResponseEventArgs>? OnExtensionResponse;

        public Task<DailyConfiguration> GetUserConfigurationForDayAsync();
        // TODO: Eliminate this method - this will be read-only from the server
        public Task SaveUserDailyConfigurationAsync(DailyConfiguration configuration);
        public void ResetExtensions();
        public void AddExtension(DateTimeOffset date, int minutes);
        public Task RequestExtensionAsync(int v);
        public Task SendHeartbeatAsync(Heartbeat heartbeat);
    }
}
