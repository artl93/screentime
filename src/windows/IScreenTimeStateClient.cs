﻿
using Microsoft.Extensions.Hosting;

namespace ScreenTime
{
    public interface IScreenTimeStateClient : IHostedService
    {
        event EventHandler<MessageEventArgs>? OnDayRollover;
        event EventHandler<UserStatusEventArgs>? OnTimeUpdate;
        event EventHandler<UserStatusEventArgs>? OnUserStatusChanged;
        event EventHandler<MessageEventArgs>? OnMessageUpdate;
        event EventHandler<ComputerStateEventArgs>? EventHandlerEnsureComputerState;

        public Task StartSessionAsync(string reason);
        public Task EndSessionAsync(string reason);
        public Task<UserStatus?> GetInteractiveTimeAsync();
        public Task<UserMessage?> GetMessage();
        public Task<UserConfiguration?> GetUserConfigurationAsync();
        public Task RequestExtensionAsync(int minutes);
        public Task ResetAsync();
    }
}