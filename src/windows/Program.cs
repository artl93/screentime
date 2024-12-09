using Microsoft.VisualBasic.ApplicationServices;
using Microsoft.Win32;
using System.Text.Json;
using System.DirectoryServices.AccountManagement;
using System.Management;
using System.Runtime.InteropServices;


DateTime startTime = DateTime.Now;
TimeSpan downTime = TimeSpan.Zero;
DateTime downTimeStart = DateTime.Now;
DateTime downTimeEnd = DateTime.Now;
DateTime now = DateTime.Now;



var icon = new NotifyIcon();
icon.Icon = SystemIcons.Application;
icon.ContextMenuStrip = new ContextMenuStrip();
icon.ContextMenuStrip.Items.Add("Exit", null, (s, e) => { icon.Visible = false; Environment.Exit(0); });
icon.Visible = true;

var lastMessageShown = DateTimeOffset.MinValue;

// create the server
// if "devmode" is passed as an argument, use the development server
var baseUri = (args.Length > 0 && args[0] == "devmode") ? "https://localhost:7186" : "https://screentime.azurewebsites.net";
var server = new screentime.Server(baseUri);

// get current user's logged in email from Microsoft identity 

void LogToConsole(string v)
{
    System.Diagnostics.Debug.WriteLine(v);
    System.Console.WriteLine(v);
}




SystemEvents.SessionSwitch += new SessionSwitchEventHandler(SystemEvents_SessionSwitch);
SystemEvents.PowerModeChanged += new PowerModeChangedEventHandler(SystemEvents_PowerModeChanged);
SystemEvents.SessionEnding += new SessionEndingEventHandler(SystemEvents_SessionEnding);

// start the session when the app starts
server.StartSessionAsync();
SystemEvents.SessionSwitch += new SessionSwitchEventHandler(SystemEvents_SessionSwitch);
SystemEvents.PowerModeChanged += new PowerModeChangedEventHandler(SystemEvents_PowerModeChanged);
SystemEvents.SessionEnding += new SessionEndingEventHandler(SystemEvents_SessionEnding);

// start the session when the app starts
server.StartSessionAsync();

void SystemEvents_SessionEnding(object sender, SessionEndingEventArgs e)
{
    switch(e.Reason)
    {
        case SessionEndReasons.Logoff:
            server.EndSessionAsync();
            LogToConsole("The session is ending because the user is logging off.");
            break;
        case SessionEndReasons.SystemShutdown:
            server.EndSessionAsync();
            LogToConsole("The session is ending because the system is shutting down.");
            break;
    }
}

void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
{
    switch (e.Mode)
    {
        case PowerModes.Resume:
            server.StartSessionAsync();
            LogToConsole("The system is resuming from a suspended state.");
            break;
        case PowerModes.Suspend:
            server.EndSessionAsync();
            LogToConsole("The system is entering a suspended state.");
            break;
    }
}

void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
{
    LogToConsole("Session state changed:" + Enum.GetName(e.Reason));
    switch (e.Reason)
    {
        case SessionSwitchReason.SessionLock:
        case SessionSwitchReason.SessionLogoff:
        case SessionSwitchReason.ConsoleDisconnect:
            server.EndSessionAsync();
            break;
        case SessionSwitchReason.SessionUnlock:
        case SessionSwitchReason.SessionLogon:
        case SessionSwitchReason.ConsoleConnect:
            server.StartSessionAsync();
            break;
    }
}

// get notified when the user is idle for a certain amount of time

var task = Task.Run((Func<Task?>)(async () =>
{
    do
    {
        try
        {
            // get configuration if not already initialized: 
            var configuration = await server.GetUserConfigurationAsync();
            if (configuration == null)
            {
                LogToConsole("The server returned a null configuration.");
                continue;
            }

            // check idle time
            UpdateIdleTime();

            var status = await server.GetInteractiveTimeAsync();
            if (status == null)
            {
                LogToConsole("The server returned a null status.");
                continue;
            }

            // humanize the uptime and show it in the icon text

            var humanizedUptime = Humanizer.TimeSpanHumanizeExtensions.Humanize(status.LoggedInTime);
            var humanizedDailyLimit = Humanizer.TimeSpanHumanizeExtensions.Humanize(status.dailyTimeLimit);

            icon.Text = $"{status.Icon} Interactive time: {humanizedUptime} out of {humanizedDailyLimit}.";

            // if status shows no time logged, send a start 
            if (status.LoggedInTime == TimeSpan.Zero)
            {
                server.StartSessionAsync();
            }

            icon.Icon = status.Status switch
            {
                Status.Okay => SystemIcons.Information,
                Status.Warn => SystemIcons.Warning,
                Status.Error => SystemIcons.Error,
                Status.Lock => SystemIcons.Shield,
                _ => SystemIcons.Application
            };

            if (status.Status != Status.Okay)
            {
                ShowMessageAsync(icon, server, configuration);
                // pick the right icon for the notification icon based on the status

            }

            if (status.Status == Status.Lock)
            {
                await Task.Delay(10000);
                server.EndSessionAsync();
                // lock the computer if the status is lock
                LogToConsole("The user's status is locked.");
                Windows.LockWorkStation();
                // LogUserOut();
            }


            await Task.Delay(6000);

        }
        catch (Exception e)
        {
            LogToConsole(e.Message);
        }
    }
    while (true);
}));

void UpdateIdleTime()
{
    var idleTime = IdleTimeDetector.GetIdleTime();
    if (idleTime.TotalMinutes >= 5) // Notify if idle for 5 minutes
    {
        server.EndSessionAsync();
        LogToConsole("The system has been idle for 5 minutes.");
    }
    else
    {
        server.StartSessionAsync();
    }
}

Application.ApplicationExit += (s, e) =>
{
    server.EndSessionAsync();
    icon.Visible = false;
    icon.Dispose();
    task.Dispose();
};
// make sure the main window loop runs
Application.Run(new HiddenForm(task));


async void ShowMessageAsync(NotifyIcon icon, screentime.Server server, UserConfiguration configuration)
{
    // only show every 30 seconds
    if (DateTimeOffset.Now - lastMessageShown < TimeSpan.FromSeconds(configuration.WarningIntervalSeconds))
    {
        return;
    }
    lastMessageShown = DateTimeOffset.Now;

    var message = await server.GetMessage();
    if (message == null)
    {
        return;
    }
    icon.BalloonTipTitle = message.Title ?? string.Empty;
    icon.BalloonTipText = (message.Icon ?? string.Empty) + " " + (message.Message ?? string.Empty);
    icon.ShowBalloonTip(1000);
}

static void LogUserOut()
{
    ManagementBaseObject mboShutdown = null;
    ManagementClass mcWin32 = new ManagementClass("Win32_OperatingSystem");
    mcWin32.Get();
    mcWin32.Scope.Options.EnablePrivileges = true;
    ManagementBaseObject mboShutdownParams = mcWin32.GetMethodParameters("Win32Shutdown");
    mboShutdownParams["Flags"] = "2";
    mboShutdownParams["Reserved"] = "0";
    foreach (ManagementObject manObj in mcWin32.GetInstances())
    {
        mboShutdown = manObj.InvokeMethod("Win32Shutdown", mboShutdownParams, null);
    }
}