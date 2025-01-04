using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using ScreenTime.Common;

namespace ScreenTimeClient.Configuration
{
    public class DailyConfigurationRemoteProvider(ScreenTimeServiceClient connectionProvider, ILogger<DailyConfigurationRemoteProvider> logger) : IDailyConfigurationProvider, IDisposable
    {
        private readonly ILogger logger = logger;
        private readonly ScreenTimeServiceClient serviceClient = connectionProvider;


        private DailyConfiguration? userConfigurationCache = null;
        public event EventHandler<DailyConfigurationEventArgs>? OnConfigurationChanged;
        public event EventHandler<DailyConfigurationResponseEventArgs>? OnExtensionResponse;

        public Task<DailyConfiguration> GetConfigurationAsync()
        {
            return serviceClient.GetConfigurationAsync();
        }

        public async Task<DailyConfiguration> GetUserConfigurationForDayAsync()
        {
            // fire off request to get configuration asynchronously, but return immediately with the cached value

            return await GetConfigurationAsync();
        }

        public Task SaveUserDailyConfigurationAsync(DailyConfiguration configuration)
        {
            throw new NotImplementedException();
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

        public Task RequestExtensionAsync(int time)
        {
            var extension = new ExtensionRequest(TimeSpan.FromMinutes(time));
            return connectionProvider.RequestExtension(extension);
        }

        public async Task SendHeartbeatAsync(Heartbeat heartbeat)
        {
            await serviceClient.SendHeartbeatAsync(heartbeat);
        }

        public void Dispose()
        {
            ((IDisposable)serviceClient).Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
