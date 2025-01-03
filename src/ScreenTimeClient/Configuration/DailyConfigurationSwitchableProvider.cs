using ScreenTime.Common;


namespace ScreenTimeClient.Configuration
{
    public class DailyConfigurationSwitchableProvider : IDailyConfigurationProvider, IDisposable
    {
        private readonly DailyConfigurationLocalProvider localProvider;
        private readonly DailyConfigurationRemoteProvider remoteProvider;
        public event EventHandler<DailyConfigurationEventArgs>? OnConfigurationChanged;
        public event EventHandler<DailyConfigurationResponseEventArgs>? OnExtensionResponse;
        public DailyConfigurationSwitchableProvider(DailyConfigurationRemoteProvider remoteProvider, DailyConfigurationLocalProvider localProvider)
        {
            this.remoteProvider = remoteProvider;
            this.localProvider = localProvider;
            remoteProvider.OnConfigurationChanged += (sender, args) => OnConfigurationChanged?.Invoke(sender, args);
            remoteProvider.OnExtensionResponse += (sender, args) => OnExtensionResponse?.Invoke(sender, args);
        }
        public async Task<DailyConfiguration> GetUserConfigurationForDayAsync()
        {
            // always enable remote remoteProvider
            var result = await localProvider.GetUserConfigurationForDayAsync();
            return result;
        }
        public Task SaveUserDailyConfigurationAsync(DailyConfiguration configuration)
        {
            return localProvider.SaveUserDailyConfigurationAsync(configuration);
        }
        public void ResetExtensions()
        {
            localProvider.ResetExtensions();
        }
        public void AddExtension(DateTimeOffset date, int minutes)
        {
            localProvider.AddExtension(date, minutes);
        }

        public void SwitchToBackup()
        {
            if (localProvider is null)
            {
                return;
            }
            remoteProvider.OnConfigurationChanged -= (sender, args) => OnConfigurationChanged?.Invoke(sender, args);
            remoteProvider.OnExtensionResponse -= (sender, args) => OnExtensionResponse?.Invoke(sender, args);
            localProvider.OnConfigurationChanged += (sender, args) => OnConfigurationChanged?.Invoke(sender, args);
            localProvider.OnExtensionResponse += (sender, args) => OnExtensionResponse?.Invoke(sender, args);
        }
        public void SwitchToPrimary()
        {
            if (localProvider is null)
            {
                return;
            }
            localProvider.OnConfigurationChanged -= (sender, args) => OnConfigurationChanged?.Invoke(sender, args);
            localProvider.OnExtensionResponse -= (sender, args) => OnExtensionResponse?.Invoke(sender, args);
            remoteProvider.OnConfigurationChanged += (sender, args) => OnConfigurationChanged?.Invoke(sender, args);
            remoteProvider.OnExtensionResponse += (sender, args) => OnExtensionResponse?.Invoke(sender, args);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                remoteProvider.Dispose();
                localProvider?.Dispose();
            }
        }
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public Task RequestExtensionAsync(int v)
        {
            return localProvider.RequestExtensionAsync(v);
        }

        public async Task SendHeartbeatAsync(Heartbeat heartbeat)
        {
            await localProvider.SendHeartbeatAsync(heartbeat); //.ConfigureAwait(false);
            await remoteProvider.SendHeartbeatAsync(heartbeat); // .ConfigureAwait(false);

        }
    }
}