using ScreenTime.Common;


namespace ScreenTimeClient.Configuration
{
    public class SwitchableUserConfigurationProvider : IUserConfigurationProvider, IDisposable
    {
        private readonly LocalUserConfigurationProvider localProvider;
        private readonly RemoteUserConfigurationProvider remoteProvider;
        public event EventHandler<UserConfigurationEventArgs>? OnConfigurationChanged;
        public event EventHandler<UserConfigurationResponseEventArgs>? OnExtensionResponse;
        public SwitchableUserConfigurationProvider(RemoteUserConfigurationProvider remoteProvider, LocalUserConfigurationProvider localProvider)
        {
            this.remoteProvider = remoteProvider;
            this.localProvider = localProvider;
            remoteProvider.OnConfigurationChanged += (sender, args) => OnConfigurationChanged?.Invoke(sender, args);
            remoteProvider.OnExtensionResponse += (sender, args) => OnExtensionResponse?.Invoke(sender, args);
        }
        public async Task<UserConfiguration> GetUserConfigurationForDayAsync()
        {
            // always enable remote remoteProvider
            var result = await localProvider.GetUserConfigurationForDayAsync();
            var newResult = result with { EnableOnline = true };
            return newResult;
        }
        public Task SaveUserConfigurationForDayAsync(UserConfiguration configuration)
        {
            return localProvider.SaveUserConfigurationForDayAsync(configuration);
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