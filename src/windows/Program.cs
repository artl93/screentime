using Microsoft.VisualBasic.ApplicationServices;
using Microsoft.Win32;
using System.DirectoryServices.AccountManagement;

DateTime startTime = DateTime.Now;
TimeSpan downTime = TimeSpan.Zero;
DateTime downTimeStart = DateTime.Now;
DateTime downTimeEnd = DateTime.Now;
DateTime now = DateTime.Now;
bool isLocked = false;
var icon = new NotifyIcon();
icon.Icon = SystemIcons.Application;
icon.ContextMenuStrip = new ContextMenuStrip();
icon.ContextMenuStrip.Items.Add("Exit", null, (s, e) => { icon.Visible = false; Environment.Exit(0); });
icon.Visible = true;


var lastKnownTime = DateTime.UtcNow;
var duration = TimeSpan.Zero;

var lastKnownTimeObject = Registry.GetValue(@"HKEY_CURRENT_USER\Software\ScreenTime", "Last", null);
var durationObject = Registry.GetValue(@"HKEY_CURRENT_USER\Software\ScreenTime", "Cumulative", null);

// if the last known time minus the duration adds up to a time that would be yesterday or earlier (local time), reset the duration
if (lastKnownTimeObject != null && durationObject != null)
{
    lastKnownTime = DateTime.Parse(lastKnownTimeObject.ToString());
    duration = TimeSpan.Parse(durationObject.ToString());
    if (lastKnownTime.ToLocalTime() < DateTime.Today)
    {
        duration = TimeSpan.Zero;
    }
}


startTime = System.DateTime.Now - duration;

void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
{
    if (e.Reason == SessionSwitchReason.SessionLock)
    {
        isLocked = true;
        downTimeStart = System.DateTime.Now;
        Console.WriteLine("The session has been locked.");
    }
    else if (e.Reason == SessionSwitchReason.SessionUnlock)
    {
        isLocked = false;
        downTimeEnd = System.DateTime.Now;
        var lockTime = downTimeEnd - downTimeStart;
        downTime = downTime.Add(lockTime);
        Console.WriteLine("The session has been unlocked.");
        Console.WriteLine("The session was locked for " + (lockTime.TotalSeconds) + " seconds.");
    }
}

SystemEvents.SessionSwitch += new SessionSwitchEventHandler(SystemEvents_SessionSwitch);
Console.WriteLine("Listening for session switch events. Press any key to exit...");

await Task.Run(async () =>
{
    while (true)
    {
        now = DateTime.Now;
        await Task.Delay(1000);
        if (isLocked)
        {
            Console.WriteLine("The session is locked.");
            continue;
        }
        var time = now - startTime - downTime;
        // render the time as hours:minutes:seconds without milliseconds
        string timeString = time.ToString(@"hh\:mm\:ss");

        Console.WriteLine($"Time logged in interactively: {timeString}");
        icon.Text = $"Time logged in interactively: {timeString}";

        // write the time to the registry
        Registry.SetValue(@"HKEY_CURRENT_USER\Software\ScreenTime", "Last", DateTime.UtcNow.ToString("o"));
        Registry.SetValue(@"HKEY_CURRENT_USER\Software\ScreenTime", "Cumulative", time.ToString("G"));
    }
});

 

