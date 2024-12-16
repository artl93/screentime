using Microsoft.Win32;

namespace ScreenTime
{
    public class UserStateProvider
    {
        const string _baseKey = @"HKEY_CURRENT_USER\Software\ScreenTime";


        public virtual void SaveState(DateTimeOffset lastKnownTime, TimeSpan duration)
        {
            // write the time to the registry
            Registry.SetValue(_baseKey, "Last", lastKnownTime.UtcDateTime.ToString("o"));
            Registry.SetValue(_baseKey, "Cumulative", duration.ToString("G"));
        }

        public virtual void LoadState(out DateTimeOffset lastKnownTime, out TimeSpan duration)
        {
            lastKnownTime = DateTimeOffset.MinValue;
            duration = TimeSpan.Zero;
            // load the last known time and duration from the registry
            var lastKnownTimeObject = Registry.GetValue(_baseKey, "Last", null);
            var durationObject = Registry.GetValue(_baseKey, "Cumulative", null);
            if (lastKnownTimeObject != null)
            {
                _ = DateTimeOffset.TryParse(lastKnownTimeObject.ToString(), out lastKnownTime);
            }
            if (durationObject != null)
            {
                _ = TimeSpan.TryParse(durationObject.ToString(), out duration);
            }
        }
    }
}
