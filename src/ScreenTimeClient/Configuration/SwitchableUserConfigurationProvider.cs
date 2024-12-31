namespace ScreenTimeClient.Configuration
{
    public class SwitchableUserConfigurationProvider : IUserConfigurationProvider, IDisposable
    {
        private readonly LocalUserConfigurationProvider backupProvider;
        private readonly RemoteUserConfigurationProvider provider;
        public event EventHandler<UserConfigurationEventArgs>? OnConfigurationChanged;
        public event EventHandler<UserConfigurationResponseEventArgs>? OnExtensionResponse;
        public SwitchableUserConfigurationProvider(RemoteUserConfigurationProvider provider, LocalUserConfigurationProvider backupProvider)
        {
            this.provider = provider;
            this.backupProvider = backupProvider;
            provider.OnConfigurationChanged += (sender, args) => OnConfigurationChanged?.Invoke(sender, args);
            provider.OnExtensionResponse += (sender, args) => OnExtensionResponse?.Invoke(sender, args);
        }
        public async Task<UserConfiguration> GetUserConfigurationForDayAsync()
        {
            // always enable remote provider
            var result = await backupProvider.GetUserConfigurationForDayAsync();
            var newResult = result with { EnableOnline = true };
            return newResult;
        }
        public Task SaveUserConfigurationForDayAsync(UserConfiguration configuration)
        {
            return backupProvider.SaveUserConfigurationForDayAsync(configuration);
        }
        public void ResetExtensions()
        {
            backupProvider.ResetExtensions();
        }
        public void AddExtension(DateTimeOffset date, int minutes)
        {
            backupProvider.AddExtension(date, minutes);
        }

        public void SwitchToBackup()
        {
            if (backupProvider is null)
            {
                return;
            }
            provider.OnConfigurationChanged -= (sender, args) => OnConfigurationChanged?.Invoke(sender, args);
            provider.OnExtensionResponse -= (sender, args) => OnExtensionResponse?.Invoke(sender, args);
            backupProvider.OnConfigurationChanged += (sender, args) => OnConfigurationChanged?.Invoke(sender, args);
            backupProvider.OnExtensionResponse += (sender, args) => OnExtensionResponse?.Invoke(sender, args);
        }
        public void SwitchToPrimary()
        {
            if (backupProvider is null)
            {
                return;
            }
            backupProvider.OnConfigurationChanged -= (sender, args) => OnConfigurationChanged?.Invoke(sender, args);
            backupProvider.OnExtensionResponse -= (sender, args) => OnExtensionResponse?.Invoke(sender, args);
            provider.OnConfigurationChanged += (sender, args) => OnConfigurationChanged?.Invoke(sender, args);
            provider.OnExtensionResponse += (sender, args) => OnExtensionResponse?.Invoke(sender, args);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                provider.Dispose();
                backupProvider?.Dispose();
            }
        }
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public Task RequestExtensionAsync(int v)
        {
            return backupProvider.RequestExtensionAsync(v);
        }
    }
}