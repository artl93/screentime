using Microsoft.Win32;

System.DateTime startTime = System.DateTime.Now;
System.TimeSpan downTime = System.TimeSpan.Zero;
System.DateTime now = System.DateTime.Now;

void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
{
    if (e.Reason == SessionSwitchReason.SessionLock)
    {
        downTime = System.DateTime.Now - now;
        Console.WriteLine("The session has been locked.");
    }
    else if (e.Reason == SessionSwitchReason.SessionUnlock)
    {
        Console.WriteLine("The session has been unlocked.");
        Console.WriteLine("The session was locked for " + downTime.TotalSeconds + " seconds.");
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
        Console.WriteLine("Time logged in interactively: " + (now - startTime - downTime).ToString());
    }
});

 

