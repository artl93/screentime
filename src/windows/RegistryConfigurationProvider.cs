

namespace ScreenTime
{
    public class RegistryConfigurationProvider : IUserConfigurationProvider, IDisposable
    {
        public event EventHandler<UserConfiguration>? OnConfigurationChanged;
        UserConfiguration? userConfigurationCache = null;
        private readonly ITimer? timer;
        private bool disposedValue;
        private readonly IUserConfigurationReader reader;

        public RegistryConfigurationProvider(IUserConfigurationReader reader, TimeProvider? timeProvider = null)
        {
            this.reader = reader;
            timer = timeProvider?.CreateTimer(OnCheckForUpdates, null, TimeSpan.FromSeconds(20), TimeSpan.FromMinutes(20));
            userConfigurationCache = reader.GetConfiguration();
        }

        private void OnCheckForUpdates(object? state)
        {
            if (!disposedValue) {
                var configuration = reader.GetConfiguration();
                if (userConfigurationCache != configuration)
                {
                    OnConfigurationChanged?.Invoke(this, configuration);
                    userConfigurationCache = configuration;
                }
            }
        }

        public Task<UserConfiguration> GetUserConfigurationForDayAsync()
        {
            return Task.Run(() => reader.GetConfiguration());
        }

        public Task SaveUserConfigurationForDayAsync(UserConfiguration configuration)
        {
            return Task.Run(() =>
            {
                if (configuration != userConfigurationCache)
                {
                    reader.SetConfiguration(configuration);
                    OnConfigurationChanged?.Invoke(this, configuration);
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
        // ~RegistryConfigurationProvider()
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