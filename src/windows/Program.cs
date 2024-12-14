using Microsoft.VisualBasic.ApplicationServices;
using System.Text.Json;
using System.DirectoryServices.AccountManagement;
using System.Management;
using System.Runtime.InteropServices;
using screentime;


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
icon.Text = "Connecting...";

var lastMessageShown = DateTimeOffset.MinValue;

// create the server
var firstArg = args.Length > 0 ? args[0] : string.Empty;
IScreenTimeStateClient client = firstArg switch
{
    "develop" => new screentime.ScreenTimeServiceClient("https://localhost:7186"),
    "live" => new screentime.ScreenTimeServiceClient("https://screentime.azurewebsites.net"),
    _ => new screentime.ScreenTimeLocalStateClient()
};
    

// start the session when the app starts
client.StartSessionAsync();

var systemEventHandlers = new SystemEventHandlers(client);

// get notified when the user is idle for a certain amount of time

var task = Task.Run(async () =>
{
    do
    {
        try
        {
            // get configuration if not already initialized: 
            var configuration = await client.GetUserConfigurationAsync();
            if (configuration == null)
            {
                Utilities.LogToConsole("The server returned a null configuration.");
                continue;
            }

            // check idle time
            UpdateIdleTime();

            var status = await client.GetInteractiveTimeAsync();
            if (status == null)
            {
                Utilities.LogToConsole("The server returned a null status.");
                continue;
            }

            // humanize the uptime and show it in the icon text

            var humanizedUptime = Humanizer.TimeSpanHumanizeExtensions.Humanize(status.LoggedInTime, 2);
            var humanizedDailyLimit = Humanizer.TimeSpanHumanizeExtensions.Humanize(status.dailyTimeLimit,2);

            icon.Text = $"{status.Icon} Interactive time: {humanizedUptime} out of {humanizedDailyLimit}.";

            // if status shows no time logged, send a start 
            if (status.LoggedInTime == TimeSpan.Zero)
            {
                client.StartSessionAsync();
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
                ShowMessageAsync(icon, client, configuration);
                // pick the right icon for the notification icon based on the status

            }

            if (status.Status == Status.Lock)
            {
                await Task.Delay(10000);
                client.EndSessionAsync();
                // lock the computer if the status is lock
                Utilities.LogToConsole("The user's status is locked.");
                Win32.LockWorkStation();
                // LogUserOut();
            }


            await Task.Delay(6000);

        }
        catch (Exception e)
        {
            Utilities.LogToConsole(e.Message);
        }
    }
    while (true);
});

void UpdateIdleTime()
{
    var idleTime = IdleTimeDetector.GetIdleTime();
    if (idleTime.TotalMinutes >= 5) // Notify if idle for 5 minutes
    {
        client.EndSessionAsync();
        Utilities.LogToConsole("The system has been idle for 5 minutes.");
    }
    else
    {
        client.StartSessionAsync();
    }
}

Application.ApplicationExit += (s, e) =>
{
    client.EndSessionAsync();
    icon.Visible = false;
    icon.Dispose();
    task.Dispose();
};
// make sure the main window loop runs
Application.Run(new HiddenForm(task));


async void ShowMessageAsync(NotifyIcon icon, IScreenTimeStateClient server, UserConfiguration configuration)
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