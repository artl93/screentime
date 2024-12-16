using Microsoft.Win32;

namespace ScreenTime
{
    public static class UserConfigurationReader
    {
        const string _baseKey = @"HKEY_CURRENT_USER\Software\ScreenTime";
        const string _defaultResetTime = "06:00:00";

        internal static UserConfiguration GetConfiguration()
        {
            var dailyLimit = GetRegistryIntValue(_baseKey, "DailyLimit", 120);
            var warningTime = GetRegistryIntValue(_baseKey, "WarningTime", 10);
            var warningInterval = GetRegistryIntValue(_baseKey, "WarningInterval", 60);
            var graceMinutes = GetRegistryIntValue(_baseKey, "GraceMinutes", 5);
            var dailyResetTime = Registry.GetValue(_baseKey, "DailyResetTime", _defaultResetTime);
            var dailyResetString = dailyResetTime == null ? _defaultResetTime : dailyResetTime.ToString() ?? _defaultResetTime;

            return new UserConfiguration(Guid.NewGuid(), Environment.UserName, dailyLimit, warningTime, warningInterval, graceMinutes, dailyResetString);

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
