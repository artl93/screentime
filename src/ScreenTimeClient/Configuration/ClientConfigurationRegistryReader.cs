using Microsoft.Win32;

namespace ScreenTimeClient.Configuration
{
    public class ClientConfigurationRegistryReader : IClientConfigurationReader
    {
        const string _baseKey = @"HKEY_CURRENT_USER\Software\ScreenTime";

        public ClientConfiguration GetConfiguration()
        {
            var warningInterval = GetRegistryIntValue(_baseKey, "WarningInterval", 60);
            var enableOnline = GetRegistryIntValue(_baseKey, "EnableOnline", 0) == 1; // convert to boolean

            return new ClientConfiguration(warningInterval, enableOnline);

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
