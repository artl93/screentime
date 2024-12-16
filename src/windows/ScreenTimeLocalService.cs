﻿using Humanizer;
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

    public class ScreenTimeLocalService : IScreenTimeStateClient, IDisposable
    {
        DateTimeOffset lastKnownTime;
        private DateTimeOffset nextResetDate;
        TimeSpan duration;

        private TimeProvider _timeProvider;
        public UserConfiguration configuration;
        private UserStateProvider _stateProvider;
        private TimeSpan _resetTime;
        private ITimer callbackTimer;
        private bool disposedValue = false;

        enum State
        {
            active,
            inactive
        }

        State currentState = State.inactive;
        private bool disposedValue1;

        public ScreenTimeLocalService(TimeProvider timeProvider, UserConfiguration userConfiguration, UserStateProvider stateProvider)
        {
            _timeProvider = timeProvider;
            configuration = userConfiguration;
            _stateProvider = stateProvider;
            _resetTime = TimeSpan.Parse($"{userConfiguration.ResetTime}");
            nextResetDate = GetNextResetTime(_resetTime);

            stateProvider.LoadState(out lastKnownTime, out duration);
            // data corruption issue
            if (lastKnownTime.UtcDateTime - duration >= _timeProvider.GetUtcNow())
            {
                lastKnownTime = _timeProvider.GetUtcNow();
                duration = TimeSpan.Zero;
            }

            callbackTimer = _timeProvider.CreateTimer(UpdateInteractiveTime, this, TimeSpan.Zero, TimeSpan.FromSeconds(1));
        }

        private DateTimeOffset GetNextResetTime(TimeSpan resetTime)
        {
            var resetOffset = -(_timeProvider.LocalTimeZone.BaseUtcOffset) + resetTime;
            var newResetTime = _timeProvider.GetUtcNow().Date + resetOffset;
            var utcResetTime = new DateTimeOffset(newResetTime, TimeSpan.FromHours(0));

            if (utcResetTime < _timeProvider.GetUtcNow())
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
            if (disposedValue) {
                return;
            }
            // get the current time

            var currentTime = _timeProvider.GetUtcNow();
            var timeSinceLast = currentTime - lastKnownTime;

            if ((currentTime >= nextResetDate))
            {
                // reset the duration once per day
                var delta = currentTime - nextResetDate;
                nextResetDate = GetNextResetTime(_resetTime);
                duration = delta;
            }
            else if (currentState == State.active)
            {
                duration += timeSinceLast;
            }


            // set the last known time to the current time
            lastKnownTime = currentTime;
            // save the state
            _stateProvider.SaveState(lastKnownTime, duration);
        }


        public void EndSessionAsync()
        {
            currentState = State.inactive;
        }
        public void StartSessionAsync()
        {
            currentState = State.active;
        }

        public Task<UserStatus?> GetInteractiveTimeAsync()
        {
            var interactiveTime = duration;
            var dailyTimeLimit = TimeSpan.FromMinutes(configuration.DailyLimitMinutes);
            var warningTime = TimeSpan.FromMinutes(configuration.WarningTimeMinutes);
            var graceTime = TimeSpan.FromMinutes(configuration.GraceMinutes);

            return Task.FromResult<UserStatus?>(GetUserStatus(interactiveTime, dailyTimeLimit, warningTime, graceTime));
        }


        // get the user status based on the time logged in
        // keey this in sync with the server version of this method
        private UserStatus GetUserStatus(TimeSpan interactiveTime, TimeSpan dailyTimeLimit, TimeSpan warningTime, TimeSpan gracePeriod)
        {

            // get user status based on time logged in
            // if the user has gone over the limit + the grade period, log them off
            if (interactiveTime >= dailyTimeLimit + gracePeriod)
            {
                return new UserStatus(interactiveTime, "🛡️", "logout", Status.Lock, dailyTimeLimit);
            }
            else if (interactiveTime >= dailyTimeLimit)
            {
                return new UserStatus(interactiveTime, "🛑", "logout", Status.Error, dailyTimeLimit);
            }
            else if (dailyTimeLimit - interactiveTime <= warningTime && dailyTimeLimit - interactiveTime > TimeSpan.Zero)
            {
                return new UserStatus(interactiveTime, "⚠️", "none", Status.Warn, dailyTimeLimit);
            }
            else
            {
                return new UserStatus(interactiveTime, "⏳", "none", Status.Okay, dailyTimeLimit);
            }
        }

        public Task<UserMessage?> GetMessage()
        {
            return Task.FromResult<UserMessage?>(GetUserMessage());
        }

        private UserMessage GetUserMessage()
        {
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
        }

        public Task<UserConfiguration?> GetUserConfigurationAsync()
        {
            return Task.FromResult<UserConfiguration?>(configuration);
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
