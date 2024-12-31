using Microsoft.Win32;
using System.Configuration;
using System.Text.Json;

namespace ScreenTimeClient
{
    public class UserConfigurationRegistryReader : IUserConfigurationReader
    {
        const string _baseKey = @"HKEY_CURRENT_USER\Software\ScreenTime\Config";
        const string _defaultResetTime = "06:00:00";
        readonly JsonSerializerOptions serializerOptions = new()
        { IncludeFields = true, };

        public UserConfiguration GetConfiguration()
        {
            var dailyLimit = GetRegistryIntValue(_baseKey, "DailyLimit", 120);
            var warningTime = GetRegistryIntValue(_baseKey, "WarningTime", 10);
            var warningInterval = GetRegistryIntValue(_baseKey, "WarningInterval", 60);
            var graceMinutes = GetRegistryIntValue(_baseKey, "GraceMinutes", 5);
            var dailyResetTime = Registry.GetValue(_baseKey, "DailyResetTime", _defaultResetTime);
            var dailyResetString = dailyResetTime == null ? _defaultResetTime : dailyResetTime.ToString() ?? _defaultResetTime;
            var extensionsString = Registry.GetValue(_baseKey, "Extensions", null);
            var extensions = JsonSerializer.Deserialize<(DateTimeOffset, int)[]>(extensionsString?.ToString() ?? "[]", serializerOptions);
            var disableLock = GetRegistryIntValue(_baseKey, "DisableLock", 0) == 1; // convert to boolean
            var delayLockSeconds = GetRegistryIntValue(_baseKey, "DelayLockSeconds", 10);
            var enableOnline = GetRegistryIntValue(_baseKey, "EnableOnline", 0) == 1; // convert to boolean
            delayLockSeconds = Math.Max(5, delayLockSeconds); // avoid pathologically low values

            return new UserConfiguration(Environment.UserName, dailyLimit, 
                warningTime, warningInterval, graceMinutes, dailyResetString, disableLock, delayLockSeconds, enableOnline, extensions?.ToList());

        }

        public void SetConfiguration(UserConfiguration configuration)
        {
            Registry.SetValue(_baseKey, "DailyLimit", configuration.DailyLimitMinutes);
            Registry.SetValue(_baseKey, "WarningTime", configuration.WarningTimeMinutes);
            Registry.SetValue(_baseKey, "WarningInterval", configuration.WarningIntervalSeconds);
            Registry.SetValue(_baseKey, "GraceMinutes", configuration.GraceMinutes);
            Registry.SetValue(_baseKey, "DailyResetTime", configuration.ResetTime);
            Registry.SetValue(_baseKey, "DisableLock", configuration.DisableLock ? 1 : 0); // convert to int
            Registry.SetValue(_baseKey, "DelayLockSeconds", configuration.DelayLockSeconds);
            Registry.SetValue(_baseKey, "EnableOnline", configuration.EnableOnline ? 1 : 0); // convert to int
            Registry.SetValue(_baseKey, "Extensions", System.Text.Json.JsonSerializer.Serialize(configuration.Extensions, serializerOptions));
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
