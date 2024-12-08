using Microsoft.VisualBasic.ApplicationServices;
using Microsoft.Win32;
using System.Text.Json;
using System.DirectoryServices.AccountManagement;
using System.Runtime.InteropServices;

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
    _ = DateTime.TryParse(lastKnownTimeObject.ToString(), out lastKnownTime);
    _ = TimeSpan.TryParse(durationObject.ToString(), out duration);
    if (lastKnownTime.ToLocalTime() < DateTime.Today)
    {
        duration = TimeSpan.Zero;
    }
}


startTime = System.DateTime.Now - duration;

void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
{
    switch (e.Reason)
    {
        case SessionSwitchReason.SessionLock:
            isLocked = true;
            downTimeStart = System.DateTime.Now;
            Console.WriteLine("The session has been locked.");
            break;
        case SessionSwitchReason.SessionUnlock:
            isLocked = false;
            downTimeEnd = System.DateTime.Now;
            var lockTime = downTimeEnd - downTimeStart;
            downTime = downTime.Add(lockTime);
            Console.WriteLine("The session has been unlocked.");
            Console.WriteLine("The session was locked for " + (lockTime.TotalSeconds) + " seconds.");
            break;
        case SessionSwitchReason.SessionLogoff:
            Console.WriteLine("The session has been logged off.");
            break;
        case SessionSwitchReason.SessionLogon:
            Console.WriteLine("The session has been logged on.");
            break;
        case SessionSwitchReason.ConsoleConnect:
            Console.WriteLine("The console has been connected.");
            break;
        case SessionSwitchReason.ConsoleDisconnect:
            Console.WriteLine("The console has been disconnected.");
            break;
        default:
            Console.WriteLine("The session switch reason is " + e.Reason);
            break;
    }

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
    else if (e.Reason == SessionSwitchReason.SessionLogoff)
    {
        Console.WriteLine("The session has been logged off.");
    }
    else if (e.Reason == SessionSwitchReason.SessionLogon)
    {
        Console.WriteLine("The session has been logged on.");
    }
    else if (e.Reason == SessionSwitchReason.ConsoleConnect)
    {
        Console.WriteLine("The console has been connected.");
    }
    else if (e.Reason == SessionSwitchReason.ConsoleDisconnect)
    {
        Console.WriteLine("The console has been disconnected.");
    }
    else
    {
        Console.WriteLine("The session switch reason is " + e.Reason);
    }
}

SystemEvents.SessionSwitch += new SessionSwitchEventHandler(SystemEvents_SessionSwitch);
SystemEvents.PowerModeChanged += new PowerModeChangedEventHandler(SystemEvents_PowerModeChanged);
SystemEvents.SessionEnding += new SessionEndingEventHandler(SystemEvents_SessionEnding);

void SystemEvents_SessionEnding(object sender, SessionEndingEventArgs e)
{
    switch(e.Reason)
    {
        case SessionEndReasons.Logoff:
            Console.WriteLine("The session is ending because the user is logging off.");
            break;
        case SessionEndReasons.SystemShutdown:
            Console.WriteLine("The session is ending because the system is shutting down.");
            break;
    }
}

void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
{
    switch (e.Mode)
    {
        case PowerModes.Resume:
            Console.WriteLine("The system is resuming from a suspended state.");
            break;
        case PowerModes.Suspend:
            Console.WriteLine("The system is entering a suspended state.");
            break;
    }
}

// get notified when the user is idle for a certain amount of time



Console.WriteLine("Listening for session switch events. Press any key to exit...");

// create the http connection
var client = new HttpClient();
client.BaseAddress = new Uri("https://localhost:7186/");
client.DefaultRequestHeaders.Accept.Clear();
client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));


var options = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
};


var task = Task.Run(async () =>
{
    do
    {
        try
        {
            now = DateTime.Now;
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

            // send username and time since last update to status to the server at https://localhost:7186/log/{Environment.UserName}/{time.TotalSeconds}
            var response = await client.PutAsync($"log/{Environment.UserName}/{time.Seconds}", null);

            // get any messages from the server at https://localhost:7186/messages/{Environment.UserName} and show them in a notification
            var messageResponse = await client.GetAsync($"message/{Environment.UserName}");

            if (messageResponse.IsSuccessStatusCode)
            {
                var message = await messageResponse.Content.ReadAsStringAsync();
                // deserialize the message from JSON and show it in a notification
                var messageObject = System.Text.Json.JsonSerializer.Deserialize<ServerMessage>(message, options);

                var toolTipIcon = ToolTipIcon.None;
                // map the icon string to a ToolTipIcon
                if (messageObject.Icon == "\U0001f6d1")
                {
                    icon.Icon = SystemIcons.Error;
                    toolTipIcon = ToolTipIcon.Error;

                }
                else if (messageObject.Icon == "⏳")
                {
                    icon.Icon = SystemIcons.Hand;
                    toolTipIcon = ToolTipIcon.Warning;
                }
                else if (messageObject.Icon == "⚠️")
                {
                    icon.Icon = SystemIcons.Warning;
                    toolTipIcon = ToolTipIcon.Warning;
                }
                else if (messageObject.Icon == "🕒")
                {
                    icon.Icon = SystemIcons.Information;
                    toolTipIcon = ToolTipIcon.Info; 
                }

                Console.WriteLine(messageObject);
                if (messageObject != null)
                {

                    icon.ShowBalloonTip(100, 
                        messageObject.Title, 
                        messageObject.Icon + " " +
                        messageObject.Message,
                        toolTipIcon);
                }

                // icon.ShowBalloonTip(100, "Screen Time", message, ToolTipIcon.Info);
            }

                    var idleTime = IdleTimeDetector.GetIdleTime();
        if (idleTime.TotalMinutes >= 5) // Notify if idle for 5 minutes
        {
            Console.WriteLine("The system has been idle for 5 minutes.");
            icon.ShowBalloonTip(100, "Idle Notification", "The system has been idle for 5 minutes.", ToolTipIcon.Info);
        }

            await Task.Delay(6000);



            // show a notification with the time every five minutes
            // if (now.Minute % 1 == 0 && now.Second == 0)
            // icon.ShowBalloonTip(100, "Screen Time", $"Time logged in interactively: {timeString}", ToolTipIcon.Info);

            // write the time to the registry
            Registry.SetValue(@"HKEY_CURRENT_USER\Software\ScreenTime", "Last", DateTime.UtcNow.ToString("o"));
            Registry.SetValue(@"HKEY_CURRENT_USER\Software\ScreenTime", "Cumulative", time.ToString("G"));
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
    while (true);
});


Application.ApplicationExit += (s, e) =>
{
    icon.Visible = false;
    icon.Dispose();
    task.Dispose();
};
// make sure the main window loop runs
Application.Run(new HiddenForm(task));


public class IdleTimeDetector
{
    [DllImport("user32.dll")]
    static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

    [StructLayout(LayoutKind.Sequential)]
    struct LASTINPUTINFO
    {
        public uint cbSize;
        public uint dwTime;
    }

    public static TimeSpan GetIdleTime()
    {
        LASTINPUTINFO lastInputInfo = new LASTINPUTINFO();
        lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);
        GetLastInputInfo(ref lastInputInfo);

        uint idleTime = (uint)Environment.TickCount - lastInputInfo.dwTime;
        return TimeSpan.FromMilliseconds(idleTime);
    }
}