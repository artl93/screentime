

namespace ScreenTime
{
    public class UserConfigurationRegistryProvider : IUserConfigurationProvider, IDisposable
    {
        public event EventHandler<UserConfigurationEventArgs>? OnConfigurationChanged;
        UserConfiguration? userConfigurationCache = null;
        private readonly ITimer? timer;
        private bool disposedValue;
        private readonly IUserConfigurationReader reader;

        public UserConfigurationRegistryProvider(IUserConfigurationReader reader, TimeProvider? timeProvider = null)
        {
            this.reader = reader;
            userConfigurationCache = reader.GetConfiguration();
            timer = timeProvider?.CreateTimer(OnCheckForUpdates, null, TimeSpan.FromSeconds(20), TimeSpan.FromMinutes(20));
        }

        private void OnCheckForUpdates(object? state)
        {
            if (!disposedValue) {
                var configuration = reader.GetConfiguration();
                if (userConfigurationCache != configuration)
                {
                    lock (this)
                    {
                        if (userConfigurationCache != configuration)
                        {

                            OnConfigurationChanged?.Invoke(this, new(this, configuration));
                            userConfigurationCache = configuration;
                        }
                    }
                }
            }
        }

        public Task<UserConfiguration> GetUserConfigurationForDayAsync()
        {
            var configuration = reader.GetConfiguration();
            return Task.FromResult(configuration);
        }

        public async Task SaveUserConfigurationForDayAsync(UserConfiguration configuration)
        {
            await Task.Run(() =>
            {
                if (configuration != userConfigurationCache)
                {
                    reader.SetConfiguration(configuration);
                    OnConfigurationChanged?.Invoke(this, new(this, configuration));
                    userConfigurationCache = configuration;
                }
            });
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    timer?.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~UserConfigurationRegistryProvider()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}