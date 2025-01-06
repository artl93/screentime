using Microsoft.Win32;
using ScreenTime.Common;

namespace ScreenTimeClient
{
    public class UserStateRegistryProvider
    {
        const string _baseKey = @"HKEY_CURRENT_USER\Software\ScreenTime";


        public virtual void SaveState(DateTimeOffset lastKnownTime, TimeSpan duration, UserState state, DateTimeOffset timeLastMessageShown, UserActivityState activityState)
        {
            // write the time to the registry
            Registry.SetValue(_baseKey, "Last", lastKnownTime.UtcDateTime.ToString("o"));
            Registry.SetValue(_baseKey, "Cumulative", duration.ToString("G"));
            Registry.SetValue(_baseKey, "State", state.ToString());
            Registry.SetValue(_baseKey, "LastMessage", timeLastMessageShown.UtcDateTime.ToString("o"));
            Registry.SetValue(_baseKey, "UserActivityState", activityState.ToString());

        }

        public virtual void LoadState(out DateTimeOffset lastKnownTime, out TimeSpan duration, out UserState state, out DateTimeOffset timeLastMessageShown, out UserActivityState activityState)
        {
            lastKnownTime = DateTimeOffset.MinValue;
            duration = TimeSpan.Zero;
            state = UserState.Okay;
            timeLastMessageShown = DateTimeOffset.MinValue;
            activityState = UserActivityState.Unknown;
            // load the last known time and duration from the registry
            var lastKnownTimeObject = Registry.GetValue(_baseKey, "Last", null);
            var durationObject = Registry.GetValue(_baseKey, "Cumulative", null);
            var stateObject = Registry.GetValue(_baseKey, "State", null);
            var timeLastMessageShownObject = Registry.GetValue(_baseKey, "LastMessage", null);
            var activityStateObject = Registry.GetValue(_baseKey, "UserActivityState", null);
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
            if (activityStateObject != null)
            {
                _ = Enum.TryParse(activityStateObject.ToString(), out activityState);
            }
        }
    }
}
