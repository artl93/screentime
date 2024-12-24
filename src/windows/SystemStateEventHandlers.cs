using Microsoft.Win32;

namespace ScreenTime
{

    public class SystemStateEventHandlers
    {
        private readonly IScreenTimeStateClient _client;

        public SystemStateEventHandlers(IScreenTimeStateClient client)
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
            _client.EndSession(Enum.GetName(e.Reason)??string.Empty);
        }

        void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            switch (e.Mode)
            {
                case PowerModes.Resume:
                    _client.StartSession(Enum.GetName(e.Mode) ?? string.Empty);
                    break;
                case PowerModes.Suspend:
                    _client.EndSession(Enum.GetName(e.Mode) ?? string.Empty);
                    break;
            }
        }

        void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            switch (e.Reason)
            {
                case SessionSwitchReason.SessionLock:
                case SessionSwitchReason.SessionLogoff:
                case SessionSwitchReason.ConsoleDisconnect:
                    _client.EndSession(Enum.GetName(e.Reason) ?? string.Empty);
                    break;
                case SessionSwitchReason.SessionUnlock:
                case SessionSwitchReason.SessionLogon:
                case SessionSwitchReason.ConsoleConnect:
                    _client.StartSession(Enum.GetName(e.Reason)??string.Empty);

                    break;
            }
        }
    }

}