using Microsoft.Win32;

namespace ScreenTime
{
    public class UserConfigurationReader
    {
        const string _baseKey = @"HKEY_CURRENT_USER\Software\ScreenTime\Config";
        const string _defaultResetTime = "06:00:00";

        public UserConfiguration GetConfiguration()
        {
            var dailyLimit = GetRegistryIntValue(_baseKey, "DailyLimit", 120);
            var warningTime = GetRegistryIntValue(_baseKey, "WarningTime", 10);
            var warningInterval = GetRegistryIntValue(_baseKey, "WarningInterval", 60);
            var graceMinutes = GetRegistryIntValue(_baseKey, "GraceMinutes", 5);
            var dailyResetTime = Registry.GetValue(_baseKey, "DailyResetTime", _defaultResetTime);
            var dailyResetString = dailyResetTime == null ? _defaultResetTime : dailyResetTime.ToString() ?? _defaultResetTime;

            return new UserConfiguration(Guid.NewGuid(), Environment.UserName, dailyLimit, warningTime, warningInterval, graceMinutes, dailyResetString);

        }

        public void SetConfiguration(UserConfiguration configuration)
        {
            Registry.SetValue(_baseKey, "DailyLimit", configuration.DailyLimitMinutes);
            Registry.SetValue(_baseKey, "WarningTime", configuration.WarningTimeMinutes);
            Registry.SetValue(_baseKey, "WarningInterval", configuration.WarningIntervalSeconds);
            Registry.SetValue(_baseKey, "GraceMinutes", configuration.GraceMinutes);
            Registry.SetValue(_baseKey, "DailyResetTime", configuration.ResetTime);
        }

        private static int GetRegistryIntValue(string key, string valueName, int defaultValue)
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
}
