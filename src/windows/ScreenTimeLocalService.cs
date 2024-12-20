using Humanizer;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;
[assembly: InternalsVisibleTo("ScreenTimeTest")]

namespace ScreenTime
{
    public partial class ScreenTimeLocalService : IScreenTimeStateClient, IDisposable
    {
        private DateTimeOffset lastKnownTime;
        private DateTimeOffset nextResetDate;
        private TimeSpan duration;
        private TimeProvider _timeProvider;
        public UserConfiguration configuration;
        private UserStateProvider _stateProvider;
        private TimeSpan _resetTime;
        private ITimer callbackTimer;
        private bool disposedValue = false;
        private ActivityState currentState = ActivityState.Inactive;
        private bool disposedValue1;
        private UserState lastUserState;

        public event EventHandler? OnDayRollover;
        public event EventHandler? OnTimeUpdate;
        public event EventHandler<UserStatusEventArgs>? OnUserStatusChanged;



        public ScreenTimeLocalService(TimeProvider timeProvider, UserConfiguration userConfiguration, UserStateProvider stateProvider)
        {
            _timeProvider = timeProvider;
            configuration = userConfiguration;
            _stateProvider = stateProvider;
            _resetTime = TimeSpan.Parse($"{userConfiguration.ResetTime}");
            nextResetDate = GetNextResetTime(_resetTime);

            stateProvider.LoadState(out lastKnownTime, out duration);
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

            // this should have been called in CreateTimer
            DoUpdateTime();
            callbackTimer = _timeProvider.CreateTimer(UpdateInteractiveTime, this, TimeSpan.Zero, TimeSpan.FromSeconds(1));
        }

        private DateTimeOffset GetNextResetTime(TimeSpan resetTime)
        {
            var resetOffset = -(_timeProvider.LocalTimeZone.BaseUtcOffset) + resetTime;
            var newResetTime = _timeProvider.GetUtcNow().Date + resetOffset;
            var utcResetTime = new DateTimeOffset(newResetTime, TimeSpan.FromHours(0));

            if (utcResetTime <= _timeProvider.GetUtcNow())
            {
                return utcResetTime.AddDays(1);
            }
            else
            {
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
            // save the state
            _stateProvider.SaveState(lastKnownTime, duration);
        }


        private void DoUpdateTime()
        {
            var currentTime = _timeProvider.GetUtcNow();
            var timeSinceLast = currentTime - lastKnownTime;

            if (currentTime >= nextResetDate)
            {
                var delta = currentTime - nextResetDate;
                delta = currentState == ActivityState.Active ? delta.Add(TimeSpan.FromDays(delta.Days * -1)) : TimeSpan.FromMinutes(0);
                nextResetDate = GetNextResetTime(_resetTime);
                duration = delta;
                OnDayRollover?.Invoke(this, EventArgs.Empty);
            }
            else if (currentState == ActivityState.Active)
            {
                duration += timeSinceLast;
            }

            lastKnownTime = currentTime;
            OnTimeUpdate?.Invoke(this, EventArgs.Empty);
            PostStatusChanges();
            PostMessage();
        }

        private void PostStatusChanges()
        {
            var interactiveTime = duration;
            var dailyTimeLimit = TimeSpan.FromMinutes(configuration.DailyLimitMinutes);
            var warningTime = TimeSpan.FromMinutes(configuration.WarningTimeMinutes);
            var graceTime = TimeSpan.FromMinutes(configuration.GraceMinutes);
            var status = GetUserStatus(interactiveTime, dailyTimeLimit, warningTime, graceTime);
            if (status.State != lastUserState)
            {
                lastUserState = status.State;
                OnUserStatusChanged?.Invoke(this, new UserStatusEventArgs(status, _timeProvider.GetUtcNow(), interactiveTime));
            }
        }

        public event EventHandler<MessageEventArgs>? OnMessageUpdate;

        private void PostMessage()
        {
            var message = GetUserMessage();
            if (message != null)
            {
                OnMessageUpdate?.Invoke(this, new MessageEventArgs(message));
            }
        }

        public void EndSessionAsync()
        {
            DoUpdateTime();
            currentState = ActivityState.Inactive;
        }

        public void StartSessionAsync()
        {
            DoUpdateTime();
            currentState = ActivityState.Active;
        }

        public Task<UserStatus?> GetInteractiveTimeAsync()
        {
            var interactiveTime = duration;
            var dailyTimeLimit = TimeSpan.FromMinutes(configuration.DailyLimitMinutes);
            var warningTime = TimeSpan.FromMinutes(configuration.WarningTimeMinutes);
            var graceTime = TimeSpan.FromMinutes(configuration.GraceMinutes);

            return Task.FromResult<UserStatus?>(GetUserStatus(interactiveTime, dailyTimeLimit, warningTime, graceTime));
        }

        private static UserStatus GetUserStatus(TimeSpan interactiveTime, TimeSpan dailyTimeLimit, TimeSpan warningTime, TimeSpan gracePeriod)
        {
            // get user status based on time logged in
            // if the user has gone over the limit + the grade period, log them off
            if (interactiveTime >= dailyTimeLimit + gracePeriod)
            {
                return new UserStatus(interactiveTime, "🛡️", "logout", UserState.Lock, dailyTimeLimit);
            }
            else if (interactiveTime >= dailyTimeLimit)
            {
                return new UserStatus(interactiveTime, "🛑", "logout", UserState.Error, dailyTimeLimit);
            }
            else if (dailyTimeLimit - interactiveTime <= warningTime && dailyTimeLimit - interactiveTime > TimeSpan.Zero)
            {
                return new UserStatus(interactiveTime, "⚠️", "none", UserState.Warn, dailyTimeLimit);
            }
            else
            {
                return new UserStatus(interactiveTime, "⏳", "none", UserState.Okay, dailyTimeLimit);
            }
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

        public void Reset()
        {
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
                    callbackTimer.Dispose();
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
