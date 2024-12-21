using Humanizer;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("ScreenTimeTest")]

namespace ScreenTime
{
    public partial class ScreenTimeLocalService(TimeProvider timeProvider, UserConfiguration userConfiguration, UserStateProvider stateProvider, ILogger? logger) : IScreenTimeStateClient, IDisposable, IHostedService
    {
        private DateTimeOffset lastKnownTime;
        private DateTimeOffset nextResetDate;
        private TimeSpan duration;
        private TimeProvider _timeProvider = timeProvider;
        public UserConfiguration configuration = userConfiguration;
        private UserStateProvider _stateProvider = stateProvider;
        private TimeSpan _resetTime = TimeSpan.Zero;
        private ITimer? callbackTimer;
        private bool disposedValue = false;
        private ActivityState activityState = ActivityState.Unknown;
        private bool disposedValue1;
        private UserState lastUserState;
        private DateTimeOffset lastMessageShown;
        bool started = false;
        private readonly ILogger? logger = logger;

        public event EventHandler<MessageEventArgs>? OnDayRollover;
        public event EventHandler<UserStatusEventArgs>? OnTimeUpdate;
        public event EventHandler<UserStatusEventArgs>? OnUserStatusChanged;
        public event EventHandler<MessageEventArgs>? OnMessageUpdate;


        public Task StartAsync(CancellationToken cancellationToken)
        {
            logger?.LogInformation("Starting ScreenTimeLocalService");
            _resetTime = TimeSpan.Parse($"{configuration.ResetTime}");
            nextResetDate = GetNextResetTime(_resetTime);

            _stateProvider.LoadState(out lastKnownTime, out duration, out lastUserState, out lastMessageShown, out activityState);
            // data corruption issue
            if (lastKnownTime == DateTimeOffset.MinValue || lastKnownTime.UtcDateTime - duration >= _timeProvider.GetUtcNow())
            {
                lastKnownTime = _timeProvider.GetUtcNow();
                duration = TimeSpan.Zero;
            }
            // if it has been more than 24 hours since the last reset, reset the duration
            else if (lastKnownTime.UtcDateTime <= nextResetDate.AddDays(-1) || duration >= TimeSpan.FromDays(1))
            {
                duration = TimeSpan.Zero;
                lastKnownTime = _timeProvider.GetUtcNow();
            }
            if (activityState != ActivityState.Active)
            {
                 // only count the time in between the last state and now if the last state was active
                 // otherwise, throw it out.
                 lastKnownTime = _timeProvider.GetUtcNow();
            }
            activityState = ActivityState.Active;


            started = true;
            // this should have been called in CreateTimer
            callbackTimer = _timeProvider.CreateTimer(UpdateInteractiveTime, this, TimeSpan.Zero, TimeSpan.FromSeconds(1));

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            logger?.LogInformation("Stopping ScreenTimeLocalService");
            return Task.CompletedTask;
        }

        private DateTimeOffset GetNextResetTime(TimeSpan resetTime)
        {
            var resetOffset = -(_timeProvider.LocalTimeZone.BaseUtcOffset) + resetTime;
            var newResetTime = _timeProvider.GetUtcNow().Date + resetOffset;
            var utcResetTime = new DateTimeOffset(newResetTime, TimeSpan.FromHours(0));

            if (utcResetTime <= _timeProvider.GetUtcNow())
            {
                logger?.LogInformation("Next reset time is {0}", utcResetTime.AddDays(1));
                return utcResetTime.AddDays(1);
            }
            else
            {
                logger?.LogInformation("Next reset time is {0}", utcResetTime);
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
            if (!started)
                return; 
            UpdateIdleTime();
            var currentTime = _timeProvider.GetUtcNow();
            var timeSinceLast = currentTime - lastKnownTime;

            if (nextResetDate == DateTimeOffset.MinValue)
            {
                nextResetDate = GetNextResetTime(_resetTime);
                duration = TimeSpan.Zero;
            }

            if (currentTime >= nextResetDate)
            {
                var delta = currentTime - nextResetDate;
                delta = activityState == ActivityState.Active ? delta.Add(TimeSpan.FromDays(delta.Days * -1)) : TimeSpan.FromMinutes(0);
                nextResetDate = GetNextResetTime(_resetTime);
                duration = delta;
                OnDayRollover?.Invoke(this, new MessageEventArgs(new UserMessage(
                    "Day rollover",
                    "It's a new day! Your time has been reset.",
                    "🎉",
                    "none"
                    ) ));
            }
            else if (activityState == ActivityState.Active)
            {
                duration += timeSinceLast;
            }

            lastKnownTime = currentTime;
            OnTimeUpdate?.Invoke(this, new UserStatusEventArgs(
                GetUserStatus(duration, TimeSpan.FromMinutes(configuration.DailyLimitMinutes), TimeSpan.FromMinutes(configuration.WarningTimeMinutes), 
                TimeSpan.FromMinutes(configuration.GraceMinutes)), 
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

        }

        private void PostStatusChanges()
        {
            var interactiveTime = duration;
            var dailyTimeLimit = TimeSpan.FromMinutes(configuration.DailyLimitMinutes);
            var warningTime = TimeSpan.FromMinutes(configuration.WarningTimeMinutes);
            var graceTime = TimeSpan.FromMinutes(configuration.GraceMinutes);
            var status = GetUserStatus(interactiveTime, dailyTimeLimit, warningTime, graceTime);
            OnUserStatusChanged?.Invoke(this, new UserStatusEventArgs(status, _timeProvider.GetUtcNow(), interactiveTime));
        }


        private void PostMessages(bool stateChanged)
        {
            // state change or debounced
            if (stateChanged || 
                (_timeProvider.GetUtcNow() - lastMessageShown < TimeSpan.FromMinutes(1)))
            {
                return;
            }
            var message = GetUserMessage();
            if (message != null)
            {
                OnMessageUpdate?.Invoke(this, new MessageEventArgs(message));
                lastMessageShown = _timeProvider.GetUtcNow();
            }
        }

        public void EndSessionAsync(string reason)
        {
            logger?.LogInformation($"End session called ({reason}) - {duration}");
            activityState = ActivityState.Inactive;
            DoUpdateTime();
            PostStatusChanges();
            // todo - ensure time transitioned here
        }


        public void StartSessionAsync(string reason)
        {
            logger?.LogInformation($"Start session called. ({reason}) - {duration}");
            activityState = ActivityState.Active;
            // todo - ensure time transitioned here
            DoUpdateTime();
            PostStatusChanges();
        }

        public Task<UserStatus?> GetInteractiveTimeAsync()
        {
            var interactiveTime = duration;
            var dailyTimeLimit = TimeSpan.FromMinutes(configuration.DailyLimitMinutes);
            var warningTime = TimeSpan.FromMinutes(configuration.WarningTimeMinutes);
            var graceTime = TimeSpan.FromMinutes(configuration.GraceMinutes);

            return Task.FromResult<UserStatus?>(GetUserStatus(interactiveTime, dailyTimeLimit, warningTime, graceTime));
        }

        private UserState GetUserState()
        {
            return GetUserState(duration);
        }

        private UserState GetUserState(TimeSpan interactiveTime)
        {
            var dailyTimeLimit = TimeSpan.FromMinutes(configuration.DailyLimitMinutes);
            var warningTime = TimeSpan.FromMinutes(configuration.WarningTimeMinutes);
            var graceTime = TimeSpan.FromMinutes(configuration.GraceMinutes);
            return GetUserState(interactiveTime, dailyTimeLimit, warningTime, graceTime);
        }

        private static UserState GetUserState(TimeSpan interactiveTime, TimeSpan dailyTimeLimit, TimeSpan warningTime, TimeSpan gracePeriod)
        {
            if (interactiveTime >= dailyTimeLimit + gracePeriod)
            {
                return UserState.Lock;
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

        private static UserStatus GetUserStatus(TimeSpan interactiveTime, TimeSpan dailyTimeLimit, TimeSpan warningTime, TimeSpan gracePeriod)
        {
            var state = GetUserState(interactiveTime, dailyTimeLimit, warningTime, gracePeriod);
            return state switch
            {
                UserState.Lock => new UserStatus(interactiveTime, "🛡️", "logout", UserState.Lock, dailyTimeLimit),
                UserState.Error => new UserStatus(interactiveTime, "🛑", "logout", UserState.Error, dailyTimeLimit),
                UserState.Warn => new UserStatus(interactiveTime, "⚠️", "none", UserState.Warn, dailyTimeLimit),
                UserState.Okay => new UserStatus(interactiveTime, "⏳", "none", UserState.Okay, dailyTimeLimit),
                _ => new UserStatus(interactiveTime, "❌", "none", UserState.Invalid, dailyTimeLimit)
            };
        }

        public Task<UserMessage?> GetMessage()
        {
            return Task.FromResult<UserMessage?>(GetUserMessage());
        }

        private UserMessage GetUserMessage()
        {
            var interactiveTime = duration;
            var dailyTimeLimit = TimeSpan.FromMinutes(configuration.DailyLimitMinutes);
            var warningTime = TimeSpan.FromMinutes(configuration.WarningTimeMinutes);
            var graceTime = TimeSpan.FromMinutes(configuration.GraceMinutes);

            var interactiveTimeString = TimeSpanHumanizeExtensions.Humanize(interactiveTime);
            var allowedTimeString = TimeSpanHumanizeExtensions.Humanize(dailyTimeLimit);

            // when time is up, log them off
            if (interactiveTime >= dailyTimeLimit)
            {
                if (interactiveTime > dailyTimeLimit + graceTime)
                {
                    // if they have gone over the limit, log them off
                    return new UserMessage("Hey, you're done.", $"You have been logged for {interactiveTimeString} today. You're allowed {allowedTimeString} today. You have gone over by {TimeSpanHumanizeExtensions.Humanize(interactiveTime - dailyTimeLimit)}", "🛡️", "lock");
                }
                return new UserMessage("Time to log out.", $"You have been logged for {interactiveTimeString} today. You're allowed {allowedTimeString} today.", "🛑", "logout");
            }

            // when they have 10 minutes left, warn them every one minute
            if (dailyTimeLimit - interactiveTime <= warningTime && dailyTimeLimit - interactiveTime > TimeSpan.Zero)
            {
                if ((dailyTimeLimit - interactiveTime).Minutes % 1 == 0)
                {
                    var remainingTimeString = TimeSpanHumanizeExtensions.Humanize(dailyTimeLimit - interactiveTime);
                    return new UserMessage("Time Warning", $"You have {remainingTimeString} left out of {allowedTimeString}", "⏳", "warn");
                }
            }

            return new UserMessage("Time Logged", $"You have been logged for {interactiveTimeString} today out of {allowedTimeString}", "🕒", "none");
        }

        public Task<UserConfiguration?> GetUserConfigurationAsync()
        {
            return Task.FromResult<UserConfiguration?>(configuration);
        }

        void UpdateIdleTime()
        {
            var idleTime = IdleTimeDetector.GetIdleTime();
            if (idleTime.TotalMinutes >= 5) // Notify if idle for 5 minutes
            {
                EndSessionInternal();
                Utilities.LogToConsole("The system has been idle for 5 minutes.");
            }
            else
            {
                StartSessionInternal();
            }
        }

        private void EndSessionInternal()
        {
            // activityState = ActivityState.Inactive;
        }

        private void StartSessionInternal()
        {
            // activityState = ActivityState.Active;
        }

        public void Reset()
        {
            logger?.LogCritical("User time was reset!");
            lastKnownTime = _timeProvider.GetUtcNow();
            duration = TimeSpan.Zero;
            nextResetDate = GetNextResetTime(_resetTime);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue1)
            {
                if (disposing)
                {
                    callbackTimer?.Dispose();
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue1 = true;
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
