﻿using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace ScreenTimeClient.Configuration
{
    public class RemoteUserConfigurationProvider(HttpClient httpClient, ILogger logger) : IUserConfigurationProvider, IDisposable
    {
        private readonly HttpClient httpClient = httpClient;
        const string configUrl = "configration/";
        const string extensionUrl = "extensions/request/{0}";
        const string profleUrl = "profile/";

        ILogger logger = logger;

        private readonly JsonSerializerOptions options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        private UserConfiguration? userConfigurationCache = null;
        private bool disposedValue;
        public event EventHandler<UserConfigurationEventArgs>? OnConfigurationChanged;
        public event EventHandler<UserConfigurationResponseEventArgs>? OnExtensionResponse;

        public async Task<UserConfiguration?> GetConfigurationAsync()
        {
            var response = await httpClient.GetAsync(configUrl);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<UserConfiguration>(content, options);
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
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    httpClient.Dispose();
                }
                disposedValue = true;
            }
        }
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public Task RequestExtensionAsync(int v)
        {
            throw new NotImplementedException();
        }
    }
}