
namespace screentime
{
    internal interface IScreenTimeStateClient
    {
        void EndSessionAsync();
        Task<UserStatus?> GetInteractiveTimeAsync();
        Task<ServerMessage?> GetMessage();
        Task<UserConfiguration?> GetUserConfigurationAsync();
        void StartSessionAsync();
    }
}