
namespace ScreenTime
{
    public interface IScreenTimeStateClient
    {
        public void EndSessionAsync();
        public Task<UserStatus?> GetInteractiveTimeAsync();
        public Task<UserMessage?> GetMessage();
        public Task<UserConfiguration?> GetUserConfigurationAsync();
        public void StartSessionAsync();
    }
}