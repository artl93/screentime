using Humanizer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ScreenTimeClient;
using System.Runtime.CompilerServices;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
[assembly: InternalsVisibleTo("ScreenTimeTest")]

namespace ScreenTimeClient
{

    public partial class ScreenTimeLocalService(TimeProvider timeProvider,
                                                IUserConfigurationProvider userConfigurationProvider,
                                                UserStateRegistryProvider stateProvider,
                                                IIdleTimeDetector idleDetector, ILogger? logger) 
        : IScreenTimeStateClient, IDisposable, IHostedService
    {
        private readonly IUserConfigurationProvider userConfigurationProvider = userConfigurationProvider;
        private DateTimeOffset lastKnownTime;
        private DateTimeOffset nextResetDate;
        private TimeSpan duration;
        private readonly TimeProvider _timeProvider = timeProvider;
        public UserConfiguration? configuration;
        private readonly UserStateRegistryProvider _stateProvider = stateProvider;
        private ITimer? callbackTimer;
        private ITimer? heartbeatTimer;
        private bool disposedValue = false;
        private UserActivityState activityState = UserActivityState.Unknown;
        private UserState lastUserState;
        private DateTimeOffset lastMessageShown;
        bool started = false;
        private readonly IIdleTimeDetector idleDetector = idleDetector;
        private bool isIdle;
        private readonly ILogger? logger = logger;

        public event EventHandler<MessageEventArgs>? OnDayRollover;
        public event EventHandler<UserStatusEventArgs>? OnTimeUpdate;
        public event EventHandler<UserStatusEventArgs>? OnUserStatusChanged;
        public event EventHandler<MessageEventArgs>? OnMessageUpdate;
        public event EventHandler<ComputerStateEventArgs>? EventHandlerEnsureComputerState;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            logger?.LogInformation("Starting ScreenTimeLocalService");
            configuration = await userConfigurationProvider.GetUserConfigurationForDayAsync();
            userConfigurationProvider.OnConfigurationChanged += (s, e) => OnConfigurationChanged(e.Configuration);
            userConfigurationProvider.OnExtensionResponse += (s, e) => ExtensionGrantedNotification(e.Minutes);
            nextResetDate = GetNextResetTime(TimeSpan.Parse($"{configuration.ResetTime}"));

            _stateProvider.LoadState(out lastKnownTime, out duration, out lastUserState, out lastMessageShown, out activityState);
            // data corruption issue
            if (lastKnownTime == DateTimeOffset.MinValue || lastKnownTime.UtcDateTime - duration >= _timeProvider.GetUtcNow())
            {
                lastKnownTime = _timeProvider.GetUtcNow();
                duration = TimeSpan.Zero;
                userConfigurationProvider.ResetExtensions();
            }
            // if it has been more than 24 hours since the last reset, reset the duration
            else if (lastKnownTime.UtcDateTime <= nextResetDate.AddDays(-1) || duration >= TimeSpan.FromDays(1))
            {
                duration = TimeSpan.Zero;
                lastKnownTime = _timeProvider.GetUtcNow();
                userConfigurationProvider.ResetExtensions();
            }
            // throw out all time between now and the last known state - we have no idea what was happening when the app wasn't running 
            // because the OS isn't giving shutdown signals as we'd expect 
            lastKnownTime = _timeProvider.GetUtcNow();
            activityState = UserActivityState.Active;


            started = true;
            // this should have been called in CreateTimer
            callbackTimer = _timeProvider.CreateTimer(UpdateInteractiveTime, this, TimeSpan.Zero, TimeSpan.FromSeconds(1));
            heartbeatTimer = _timeProvider.CreateTimer(LogHeartbeat, this, TimeSpan.FromMinutes(.1), TimeSpan.FromMinutes(1)); 

        }

        private void OnConfigurationChanged(UserConfiguration newConfiguration)
        {
            if ((configuration != null) && (configuration.ResetTime != newConfiguration.ResetTime))
                nextResetDate = GetNextResetTime(TimeSpan.Parse($"{newConfiguration.ResetTime}"));
            configuration = newConfiguration;
        }

        private void LogHeartbeat(object? state) => logger?.LogInformation("Heartbeat - Duration: {Duration}", duration);

        public Task StopAsync(CancellationToken cancellationToken)
        {
            logger?.LogInformation("Stopping ScreenTimeLocalService");
            logger?.LogInformation("Final duration: {Duration}", duration);
            return Task.CompletedTask;
        }

        private DateTimeOffset GetNextResetTime(TimeSpan resetTime)
        {
            var resetOffset = -(_timeProvider.LocalTimeZone.BaseUtcOffset) + resetTime;
            var newResetTime = _timeProvider.GetUtcNow().Date + resetOffset;
            var utcResetTime = new DateTimeOffset(newResetTime, TimeSpan.FromHours(0));

            if (utcResetTime <= _timeProvider.GetUtcNow())
            {
                logger?.LogInformation("Next reset time is {ResetTime}", utcResetTime.AddDays(1));
                return utcResetTime.AddDays(1);
            }
            else
            {
                logger?.LogInformation("Next reset time is {ResetTime}", utcResetTime);
                return utcResetTime;
            }
        }

        private void UpdateInteractiveTime(object? state)
        {
            if (disposedValue)
            {
                return;
            }
            DoUpdateTime();
        }

        
        private void DoUpdateTime()
        {
            if (configuration == null)
                return;
            if (!started)
                return; 
            UpdateIdleTime();
            var currentTime = _timeProvider.GetUtcNow();
            var timeSinceLast = currentTime - lastKnownTime;

            if (nextResetDate == DateTimeOffset.MinValue)
            {
                nextResetDate = GetNextResetTime(TimeSpan.Parse($"{configuration.ResetTime}"));
                userConfigurationProvider.ResetExtensions();
                duration = TimeSpan.Zero;
            }

            if (currentTime >= nextResetDate)
            {
                var delta = currentTime - nextResetDate;
                delta = activityState == UserActivityState.Active ? delta.Add(TimeSpan.FromDays(delta.Days * -1)) : TimeSpan.FromMinutes(0);
                nextResetDate = GetNextResetTime(TimeSpan.Parse($"{configuration.ResetTime}"));
                userConfigurationProvider.ResetExtensions();
                duration = delta;
                OnDayRollover?.Invoke(this, new MessageEventArgs(new UserMessage(
                    "Day rollover",
                    "It's a new day! Your time has been reset.",
                    "🎉",
                    "none"
                    ) ));
            }
            else if (activityState == UserActivityState.Active)
            {
                duration += timeSinceLast;
            }

            lastKnownTime = currentTime;
            OnTimeUpdate?.Invoke(this, new UserStatusEventArgs(
                GetUserStatus(), 
                currentTime, duration));
            lock (this)
            {
                var newState = GetUserState();
                var stateChanged = newState != lastUserState;
                if (newState != lastUserState)
                {
                    lastUserState = newState;
                    PostStatusChanges();
                }
                PostMessages(stateChanged);
                // save the state
                _stateProvider.SaveState(lastKnownTime, duration, lastUserState, lastMessageShown, activityState);
            }
            EventHandlerEnsureComputerState?.Invoke(this, new ComputerStateEventArgs(lastUserState));

        }

        private void PostStatusChanges()
        {
            if (configuration == null)
                return;
            var status = GetUserStatus();
            if (currentUserStatus != status)
            {
                currentUserStatus = status;
                OnUserStatusChanged?.Invoke(this, new UserStatusEventArgs(currentUserStatus, _timeProvider.GetUtcNow(), duration));
            }
        }


        private void PostMessages(bool stateChanged)
        {
            // state change or debounced
            if (stateChanged || 
                (_timeProvider.GetUtcNow() - lastMessageShown < TimeSpan.FromMinutes(1)))
            {
                return;
            }
            if ((lastUserState == UserState.Okay) && (_timeProvider.GetUtcNow() - lastMessageShown < TimeSpan.FromMinutes(15)))
                return;
            var message = GetUserMessage();
            OnMessageUpdate?.Invoke(this, new MessageEventArgs(message));
            lastMessageShown = _timeProvider.GetUtcNow();
        }

        public void EndSession(string reason)
        {
            logger?.LogInformation("End session called ({Reason}) - {Duration}", reason, duration);
            activityState = UserActivityState.Inactive;
            DoUpdateTime();
            PostStatusChanges();
            // todo - ensure time transitioned here
        }


        public void StartSession(string reason)
        {

            logger?.LogInformation("Start session called. ({Reason}) - {Duration}", reason, duration);
            activityState = UserActivityState.Active;
            // todo - ensure time transitioned here
            DoUpdateTime();
            PostStatusChanges();            
        }

        public Task<UserStatus?> GetInteractiveTimeAsync()
        {
            if (configuration == null)
                return Task.FromResult<UserStatus?>(null);

            return Task.FromResult<UserStatus?>(GetUserStatus());
        }

        public UserState GetUserState()
        {
            if (configuration == null)
                return UserState.Invalid;
            var dailyTimeLimit = configuration.TotalTimeAllowed;
            var warningTime = TimeSpan.FromMinutes(configuration.WarningTimeMinutes);
            var graceTime = TimeSpan.FromMinutes(configuration.GraceMinutes);
            return GetUserState(duration, dailyTimeLimit, warningTime, graceTime, isIdle);
        }

        private static UserState GetUserState(TimeSpan interactiveTime, TimeSpan dailyTimeLimit, TimeSpan warningTime, TimeSpan gracePeriod, bool isIdle)
        {
            if (interactiveTime >= dailyTimeLimit + gracePeriod)
            {
                return UserState.Lock;
            }
            else if (isIdle)
            {
                return UserState.Paused;
            }
            else if (interactiveTime >= dailyTimeLimit)
            {
                return UserState.Error;
            }
            else if (dailyTimeLimit - interactiveTime <= warningTime && dailyTimeLimit - interactiveTime > TimeSpan.Zero)
            {
                return UserState.Warn;
            }
            else
            {
                return UserState.Okay;
            }
        }

        private UserStatus GetUserStatus()
        {
            var state = GetUserState();
            var interactiveTime = duration;
            var dailyTimeLimit = configuration == null ? TimeSpan.FromMinutes(120) : configuration.TotalTimeAllowed;
            var extensions = configuration == null ? TimeSpan.Zero : configuration.TotalExtensions;
            return state switch
            {
                UserState.Lock => new UserStatus(interactiveTime, "🛡️", "logout", UserState.Lock, dailyTimeLimit, extensions),
                UserState.Error => new UserStatus(interactiveTime, "🛑", "logout", UserState.Error, dailyTimeLimit, extensions),
                UserState.Warn => new UserStatus(interactiveTime, "⚠️", "none", UserState.Warn, dailyTimeLimit, extensions),
                UserState.Okay => new UserStatus(interactiveTime, "⏳", "none", UserState.Okay, dailyTimeLimit, extensions),
                UserState.Paused => new UserStatus(interactiveTime, "💤", "none", UserState.Paused, dailyTimeLimit, extensions),
                _ => new UserStatus(interactiveTime, "❌", "none", UserState.Invalid, dailyTimeLimit, extensions)
            }; 
        }

        public Task<UserMessage?> GetMessage()
        {
            return Task.FromResult<UserMessage?>(GetUserMessage());
        }

        private UserMessage GetUserMessage()
        {
            if (configuration == null)
                return new UserMessage("Error", "No configuration found", "❌", "none");
            var interactiveTime = duration;
            var dailyTimeLimit = configuration.TotalTimeAllowed;
            var warningTime = TimeSpan.FromMinutes(configuration.WarningTimeMinutes);
            var graceTime = TimeSpan.FromMinutes(configuration.GraceMinutes);

            var interactiveTimeString = TimeSpanHumanizeExtensions.Humanize(interactiveTime, precision: 2, maxUnit: Humanizer.Localisation.TimeUnit.Hour, minUnit: Humanizer.Localisation.TimeUnit.Second);
            var allowedTimeString = TimeSpanHumanizeExtensions.Humanize(dailyTimeLimit, minUnit: Humanizer.Localisation.TimeUnit.Minute, maxUnit: Humanizer.Localisation.TimeUnit.Hour, precision: 2);

            // when time is up, log them off
            if (interactiveTime >= dailyTimeLimit)
            {
                if (interactiveTime > dailyTimeLimit + graceTime)
                {
                    // if they have gone over the limit, log them off
                    return new UserMessage("Hey, you're done.", $"You have been logged for {interactiveTimeString} today. You're allowed {allowedTimeString} today. You have gone over by {TimeSpanHumanizeExtensions.Humanize(interactiveTime - dailyTimeLimit, precision : 2)}", "🛡️", "lock");
                }
                return new UserMessage("Time to log out.", $"You have been logged for {interactiveTimeString} today. You're allowed {allowedTimeString} today.", "🛑", "logout");
            }

            // when they have 10 minutes left, warn them every one minute
            if (dailyTimeLimit - interactiveTime <= warningTime && dailyTimeLimit - interactiveTime > TimeSpan.Zero)
            {
                if ((dailyTimeLimit - interactiveTime).Minutes % 1 == 0)
                {
                    var remainingTimeString = TimeSpanHumanizeExtensions.Humanize(dailyTimeLimit - interactiveTime, precision: 2, minUnit: Humanizer.Localisation.TimeUnit.Second, maxUnit: Humanizer.Localisation.TimeUnit.Hour);
                    return new UserMessage("Time Warning", $"You have {remainingTimeString} left out of {allowedTimeString}", "⏳", "warn");
                }
            }

            return new UserMessage("Time Logged", $"You have been logged for {interactiveTimeString} today out of {allowedTimeString}", "🕒", "none");
        }

        public Task<UserConfiguration?> GetUserConfigurationAsync()
        {
            return Task.FromResult<UserConfiguration?>(configuration);
        }

        TimeSpan idleTimeLast = TimeSpan.Zero;
        private UserStatus? currentUserStatus;

        void UpdateIdleTime()
        {
            var idleTime = idleDetector.GetIdleTime();
            if (idleTime.TotalMinutes >= 5) // Notify if idle for 5 minutes
            {
                if (!isIdle)
                {
                    lock (this)
                    {
                        isIdle = true;
                        // this.OnUserStatusChanged?.Invoke(this, new UserStatusEventArgs(new UserStatus(duration, "💤", "none", UserState.Paused, TimeSpan.FromMinutes(0)), _timeProvider.GetUtcNow(), duration));
                        logger?.LogInformation("User is idle for {Minutes} minutes", idleTime.TotalMinutes);
                        activityState = UserActivityState.Inactive;
                    }
                }
            }
            else if (isIdle)
            {
                logger?.LogInformation("User is no longer idle {IdleTimeLast}", idleTimeLast);
                isIdle = false;
                activityState = UserActivityState.Active;
            }
            idleTimeLast = idleTime;

        }


        public async Task RequestExtensionAsync(int minutes)
        {
            await Task.Run(() =>
            {
                logger?.LogWarning("Requesting extension for {Minutes} minutes.", minutes);
                userConfigurationProvider.AddExtension(_timeProvider.GetUtcNow(), minutes);
            });
        }

        public void ExtensionGrantedNotification(int minutes)
        {
            logger?.LogWarning("Extension granted for {Minutes} minutes.", minutes);
            PostStatusChanges();
            OnMessageUpdate?.Invoke(this, new MessageEventArgs(new UserMessage(
                "Extension granted",
                $"Extension granted for {minutes} minutes. But seriously, touch grass.",
                "🌱",
                "none"
                )));
        }

        public UserActivityState GetActivityState()
        {
            return activityState;
        }

        public async Task ResetAsync()
        {
            await Task.Run(() =>
            {
                logger?.LogCritical("User time was attempted!");
                lastKnownTime = _timeProvider.GetUtcNow();
                duration = TimeSpan.Zero;
                if (configuration == null)
                    return;
                nextResetDate = GetNextResetTime(TimeSpan.Parse($"{configuration.ResetTime}"));
                logger?.LogCritical("User time was successful!");
            });
        }

        public async Task SaveCurrentConfigurationAsync()
        {
            if (configuration == null)
                return;
            await Task.Run(async () =>
            {
                await userConfigurationProvider.SaveUserConfigurationForDayAsync(configuration);
            });
        }


        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    callbackTimer?.Dispose();
                    heartbeatTimer?.Dispose();
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~ScreenTimeLocalService()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

    }
}
