using ScreenTime.Common;

namespace ScreenTimeClient.Configuration
{

    public class DailyConfigurationLocalProvider : IDailyConfigurationProvider, IDisposable
    {
        public event EventHandler<DailyConfigurationEventArgs>? OnConfigurationChanged;
        public event EventHandler<DailyConfigurationResponseEventArgs>? OnExtensionResponse;
        DailyConfiguration? userConfigurationCache = null;
        private readonly ITimer? timer;
        private bool disposedValue;
        private readonly IDailyConfigurationReader reader;

        public DailyConfigurationLocalProvider(IDailyConfigurationReader reader, TimeProvider? timeProvider = null)
        {
            this.reader = reader;
            userConfigurationCache = reader.GetConfiguration();
            timer = timeProvider?.CreateTimer(OnCheckForUpdates, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
        }

        private void OnCheckForUpdates(object? state)
        {
            if (!disposedValue)
            {
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

        public Task<DailyConfiguration> GetUserConfigurationForDayAsync()
        {
            var configuration = reader.GetConfiguration();
            return Task.FromResult(configuration);
        }

        public async Task SaveUserDailyConfigurationAsync(DailyConfiguration configuration)
        {
            await Task.Run(() =>
            {
                if (configuration != userConfigurationCache)
                {
                    reader.SetConfiguration(configuration);
                    OnConfigurationChanged?.Invoke(this, new(this, configuration));
                    userConfigurationCache = configuration;
                }
            }).ConfigureAwait(false);
        }


        public void ResetExtensions()
        {
            if (userConfigurationCache is null)
            {
                return;
            }
            var newConfiguration = userConfigurationCache with { Extensions = [] };
            SaveUserDailyConfigurationAsync(newConfiguration).Wait();
        }

        public void AddExtension(DateTimeOffset date, int minutes)
        {
            if (userConfigurationCache is null)
            {
                return;
            }
            var newExtensions = userConfigurationCache.Extensions?.ToList() ?? [];
            newExtensions.Add((date, minutes));
            var newConfiguration = userConfigurationCache with { Extensions = [.. newExtensions] };
            SaveUserDailyConfigurationAsync(newConfiguration).Wait();
            OnExtensionResponse?.Invoke(this, new(this, minutes));
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    timer?.Dispose();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public Task RequestExtensionAsync(int v)
        {
            throw new NotImplementedException();
        }

        public Task SendHeartbeatAsync(Heartbeat _)
        {
            // goes nowhere, does nothing
            return Task.CompletedTask;
        }
    }
}