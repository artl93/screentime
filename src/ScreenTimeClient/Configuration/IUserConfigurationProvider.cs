﻿namespace ScreenTimeClient.Configuration
{
    public interface IUserConfigurationProvider
    {
        public Task<UserConfiguration> GetUserConfigurationForDayAsync();
        public Task SaveUserConfigurationForDayAsync(UserConfiguration configuration);
        public event EventHandler<UserConfigurationEventArgs>? OnConfigurationChanged;
        public event EventHandler<UserConfigurationResponseEventArgs>? OnExtensionResponse;
        public void ResetExtensions();
        public void AddExtension(DateTimeOffset date, int minutes);
    }
}
