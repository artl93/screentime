
namespace ScreenTime
{
    public interface IUserConfigurationProvider
    {
        public Task<UserConfiguration> GetUserConfigurationForDayAsync();
        public Task SaveUserConfigurationForDayAsync(UserConfiguration configuration);
        public event EventHandler<UserConfiguration>? OnConfigurationChanged;
    }
}
