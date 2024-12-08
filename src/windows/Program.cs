using Microsoft.VisualBasic.ApplicationServices;
using Microsoft.Win32;
using System.Text.Json;
using System.DirectoryServices.AccountManagement;

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



var server = new screentime.Server();


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

            if (status.Status != Status.Okay)
            {
                ShowMessageAsync(icon, server);
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


async void ShowMessageAsync(NotifyIcon icon, screentime.Server server)
{
    // only show every 30 seconds
    if (DateTimeOffset.Now - lastMessageShown < TimeSpan.FromSeconds(30))
    {
        return;
    }
    lastMessageShown = DateTimeOffset.Now;

    var message = await server.GetMessage();
    if (message == null)
    {
        return;
    }
    icon.Icon = SystemIcons.Warning;
    icon.BalloonTipTitle = message.Title ?? string.Empty;
    icon.BalloonTipText = (message.Icon ?? string.Empty) + " " + (message.Message ?? string.Empty);
    icon.ShowBalloonTip(1000);
}

