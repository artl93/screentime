
using Microsoft.Extensions.Hosting;
using ScreenTimeClient.Configuration;

namespace ScreenTimeClient
{
    public interface IScreenTimeStateClient : IHostedService
    {
        event EventHandler<MessageEventArgs>? OnDayRollover;
        event EventHandler<UserStatusEventArgs>? OnTimeUpdate;
        event EventHandler<UserStatusEventArgs>? OnUserStatusChanged;
        event EventHandler<MessageEventArgs>? OnMessageUpdate;
        event EventHandler<ComputerStateEventArgs>? EventHandlerEnsureComputerState;

        public void StartSession(string reason);
        public void EndSession(string reason);
        public Task<UserStatus?> GetInteractiveTimeAsync();
        public Task<UserMessage?> GetMessage();
        public Task<UserConfiguration?> GetUserConfigurationAsync();
        public Task RequestExtensionAsync(int minutes);
        public Task ResetAsync();
        public Task SaveCurrentConfigurationAsync();
        public UserActivityState GetActivityState();
    }
}