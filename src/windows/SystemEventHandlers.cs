using Microsoft.Win32;

namespace ScreenTime
{

    public class SystemEventHandlers
    {
        private IScreenTimeStateClient _client;

        public SystemEventHandlers(IScreenTimeStateClient client)
        {
            _client = client;

            // hook a bunch of system events. 
            SystemEvents.SessionSwitch += new SessionSwitchEventHandler(SystemEvents_SessionSwitch);
            SystemEvents.PowerModeChanged += new PowerModeChangedEventHandler(SystemEvents_PowerModeChanged);
            SystemEvents.SessionEnding += new SessionEndingEventHandler(SystemEvents_SessionEnding);

            SystemEvents.SessionSwitch += new SessionSwitchEventHandler(SystemEvents_SessionSwitch);
            SystemEvents.PowerModeChanged += new PowerModeChangedEventHandler(SystemEvents_PowerModeChanged);
            SystemEvents.SessionEnding += new SessionEndingEventHandler(SystemEvents_SessionEnding);

        }

        void SystemEvents_SessionEnding(object sender, SessionEndingEventArgs e)
        {
            switch (e.Reason)
            {
                case SessionEndReasons.Logoff:
                    _client.EndSessionAsync();
                    Utilities.LogToConsole("The session is ending because the user is logging off.");
                    break;
                case SessionEndReasons.SystemShutdown:
                    _client.EndSessionAsync();
                    Utilities.LogToConsole("The session is ending because the system is shutting down.");
                    break;
            }
        }

        void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            switch (e.Mode)
            {
                case PowerModes.Resume:
                    _client.StartSessionAsync();
                    Utilities.LogToConsole("The system is resuming from a suspended state.");
                    break;
                case PowerModes.Suspend:
                    _client.EndSessionAsync();
                    Utilities.LogToConsole("The system is entering a suspended state.");
                    break;
            }
        }

        void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            Utilities.LogToConsole("Session state changed:" + Enum.GetName(e.Reason));
            switch (e.Reason)
            {
                case SessionSwitchReason.SessionLock:
                case SessionSwitchReason.SessionLogoff:
                case SessionSwitchReason.ConsoleDisconnect:
                    _client.EndSessionAsync();
                    break;
                case SessionSwitchReason.SessionUnlock:
                case SessionSwitchReason.SessionLogon:
                case SessionSwitchReason.ConsoleConnect:
                    _client.StartSessionAsync();
                    break;
            }
        }
    }

}