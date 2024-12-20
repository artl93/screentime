
using Microsoft.Extensions.Hosting;

namespace ScreenTime
{
    public interface IScreenTimeStateClient : IHostedService
    {
        event EventHandler<MessageEventArgs>? OnDayRollover;
        event EventHandler<UserStatusEventArgs>? OnTimeUpdate;
        event EventHandler<UserStatusEventArgs>? OnUserStatusChanged;
        event EventHandler<MessageEventArgs>? OnMessageUpdate;

        public void EndSessionAsync();
        public Task<UserStatus?> GetInteractiveTimeAsync();
        public Task<UserMessage?> GetMessage();
        public Task<UserConfiguration?> GetUserConfigurationAsync();
        void Reset();
        public void StartSessionAsync();
    }
}