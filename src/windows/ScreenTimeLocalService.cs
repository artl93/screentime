﻿using Humanizer;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
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

    public class UserConfigurationReader
    {
        const string _baseKey = @"HKEY_CURRENT_USER\Software\ScreenTime";

        public UserConfiguration GetConfigurationFromRegistry()
        {
            var dailyLimit = GetRegistryIntValue(_baseKey, "DailyLimit", 120);
            var warningTime = GetRegistryIntValue(_baseKey, "WarningTime", 10);
            var warningInterval = GetRegistryIntValue(_baseKey, "WarningInterval", 60);
            return new UserConfiguration(Guid.NewGuid(), Environment.UserName, dailyLimit, warningTime, warningInterval);

        }

        private int GetRegistryIntValue(string key, string valueName, int defaultValue)
        {
            var objectValue = Registry.GetValue(key, valueName, defaultValue);
            if (objectValue == null)
            {
                return defaultValue;
            }
            else if (objectValue is int intValue)
            {
                return intValue;
            }
            else if (objectValue is string stringValue)
            {
                if (int.TryParse(stringValue, out int result))
                {
                    return result;
                }
            }
            return defaultValue;
        }
    }

    public class UserStateProvider
    {
        const string _baseKey = @"HKEY_CURRENT_USER\Software\ScreenTime";


        public void SaveState(DateTimeOffset lastKnownTime, TimeSpan duration)
        {
            // write the time to the registry
            Registry.SetValue(_baseKey, "Last", lastKnownTime.ToString("o"));
            Registry.SetValue(_baseKey, "Cumulative", duration.ToString("G"));
        }

        public void LoadState(out DateTimeOffset lastKnownTime, out TimeSpan duration)
        {
            // load the last known time and duration from the registry
            var lastKnownTimeObject = Registry.GetValue(_baseKey, "Last", null);
            var durationObject = Registry.GetValue(_baseKey, "Cumulative", null);
            // if the last known time minus the duration adds up to a time that would be yesterday or earlier (local time), reset the duration
            if (lastKnownTimeObject != null && durationObject != null)
            {
                _ = DateTimeOffset.TryParse(lastKnownTimeObject.ToString(), out lastKnownTime);
                _ = TimeSpan.TryParse(durationObject.ToString(), out duration);
                if (lastKnownTime.ToLocalTime() < DateTime.Today)
                {
                    duration = TimeSpan.Zero;
                }
            }
            else
            {
                lastKnownTime = DateTimeOffset.Now;
                duration = TimeSpan.Zero;
            }
        }
    }

    public class ScreenTimeLocalService : IScreenTimeStateClient
    {
        DateTimeOffset lastKnownTime;
        TimeSpan duration;
        private TimeProvider _timeProvider;
        public UserConfiguration configuration;
        private UserStateProvider _stateProvider;

        private bool disposedValue = false;

        enum State
        {
            active,
            inactive
        }

        State currentState = State.inactive;


        public ScreenTimeLocalService(TimeProvider timeProvider, UserConfiguration userConfiguration, UserStateProvider stateProvider)
        {
            _timeProvider = timeProvider;
            configuration = userConfiguration;
            _stateProvider = stateProvider;

            _timeProvider.CreateTimer(UpdateInteractiveTime, this, TimeSpan.Zero, TimeSpan.FromSeconds(1));
        }

        private void UpdateInteractiveTime(object? state)
        {
            if (disposedValue) {
                return;
            }
            // get the current time
            var currentTime = _timeProvider.GetUtcNow();
            // calculate the time since the last known time
            var timeSinceLast = currentTime - lastKnownTime;
            // add the time since the last known time to the duration if the user is active
            if (currentState == State.active)
            {
                duration += timeSinceLast;
            }
            // set the last known time to the current time
            lastKnownTime = currentTime;
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
    }
}