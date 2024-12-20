﻿using Microsoft.Win32;

namespace ScreenTime
{
    public class UserStateProvider
    {
        const string _baseKey = @"HKEY_CURRENT_USER\Software\ScreenTime";


        public virtual void SaveState(DateTimeOffset lastKnownTime, TimeSpan duration, UserState state, DateTimeOffset timeLastMessageShown)
        {
            // write the time to the registry
            Registry.SetValue(_baseKey, "Last", lastKnownTime.UtcDateTime.ToString("o"));
            Registry.SetValue(_baseKey, "Cumulative", duration.ToString("G"));
            Registry.SetValue(_baseKey, "State", state.ToString());
            Registry.SetValue(_baseKey, "LastMessage", timeLastMessageShown.UtcDateTime.ToString("o"));
        }

        public virtual void LoadState(out DateTimeOffset lastKnownTime, out TimeSpan duration, out UserState state, out DateTimeOffset timeLastMessageShown)
        {
            lastKnownTime = DateTimeOffset.MinValue;
            duration = TimeSpan.Zero;
            state = UserState.Okay;
            timeLastMessageShown = DateTimeOffset.MinValue;
            // load the last known time and duration from the registry
            var lastKnownTimeObject = Registry.GetValue(_baseKey, "Last", null);
            var durationObject = Registry.GetValue(_baseKey, "Cumulative", null);
            var stateObject = Registry.GetValue(_baseKey, "State", null);
            var timeLastMessageShownObject = Registry.GetValue(_baseKey, "LastMessage", null);
            if (lastKnownTimeObject != null)
            {
                _ = DateTimeOffset.TryParse(lastKnownTimeObject.ToString(), out lastKnownTime);
            }
            if (durationObject != null)
            {
                _ = TimeSpan.TryParse(durationObject.ToString(), out duration);
            }
            if (stateObject != null)
            {
                _ = Enum.TryParse(stateObject.ToString(), out state);
            }
            if (timeLastMessageShownObject != null)
            {
                _ = DateTimeOffset.TryParse(timeLastMessageShownObject.ToString(), out timeLastMessageShown);
            }
        }
    }
}
