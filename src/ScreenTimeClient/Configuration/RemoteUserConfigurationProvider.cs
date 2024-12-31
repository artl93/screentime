using System.Text;
using System.Text.Json;

namespace ScreenTimeClient.Configuration
{
    public class RemoteUserConfigurationProvider(HttpClient httpClient) : IUserConfigurationProvider, IDisposable
    {
        private readonly HttpClient httpClient = httpClient;
        private const string url = "TODO: Replace Me";
        private readonly JsonSerializerOptions options = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private UserConfiguration? userConfigurationCache = null;
        private bool disposedValue;
        public event EventHandler<UserConfigurationEventArgs>? OnConfigurationChanged;
        public event EventHandler<UserConfigurationResponseEventArgs>? OnExtensionResponse;

        public UserConfiguration GetConfiguration()
        {
            var response = httpClient.GetAsync(url).Result;
            response.EnsureSuccessStatusCode();
            var content = response.Content.ReadAsStringAsync().Result;
            return JsonSerializer.Deserialize<UserConfiguration>(content, options);
        }
        public void SetConfiguration(UserConfiguration configuration)
        {
            var content = new StringContent(JsonSerializer.Serialize(configuration, options), Encoding.UTF8, "application/json");
            var response = httpClient.PostAsync(url, content).Result;
            response.EnsureSuccessStatusCode();
        }
        public Task<UserConfiguration> GetUserConfigurationForDayAsync()
        {
            return Task.FromResult(GetConfiguration());
        }
        public Task SaveUserConfigurationForDayAsync(UserConfiguration configuration)
        {
            return Task.Run(() => SetConfiguration(configuration));
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
    }
}