using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using ScreenTime.Common;

namespace ScreenTimeClient.Configuration
{
    public class RemoteUserConfigurationProvider(RemoteUserStateProvider connectionProvider, ILogger<RemoteUserConfigurationProvider> logger) : IUserConfigurationProvider, IDisposable
    {
        private readonly ILogger logger = logger;
        private readonly RemoteUserStateProvider connectionProvider = connectionProvider;


        private UserConfiguration? userConfigurationCache = null;
        public event EventHandler<UserConfigurationEventArgs>? OnConfigurationChanged;
        public event EventHandler<UserConfigurationResponseEventArgs>? OnExtensionResponse;

        public Task<UserConfiguration> GetConfigurationAsync()
        {
            return connectionProvider.GetConfigurationAsync();
        }

        public async Task<UserConfiguration> GetUserConfigurationForDayAsync()
        {
            // fire off request to get configuration asynchronously, but return immediately with the cached value

            return await GetConfigurationAsync();
        }

        public Task SaveUserConfigurationForDayAsync(UserConfiguration configuration)
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
            SaveUserConfigurationForDayAsync(newConfiguration).Wait();
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
            SaveUserConfigurationForDayAsync(newConfiguration).Wait();
            OnExtensionResponse?.Invoke(this, new(this, minutes));
        }

        public Task RequestExtensionAsync(int v)
        {
            throw new NotImplementedException();
        }

        public async Task SendHeartbeatAsync(Heartbeat heartbeat)
        {
            await connectionProvider.SendHeartbeatAsync(heartbeat);
        }

        public void Dispose()
        {
            ((IDisposable)connectionProvider).Dispose();

        }
    }
}
